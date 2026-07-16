using System.Reflection;
using Choreography.Delivery.Infrastructure;
using Choreography.Inventory.Infrastructure;
using Choreography.Order.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Choreography.Integration.Tests;

public static class ChoreographyIntegrationConfigurationExtensions
{
    public static IServiceCollection ConfigureMassTransit(this IServiceCollection services, Action<IBusRegistrationConfigurator>? configure = null)
    {

        string orderDbConnection = "Host=localhost;Database=Test_OrderDb;Username=postgres;Password=123";
        string inventoryDbConnection = "Host=localhost;Database=Test_InventoryDb;Username=postgres;Password=123"; 
        string deliveryDbConnection = "Host=localhost;Database=Test_DeliveryDb;Username=postgres;Password=123";

        services
            .AddDbContext<OrderDbContext>(options =>
            {
                options.UseNpgsql(orderDbConnection, m =>
                {
                    m.MigrationsAssembly(typeof(OrderDbContext).Assembly.FullName); // Find migration file in Order Project
                    m.MigrationsHistoryTable($"__{nameof(OrderDbContext)}");
                });
            })
            .AddDbContext<DeliveryDbContext>(options =>
            {
                options.UseNpgsql(deliveryDbConnection);
            })
            .AddHostedService<MigrationHostedService<OrderDbContext>>()
            .AddDbContext<InventoryDbContext>(options =>
            {
                options.UseNpgsql(inventoryDbConnection);
            })
            .AddMassTransitTestHarness(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();

                // x.AddQuartzConsumers();

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