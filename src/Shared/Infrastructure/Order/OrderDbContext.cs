
namespace Shared.Infrastructure.Order.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Shared.Infrastructure.Order.Infrastructure.Enums;
using Shared.Infrastructure.Order.Infrastructure.Entities;

public class OrderDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }

    // For Unit-test
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder
            .Entity<Order>()
            .Property(prop => prop.Status)
            .HasConversion<EnumToStringConverter<OrderStatus>>();
    }
}