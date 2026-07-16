
namespace Choreography.Order.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Choreography.Order.Infrastructure.Entities;
using Choreography.Order.Infrastructure.Enums;

public class OrderDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }

    // For Unit-test
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    // protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    // {
    //     optionsBuilder.UseInMemoryDatabase(databaseName: "OrderDb");
    // }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder
            .Entity<Order>()
            .Property(prop => prop.Status)
            .HasConversion<EnumToStringConverter<OrderStatus>>();
    }
}