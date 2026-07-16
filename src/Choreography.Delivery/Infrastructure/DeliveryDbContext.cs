namespace Choreography.Delivery.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Choreography.Delivery.Infrastructure.Entities;


public class DeliveryDbContext : DbContext
{
    public DbSet<Delivery> Deliveries { get; set; }

    // For Unit-test
    public DeliveryDbContext(DbContextOptions<DeliveryDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase(databaseName: "DeliveryDb");
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // builder
        //     .Entity<Order>()
        //     .Property(prop => prop.Status)
        //     .HasConversion<EnumToStringConverter<OrderStatus>>();
    }

}