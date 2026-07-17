public class InventoryDbContextFactor : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(params string[] args)
    {
        var builder = new DbContextOptionsBuilder<InventoryDbContext>();
        Apply(builder);
        return new InventoryDbContext(builder.Options);
    }

    public static void Apply(DbContextOptionsBuilder builder)
    {
        builder.UseNpgsql("Host=localhost;Database=Test_InventoryDb;Username=postgres;Password=123", m =>
        {
            m.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
            m.MigrationsHistoryTable($"__{nameof(InventoryDbContext)}");
        });
    }

    public InventoryDbContext CreateDbContext(DbContextOptionsBuilder<InventoryDbContext> optionsBuilder)
    {
        return new InventoryDbContext(optionsBuilder.Options);
    }
}