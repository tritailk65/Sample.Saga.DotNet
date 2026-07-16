using System.Reflection;
using Choreography.Order.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class OrderDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
{
    public OrderDbContext CreateDbContext(params string[] args)
    {
        var builder = new DbContextOptionsBuilder<OrderDbContext>();
        Apply(builder);
        return new OrderDbContext(builder.Options);
    }

    public static void Apply(DbContextOptionsBuilder builder)
    {
        builder.UseNpgsql("Host=localhost;Database=Test_OrderDb;Username=postgres;Password=123", m =>
        {
            m.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
            m.MigrationsHistoryTable($"__{nameof(OrderDbContext)}");
        });
    }

    public OrderDbContext CreateDbContext(DbContextOptionsBuilder<OrderDbContext> optionsBuilder)
    {
        return new OrderDbContext(optionsBuilder.Options);
    }
}