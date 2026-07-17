namespace Choreography.Integration.Tests;

[TestFixture]
public class InventoryCreateIntegrationTest
{
    private ServiceProvider _provider;
    private ITestHarness _harness;

    #region Setup and TearDown

    [SetUp]
    public async Task SetUp()
    {
        _provider = new ServiceCollection()
            .ConfigureMassTransit(x =>
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                            .Where(a => a.FullName.Contains("Choreography.Inventory"))
                            .ToArray();

                x.AddConsumers(assembly);
            })
            // .AddHostedService<MigrationHostedService<InventoryDbContext>>()
            .AddScoped<IInventoryService, InventoryServiceImplement>()
            .BuildServiceProvider(true);

        await InitializeDatabasesAsync();

        _harness = _provider.GetTestHarness();

        await _harness.Start();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_harness != null)
        {
            await _harness.Stop();
        }

        if (_provider != null)
        {
            await _provider.DisposeAsync();
        }
    }
    #endregion

    #region Mock Data
    public static readonly Guid UserId = Guid.Parse("b185922e-3061-49a1-a9e6-28521eeca2f9");

    public static readonly GoodViewModel Good = new()
    {
        Id = Guid.Parse("cf7c1502-a22b-4d0a-8f95-5b802e2f7948"),
        Name = "Product",
        Count = 1,
        Price = 100,
    };
    #endregion

    private async Task InitializeDatabasesAsync()
    {
        using var scope = _provider.CreateScope();
        var iDb = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        
        await iDb.Database.EnsureDeletedAsync(); 
        await iDb.Database.MigrateAsync(); 
    }

    [Test]
    public async Task Should_publish_success_event_when_added_inventory_to_db()
    {
        var cartItems = new List<GoodViewModel>() { Good };
        var address = "7811 NE Pleasant Valley RdLiberty, Missouri(MO), 64068";

        // Nhap kho
        using var scope = _provider.CreateScope();
        var _db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        var good = new Goods
        {
            Id = Good.Id,
            Name = Good.Name,
            Count = Good.Count
        };
        _db.Goods.Add(good);
        await _db.SaveChangesAsync();

    
        await _harness.Bus.Publish(new OrderCreateEventSuccess(Good.Id, UserId, cartItems, address));

        Assert.That(await _harness.Consumed.Any<OrderCreateEventSuccess>(),  "Message create order success not consumed");

        #region Check record in database
        var savedGoods = await _db.Goods.FirstOrDefaultAsync();
        Assert.That(savedGoods, Is.Not.Null);
        Assert.That(savedGoods.Count, Is.EqualTo(Good.Count));   // Order full 
        #endregion
        // Assert.That(await _harness.Published.Any<OrderCreateEventSuccess>(), Is.True);

        // Assert.That(await _harness.Published.Any<InventoryGoodsBookedInWarehouseEventSuccess>(
        //     e => e.Context.Message.OrderId == Good.Id), Is.True, "Success event was not published.");
        
        // Assert.That(await _harness.Published.Any<InventoryGoodsBookedInWarehouseEventFailed>(), Is.False);
    }

}