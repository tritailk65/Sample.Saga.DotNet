namespace Shared.Infrastructure.OrderSaga;

using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Shared.StateMachines;
using Shared.Infrastructure.Order.Infrastructure.Entities;

public class OrderSagaDbContext : SagaDbContext
{
    public DbSet<Order> Orders { get; set; }

    // For Unit-test
    public OrderSagaDbContext(DbContextOptions<OrderSagaDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // builder
        //     .Entity<OrderSaga>()
        //     .Property(prop => prop.Goods)
        //     .HasConversion<EnumToStringConverter<OrderStatus>>();
    }


    protected override IEnumerable<ISagaClassMap> Configurations => new[] { new OrderClassMap() };
}
