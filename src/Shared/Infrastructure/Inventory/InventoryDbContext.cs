namespace Shared.Infrastructure.Inventory.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Inventory.Infrastructure.Entities;

public class InventoryDbContext : DbContext
{
    public DbSet<Goods> Goods { get; set; }

    // For Unit-test
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    // protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    // {
    //     optionsBuilder.UseInMemoryDatabase(databaseName: "InventoryDb");
    // }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

    }


}