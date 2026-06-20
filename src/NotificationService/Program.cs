/// <summary>
/// NotificationService — a background worker that consumes RabbitMQ events.
/// There is NO HTTP server here — this is a pure worker/hosted service.
///
/// Teaching point: This service is completely decoupled from all other services.
/// It subscribes to OrderPlacedEvent via MassTransit. When an order is placed,
/// RabbitMQ delivers the message here automatically. OrderService never calls
/// this service directly — it only knows about the RabbitMQ exchange.
/// </summary>

using MassTransit;
using NotificationService.Consumers;
using SharedKernel.Messaging;

var builder = Host.CreateApplicationBuilder(args);

// Register MassTransit with our consumer and a retry policy.
//
// Retry flow — 3 attempts after the first failure, 10 seconds apart:
//   Attempt 1 (initial) → fails → wait 10 s
//   Attempt 2           → fails → wait 10 s
//   Attempt 3           → fails → wait 10 s
//   Attempt 4           → fails → message moved to dead-letter queue
//
// Dead-letter queue name: "notification-service_error"
// Inspect it in RabbitMQ Management UI → http://localhost:15672 (guest / guest)
// Queues tab → look for "*_error" to see stranded messages.
//
// To test: OrderPlacedConsumer currently throws an exception, so every message
// will exhaust all retries and land in the error queue.
builder.Services.AddRabbitMqBus(
    builder.Configuration,
    configure: x =>
    {
        x.AddConsumer<OrderPlacedConsumer>();
    },
    retryPolicy: r => r.Interval(3, TimeSpan.FromSeconds(10)));

var host = builder.Build();
host.Run();
