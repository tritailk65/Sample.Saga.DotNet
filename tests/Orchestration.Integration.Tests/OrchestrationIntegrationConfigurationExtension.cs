using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Delivery.Infrastructure;
using Shared.Infrastructure.Inventory.Infrastructure;
using Shared.Infrastructure.OrderSaga;

namespace Orchestration.Integration.Tests;


public static class OrchestrationIntegrationConfigurationExtensions
{
    public static IServiceCollection ConfigureMassTransit(this IServiceCollection services, Action<IBusRegistrationConfigurator>? configure = null)
    {
        services
            .AddDbContext<OrderSagaDbContext>(options =>
            {
                ApplicationDbContextFactory<OrderSagaDbContext>.Apply(options);
            })
            .AddDbContext<InventoryDbContext>(options =>
            {
                ApplicationDbContextFactory<InventoryDbContext>.Apply(options);
            })
            .AddDbContext<DeliveryDbContext>(options =>
            {
                ApplicationDbContextFactory<DeliveryDbContext>.Apply(options);
            })
            .AddMassTransitTestHarness(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();

                x.AddPublishMessageScheduler();

                configure?.Invoke(x);

                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UsePublishMessageScheduler();

                    cfg.ConfigureEndpoints(context);
                });
            });

        return services;
    }
}

public class DeliveryDbContextFactory : ApplicationDbContextFactory<DeliveryDbContext>
{
}

public class InventoryDbContextFactory : ApplicationDbContextFactory<InventoryDbContext>
{
}

public class OrderSagaDbContextFactory : ApplicationDbContextFactory<OrderSagaDbContext>
{
}