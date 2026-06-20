# Architecture Deep-Dive

## Why Separate Services?

Each microservice owns exactly one bounded context:

| Service | Context | Why separate? |
|---------|---------|---------------|
| UserService | Identity | Auth rules change independently; breaches are isolated |
| ProductService | Catalog | Can scale horizontally if product queries spike |
| OrderService | Commerce | Can be deployed with stronger SLAs than the catalog |
| NotificationService | Messaging | Can be slow/retry without blocking orders |

## Database-per-Service Pattern

Each service has its **own SQL Server database**. No service queries another service's database directly. If OrderService needs user information (e.g., email), it receives it in the JWT claim or in the event payload.

```
❌ BAD:  OrderService runs: SELECT email FROM UserServiceDb.Users WHERE id = ?
✅ GOOD: Email is in the JWT claim, passed from the client via the Authorization header
```

**In this demo:** All three databases live on the same SQL Server container for simplicity. In production each would be a separate server (or managed cloud DB).

## JWT Flow (How Authentication Works)

```
1. POST /api/auth/login → UserService
   - Validates password with BCrypt
   - Returns signed JWT (HMAC-SHA256, 8-hour expiry)

2. MVC Frontend stores JWT in ISession (server-side)
   - Never sent to browser as readable data
   - Session ID cookie is HttpOnly

3. Every subsequent request:
   Frontend → reads JWT from Session → attaches as Authorization: Bearer <token>

4. API Gateway validates JWT (first line of defense)
   - Requests to /api/orders/* without valid JWT → 401 here

5. OrderService also validates JWT (defense-in-depth)
   - Reads ClaimTypes.NameIdentifier to get the UserId
   - Only returns orders belonging to that user
```

All services share the same `Jwt:Secret` from the environment. UserService issues; everyone else only validates.

## RabbitMQ Messaging (How Async Works)

```
OrderService                     RabbitMQ                    NotificationService
    |                               |                               |
    | publishEndpoint.Publish(evt) ─→ Exchange: order-placed-event  |
    |                               |       ↓                       |
    | Returns 201 immediately       | Queue: notification-svc:...   |
    |                               |                ↓              |
    |                               |          IConsumer<T>.Consume()
```

**MassTransit auto-creates** the exchange and queue on startup. The exchange name is derived from the message type's full CLR name. The queue name is derived from the consumer class name + assembly.

**Key insight:** `OrderService` calls `publishEndpoint.Publish(new OrderPlacedEvent(...))`. It never calls `NotificationService` directly and doesn't even reference the `NotificationService` project. The only shared artifact is `SharedKernel.Events.OrderPlacedEvent` — the contract.

## YARP Routing

The API Gateway reads this config from `appsettings.json`:

```
Route: /api/auth/*     → users-cluster (anonymous)
Route: /api/users/*    → users-cluster (requires JWT)
Route: /api/products/* → products-cluster (anonymous)
Route: /api/orders/*   → orders-cluster (requires JWT)
```

YARP proxies the entire request, including the `Authorization` header, to the downstream service. Downstream services re-validate the JWT.

In Docker Compose, cluster addresses use Docker's internal DNS:
```
http://userservice:8080
http://productservice:8080
http://orderservice:8080
```

## SharedKernel Trade-off

In this demo, `SharedKernel` is a **project reference**. In production microservices:

- **Option A (NuGet package):** Publish `SharedKernel` to a private NuGet feed. Each service pins to a version. Breaking changes require a version bump and coordinated deployments.
- **Option B (copy the contracts):** Each service has its own copy of the event class. More duplication but zero compile-time coupling.

For a demo, project reference is the right trade-off — it's simpler and demonstrates the pattern.

## How to Add a New Microservice

1. `dotnet new webapi -n PaymentService -o src/PaymentService`
2. `dotnet sln add src/PaymentService/PaymentService.csproj`
3. Add `SharedKernel` reference if the service publishes or consumes events
4. Add EF Core + SQL Server packages; create a `PaymentDbContext`; run `dotnet ef migrations add`
5. Write endpoints in `src/PaymentService/Endpoints/`
6. Add JWT Bearer + Scalar to `Program.cs`
7. Add a new cluster + route in `src/ApiGateway/appsettings.json`
8. Add the service to `docker-compose.yml`

The existing services don't change at all — that's the point of loose coupling.
