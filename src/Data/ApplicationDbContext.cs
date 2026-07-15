
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class ApplicationDbContext : DbContext
{
//     public DbSet<Card> Cards { get; set; }
    public DbSet<Order> Orders { get; set; }
    // public DbSet<Good> Goods { get; set; }
    // public DbSet<Delivery> Deliveries { get; set; }

    // For Unit-test
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase(databaseName: "AuthorDb");
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