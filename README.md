# MicroServiceDemo

A full-stack demo application built with **.NET 10** that demonstrates how microservices work in practice. Uses an e-commerce domain (products + orders) to illustrate the key patterns every developer should understand.

## What This Demo Teaches

- **Service decomposition** — four independent services, each owning its own data
- **Database-per-service** — UserServiceDb, ProductServiceDb, OrderServiceDb on one SQL Server instance
- **API Gateway** (YARP) — single entry point, routes by URL prefix, validates JWT
- **Async messaging** (RabbitMQ + MassTransit) — OrderService publishes, NotificationService consumes with zero coupling
- **JWT authentication** — issued by UserService, validated by gateway (defense 1) and each service (defense 2)
- **API documentation** (Scalar) — each service exposes its own interactive API docs
- **Server-side session** — MVC Frontend stores JWT in ISession, not in the browser

## Architecture

```
Browser
  |
  v
[MVC Frontend :3000]   ASP.NET Core MVC, Razor views, Bootstrap
  | HttpClient → Gateway (JWT in server-side session)
  v
[API Gateway :5000]    YARP reverse proxy, validates JWT, routes by path
  |
  ├── /api/auth/*     →  [User Service :5001]   UserServiceDb
  ├── /api/products/* →  [Product Service :5002] ProductServiceDb
  └── /api/orders/*   →  [Order Service :5003]   OrderServiceDb
                              |
                    publishes OrderPlacedEvent
                              ↓
                         [RabbitMQ :5672]
                              ↓
                    [Notification Service]         (Worker, no HTTP)
                    logs "EMAIL SENT → user@..."

[SQL Server :1433]
  ├── UserServiceDb
  ├── ProductServiceDb
  └── OrderServiceDb
```

## Quick Start (Docker — Recommended)

**Prerequisites:** Docker Desktop

```bash
# 1. Copy environment config
copy .env.example .env

# 2. Start everything
docker compose up --build

# 3. Open the app
start http://localhost:3000
```

| URL | What |
|-----|------|
| `http://localhost:3000` | MVC Frontend |
| `http://localhost:5000` | API Gateway |
| `http://localhost:5001/scalar` | UserService API docs |
| `http://localhost:5002/scalar` | ProductService API docs |
| `http://localhost:5003/scalar` | OrderService API docs |
| `http://localhost:15672` | RabbitMQ Management UI (guest/guest) |

> Ports 5001–5003 are exposed by `docker-compose.override.yml` (dev mode only).

## Quick Start (Local — No Docker)

**Prerequisites:** .NET 10 SDK, SQL Server (local or Docker), RabbitMQ (local or Docker)

Start RabbitMQ if needed:
```bash
docker run -d -p 5672:5672 -p 15672:15672 rabbitmq:3.13-management
```

Update connection strings in each service's `appsettings.json` to point at your SQL Server.

Run each service in a separate terminal:
```bash
dotnet run --project src/UserService          # :5001
dotnet run --project src/ProductService       # :5002
dotnet run --project src/OrderService         # :5003
dotnet run --project src/NotificationService  # (no port)
dotnet run --project src/ApiGateway           # :5000
dotnet run --project src/Frontend             # :3000
```

## The Key Demo Flow (Place an Order)

1. Register a new user at `http://localhost:3000` → `/Account/Register`
2. You are logged in and redirected to the Product Catalog
3. Click **Place Order**, select a product and quantity, submit
4. Watch the **NotificationService** logs:

```
docker compose logs -f notificationservice
```

You will see:
```
====================================================
EMAIL SENT (simulated)
  To:      alice@example.com
  Subject: Order #3f2a... Confirmed
  Items:   1 item(s)
           - Laptop Pro 15 x1 @ $1,299.99
  Total:   $1,299.99
  Placed:  2026-06-20 14:23:45Z
====================================================
```

This appears **~1 second after** you submitted the form — demonstrating asynchronous, decoupled messaging. OrderService did not wait for this; it returned `201 Created` immediately.

## Service Reference

| Service | Project type | Database | Key packages |
|---------|-------------|----------|-------------|
| UserService | Minimal API | UserServiceDb | EF Core, BCrypt, JwtBearer, Scalar |
| ProductService | Minimal API | ProductServiceDb | EF Core, JwtBearer, Scalar |
| OrderService | Minimal API | OrderServiceDb | EF Core, MassTransit, Scalar |
| NotificationService | Worker | — | MassTransit |
| ApiGateway | Minimal API | — | YARP, JwtBearer |
| Frontend | MVC / Razor | — | ISession, HttpClient |

## Project Structure

```
MicroServiceDemo/
├── src/
│   ├── SharedKernel/          # Shared event contracts + MassTransit helper
│   ├── ApiGateway/            # YARP gateway
│   ├── UserService/           # Auth + JWT issuance
│   ├── ProductService/        # Product catalog
│   ├── OrderService/          # Orders + RabbitMQ publisher
│   ├── NotificationService/   # RabbitMQ consumer (worker)
│   └── Frontend/              # MVC frontend
├── docs/
│   ├── architecture.md        # Deep-dive on patterns used
│   └── getting-started.md     # Step-by-step developer guide
├── docker-compose.yml
├── docker-compose.override.yml
├── .env.example
└── MicroServiceDemo.sln
```

## See Also

- [Architecture Deep-Dive](docs/architecture.md)
- [Getting Started Guide](docs/getting-started.md)
