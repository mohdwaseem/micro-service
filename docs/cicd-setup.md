# CI/CD Setup Guide

Local CI/CD pipeline using Gitea (Git server) + Jenkins (CI orchestrator) + DbUp (database migrations).

## Architecture Overview

```
Developer
   │  git push
   ▼
Gitea :3001 ──webhook──► Jenkins :8090
                              │
                    ┌─────────┼──────────────┐
                    ▼         ▼              ▼
               dotnet      dotnet       DbUp migrator
               build       test         (SQL Server)
                    └─────────┼──────────────┘
                              │ artifacts
                              ▼
                        docker compose up -d
                              │
                              ▼
                     Email notification
```

---

## Prerequisites

- Docker Desktop running (Windows)
- .NET 10 SDK installed locally (for local dev)
- The project `.env` file set up (copy `.env.example` → `.env`)

---

## Step 1 — Start the App Infrastructure

The app services (SQL Server, RabbitMQ, all .NET services) must be running before the pipeline can migrate databases or deploy.

```powershell
# From the project root
docker compose up -d
```

Verify: `http://localhost:3000` (Frontend) and `http://localhost:15672` (RabbitMQ UI).

---

## Step 2 — Start CI/CD Infrastructure

```powershell
# This builds the custom Jenkins image (takes ~5 min on first run — installs .NET 10 SDK)
docker compose -f docker-compose.infra.yml up -d
```

Watch the build:

```powershell
docker compose -f docker-compose.infra.yml logs -f
```

---

## Step 3 — Configure Gitea

1. Open **http://localhost:3001**
2. Complete the setup wizard (SQLite, default settings are fine)
3. Create an admin account — note the username/password
4. Create a new organisation or use your personal account
5. Create a new repository named `MicroServiceDemo`
6. Push the local code to Gitea:

```powershell
# Add Gitea as a remote
git remote add gitea http://localhost:3001/<your-username>/MicroServiceDemo.git

# Push
git push gitea main
```

---

## Step 4 — Unlock Jenkins

1. Open **http://localhost:8090**
2. Get the initial admin password:

```powershell
docker exec demo-jenkins cat /var/jenkins_home/secrets/initialAdminPassword
```

3. Paste it into the unlock screen
4. Choose **Install suggested plugins** (the custom image pre-installs pipeline plugins too)
5. Create your admin user

---

## Step 5 — Add Credentials to Jenkins

Go to **Manage Jenkins → Credentials → System → Global credentials → Add Credential**.

Add four **Secret text** credentials with these exact IDs:

| Credential ID        | Value                          |
|----------------------|--------------------------------|
| `MSDEMO_SA_PASSWORD` | Your SQL Server SA password    |
| `MSDEMO_JWT_SECRET`  | Your JWT signing secret        |
| `MSDEMO_RABBITMQ_USER` | RabbitMQ username (`guest`)  |
| `MSDEMO_RABBITMQ_PASS` | RabbitMQ password (`guest`)  |

These match the `.env` file values.

---

## Step 6 — Create the Jenkins Pipeline

1. **New Item** → name it `MicroServiceDemo` → choose **Pipeline**
2. Under **Pipeline**:
   - Definition: **Pipeline script from SCM**
   - SCM: **Git**
   - Repository URL: `http://demo-gitea:3000/<your-username>/MicroServiceDemo.git`
     *(use the internal Docker hostname `demo-gitea` so Jenkins can reach Gitea on the shared network)*
   - Credentials: add a Gitea username/password credential if the repo is private
   - Branch: `*/main`
   - Script Path: `Jenkinsfile`
3. **Save**

---

## Step 7 — Configure the Gitea Webhook

This triggers Jenkins automatically on every `git push`.

1. In Gitea, go to your repository → **Settings → Webhooks → Add Webhook → Gitea**
2. Target URL: `http://demo-jenkins:8080/gitea-webhook/post`
   *(internal Docker hostname — Gitea and Jenkins share `demo-cicd-network`)*
3. Secret: leave blank or set one (optional)
4. Trigger: **Push events**
5. **Add Webhook** → click **Test Delivery** to verify it fires

---

## Step 8 — Configure Email Notifications (optional)

1. **Manage Jenkins → System → Extended E-mail Notification**
2. SMTP Server: your SMTP relay (e.g. `smtp.gmail.com`, port `587`, TLS)
3. Default recipients: `mohdwaseem488@gmail.com`
4. Test the configuration with **Test Configuration**

For Gmail: use an App Password, not your main password.

---

## Step 9 — Run the Pipeline

Either:
- Push a commit to Gitea → webhook fires Jenkins automatically
- Or click **Build Now** in Jenkins manually

### What each stage does

| Stage | What runs | Where |
|---|---|---|
| **Checkout** | `git checkout` from Gitea | Jenkins workspace |
| **Build** | `dotnet restore` + `dotnet build --configuration Release` | Jenkins container |
| **Unit Tests** | `dotnet test --filter Category!=E2E` | Jenkins container |
| **DB Migrate** | `DatabaseMigrator` (DbUp) connects to SQL Server on `host.docker.internal:1433` | Jenkins container → host SQL Server |
| **Docker Build** | `docker compose build` | Host Docker via socket |
| **Deploy** | `docker compose up -d` | Host Docker via socket |

---

## Running E2E Tests Manually

E2E tests are excluded from the CI pipeline (they need the live stack running).

```powershell
# 1. Install Playwright browser binaries (once)
cd tests/E2E.Tests
dotnet build
pwsh bin/Debug/net10.0/playwright.ps1 install chromium

# 2. Make sure the full stack is running (docker compose up -d)

# 3. Run E2E tests
dotnet test tests/E2E.Tests --filter "Category=E2E"
```

---

## Adding a New Database Migration

1. Create a new SQL file in the appropriate service folder:

```
src/DatabaseMigrator/Scripts/UserServiceDb/0002_AddPhoneNumber.sql
```

Scripts run in filename order — prefix with a zero-padded sequence number.

2. DbUp tracks applied scripts in a `SchemaVersions` table per database — re-running is safe.

3. For local dev, also create an EF Core migration:

```powershell
dotnet ef migrations add AddPhoneNumber --project src/UserService
```

The EF migration applies in Development; DbUp applies the SQL script in CI/Production.

---

## Troubleshooting

**Jenkins can't reach SQL Server during DB Migrate**
- Ensure `docker compose up -d` (the app stack) is running and SQL Server is healthy
- Verify port 1433 is not blocked: `Test-NetConnection -ComputerName localhost -Port 1433`

**Jenkins can't run docker compose**
- Confirm the Docker socket is mounted: `docker exec demo-jenkins docker ps`
- On Windows, Docker Desktop must be running and the Linux engine selected

**Gitea webhook returns 404**
- Verify the Gitea plugin is installed in Jenkins
- Check the webhook URL uses `demo-jenkins:8080` (internal), not `localhost:8090`

**`dotnet` not found in Jenkins**
- Rebuild the Jenkins image: `docker compose -f docker-compose.infra.yml build --no-cache jenkins`
