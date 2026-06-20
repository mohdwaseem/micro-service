# Getting Started

## Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| .NET SDK | 10.0+ | `dotnet --version` |
| Docker Desktop | Any recent | For Docker path |
| SQL Server | 2019+ | For local path (or use Docker) |

## Path 1: Docker Compose (Recommended)

Everything runs in containers — no local SQL Server or RabbitMQ needed.

```bash
# Clone/navigate to the project root
cd c:\Dev\Demos\MicroServiceDemo

# Copy environment config (edit SA_PASSWORD and JWT_SECRET if desired)
copy .env.example .env

# Build and start all 8 containers
docker compose up --build

# To run in background
docker compose up --build -d
```

Wait ~60 seconds for SQL Server to initialize on first run.

### Watching logs

```bash
# All services
docker compose logs -f

# Just NotificationService (to see async events)
docker compose logs -f notificationservice

# Just the gateway
docker compose logs -f apigateway
```

### Stopping

```bash
docker compose down          # stop containers, keep volumes (data persists)
docker compose down -v       # stop and delete all data (fresh start)
```

## Path 2: Running Locally Without Docker

### 1. Start infrastructure

```bash
# RabbitMQ (already cached if you ran Docker path)
docker run -d --name demo-rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3.13-management

# SQL Server (if no local instance)
docker run -d --name demo-sqlserver \
  -e SA_PASSWORD="YourStrong@Passw0rd" \
  -e ACCEPT_EULA=Y \
  -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2022-latest
```

### 2. Update appsettings.json for each service

All services default to `localhost` SQL Server with `YourStrong@Passw0rd` — change if needed.

### 3. Run each service in a separate terminal

```bash
# Terminal 1
dotnet run --project src/UserService

# Terminal 2
dotnet run --project src/ProductService

# Terminal 3
dotnet run --project src/OrderService

# Terminal 4
dotnet run --project src/NotificationService

# Terminal 5
dotnet run --project src/ApiGateway

# Terminal 6
dotnet run --project src/Frontend
```

EF Core migrations run automatically on startup — databases are created on first launch.

### Default local ports (from launchSettings.json)

| Service | URL |
|---------|-----|
| Frontend | http://localhost:3000 (or whatever MVC assigns) |
| ApiGateway | http://localhost:5000 |
| UserService | http://localhost:5001 |
| ProductService | http://localhost:5002 |
| OrderService | http://localhost:5003 |

> Adjust port numbers in `ApiGateway/appsettings.json` clusters to match what each service actually starts on.

## Using the Scalar API UIs

Each backend service exposes interactive API documentation at `/scalar`:

- UserService: `http://localhost:5001/scalar`
- ProductService: `http://localhost:5002/scalar`
- OrderService: `http://localhost:5003/scalar`

To test authenticated endpoints in Scalar:
1. Call `POST /api/auth/register` on UserService
2. Copy the `token` from the response
3. Click **Authorize** in Scalar → paste `Bearer <token>`
4. Authorized endpoints now work

## Using the RabbitMQ Management UI

Open `http://localhost:15672` (user: `guest`, pass: `guest`).

After placing an order, check:
- **Exchanges** tab → find `shared-kernel:events:order-placed-event` (or similar auto-generated name)
- **Queues** tab → find the consumer queue — it should show 0 messages (consumed immediately)
- **Connections** tab → see OrderService and NotificationService both connected

## Running EF Core Migrations Manually

Migrations are applied automatically on startup, but if you want to run them manually:

```bash
# From solution root
dotnet ef database update --project src/UserService --startup-project src/UserService
dotnet ef database update --project src/ProductService --startup-project src/ProductService
dotnet ef database update --project src/OrderService --startup-project src/OrderService
```

To create a new migration after modifying an entity:

```bash
dotnet ef migrations add <MigrationName> --project src/UserService --startup-project src/UserService
```
