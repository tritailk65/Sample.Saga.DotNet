namespace Choreography.Integration.Tests;

public static class ChoreographyIntegrationConfigurationExtensions
{
    public static IServiceCollection ConfigureMassTransit(this IServiceCollection services, Action<IBusRegistrationConfigurator>? configure = null)
    {
        services
            .AddDbContext<OrderDbContext>(options =>
            {
                OrderDbContextFactory.Apply(options);
            })
            
            .AddDbContext<InventoryDbContext>(options =>
            {
                InventoryDbContextFactor.Apply(options);
            })
            .AddDbContext<DeliveryDbContext>(options =>
            {
                DeliveryDbContextFactory.Apply(options);
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