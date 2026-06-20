/// <summary>
/// NotificationService — a background worker that consumes RabbitMQ events.
/// There is NO HTTP server here — this is a pure worker/hosted service.
///
/// Teaching point: This service is completely decoupled from all other services.
/// It subscribes to OrderPlacedEvent via MassTransit. When an order is placed,
/// RabbitMQ delivers the message here automatically. OrderService never calls
/// this service directly — it only knows about the RabbitMQ exchange.
/// </summary>

using NotificationService.Consumers;
using SharedKernel.Messaging;

var builder = Host.CreateApplicationBuilder(args);

// Register MassTransit with our consumer.
// MassTransit will auto-create the RabbitMQ queue named after this consumer
// and bind it to the OrderPlacedEvent exchange on startup.
builder.Services.AddRabbitMqBus(builder.Configuration, x =>
{
    x.AddConsumer<OrderPlacedConsumer>();
});

var host = builder.Build();
host.Run();
