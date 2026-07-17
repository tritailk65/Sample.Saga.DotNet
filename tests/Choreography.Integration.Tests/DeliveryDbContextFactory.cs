public class DeliveryDbContextFactory : IDesignTimeDbContextFactory<DeliveryDbContext>
{
    public DeliveryDbContext CreateDbContext(params string[] args)
    {
        var builder = new DbContextOptionsBuilder<DeliveryDbContext>();
        Apply(builder);
        return new DeliveryDbContext(builder.Options);
    }

    public static void Apply(DbContextOptionsBuilder builder)
    {
        builder.UseNpgsql("Host=localhost;Database=Test_DeliveryDb;Username=postgres;Password=123", m =>
        {
            m.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
            m.MigrationsHistoryTable($"__{nameof(DeliveryDbContext)}");
        });
    }

    public DeliveryDbContext CreateDbContext(DbContextOptionsBuilder<DeliveryDbContext> optionsBuilder)
    {
        return new DeliveryDbContext(optionsBuilder.Options);
    }
}