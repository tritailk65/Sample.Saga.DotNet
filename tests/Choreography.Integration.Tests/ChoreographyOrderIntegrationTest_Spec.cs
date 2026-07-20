namespace Choreography.Integration.Tests;

[TestFixture]
public class OrderCreateIntegrationTest
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
                            .Where(a => a.FullName.Contains("Choreography.Order"))
                            .ToArray();

                x.AddConsumers(assembly);
            })
            .AddScoped<IOrderService, OrderServiceImplement>()
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

    private async Task InitializeDatabasesAsync()
    {
        using var scope = _provider.CreateScope();
        var orderDb = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        
        await orderDb.Database.EnsureDeletedAsync(); 
        await orderDb.Database.MigrateAsync(); 
    }

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

    [Test]
    public async Task Should_publish_success_event_when_added_order_to_db()
    {
        var orderId = NewId.NextGuid();
        var cartItems = new List<GoodViewModel>() { Good };
        var address = "7811 NE Pleasant Valley RdLiberty, Missouri(MO), 64068";

        await _harness.Bus.Publish(new OrderCreateEvent(orderId, UserId, cartItems, address));

        Assert.That(await _harness.Consumed.Any<OrderCreateEvent>(), "Message create order not consumed");

        using var scope = _provider.CreateScope();
        var _db = scope.ServiceProvider.GetRequiredService<OrderDbContext>(); 

        var savedOrder = await _db.Orders.FirstOrDefaultAsync();
        Assert.That(savedOrder, Is.Not.Null);
        Assert.That(savedOrder.DeliveryAddress, Is.EqualTo(address));

        Assert.That(await _harness.Published.Any<OrderCreateEventSuccess>(), Is.True);
    }

}