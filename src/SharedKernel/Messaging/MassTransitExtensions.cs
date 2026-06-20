using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SharedKernel.Messaging;

/// <summary>
/// Shared MassTransit/RabbitMQ registration so both OrderService and NotificationService
/// configure RabbitMQ the same way from the same config keys.
///
/// Teaching point: The caller passes in a delegate to register their specific consumers
/// or publishers — this extension handles the shared infrastructure (host, credentials).
///
/// Optional retryPolicy — when supplied, MassTransit wraps every receive endpoint on
/// this bus with that retry pipeline.  After all retry attempts are exhausted, the
/// message is moved to an "_error" queue (dead-letter) that is visible in the
/// RabbitMQ Management UI at http://localhost:15672.
/// </summary>
public static class MassTransitExtensions
{
    public static IServiceCollection AddRabbitMqBus(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configure = null,
        Action<IRetryConfigurator>? retryPolicy = null)
    {
        services.AddMassTransit(x =>
        {
            configure?.Invoke(x);

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(configuration["RabbitMq:Host"] ?? "localhost", "/", h =>
                {
                    h.Username(configuration["RabbitMq:Username"] ?? "guest");
                    h.Password(configuration["RabbitMq:Password"] ?? "guest");
                });

                // Apply the retry pipeline before any consumer runs.
                // Only wired up when the caller opts in (NotificationService does; OrderService does not).
                if (retryPolicy != null)
                    cfg.UseMessageRetry(retryPolicy);

                // Auto-creates exchanges, queues, and bindings based on registered consumers.
                // Queue names are derived from the consumer class name and assembly.
                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}
