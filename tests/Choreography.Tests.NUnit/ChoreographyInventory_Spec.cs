using Choreography.Inventory.Services;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.Contracts;


namespace Choreography.Tests.NUnit;

[TestFixture]
public class InventoryEventHandlingTesting
{
    private Mock<IInventoryService> _mockService;
    private ServiceProvider _provider;
    private ITestHarness _harness;

    #region Setup and TearDown

    [SetUp]
    public async Task SetUp()
    {
        _mockService = new Mock<IInventoryService>();

        _provider = new ServiceCollection()
            .ConfigureMassTransit(x =>
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                            .Where(a => a.FullName.Contains("Choreography.Inventory"))
                            .ToArray();

                x.AddConsumers(assembly);

                //x.AddConsumer<Choreography.Inventory.IntegrationeEvent.EventHandling.OrderCreateEventSuccessHandling>();
            })
            // .AddDbContext<ApplicationDbContext>(options =>
            // {
            //     options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString());
            // })
            .AddSingleton(_mockService.Object)
            //.AddSingleton(Mock.Of<ILogger<OrderCreateEventSuccessHandling>>())
            // .AddScoped<IInventoryService, InventoryServiceImplement>() 
            .BuildServiceProvider(true);

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

    [Test]
    public async Task Should_publish_event_when_added_goods_success()
    {
        var cartItems = new List<GoodViewModel>() {Good} ;
        var address = "7811 NE Pleasant Valley RdLiberty, Missouri(MO), 64068";

        // Nhap kho
        // using var scope = _provider.CreateScope();
        // var _db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // var good = new Goods
        // {
        //     Id = Good.Id,
        //     Name = Good.Name,
        //     Count = Good.Count  
        // };
        // _db.Goods.Add(good);
        // await _db.SaveChangesAsync();

        // Không nên viết như này vì nó thuộc về integration test

        var availableGood = new Dictionary<Guid, bool>
        {
            { Good.Id, true }
        };

        _mockService
            .Setup(x => x.CheckAvailabilityAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(availableGood);

        _mockService
                .Setup(x => x.BookGoodsAsync(It.IsAny<Dictionary<Guid, int>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        await _harness.Bus.Publish(new OrderCreateEventSuccess(Good.Id, UserId, cartItems, address));

        Assert.That(await _harness.Consumed.Any<OrderCreateEventSuccess>(),  "Message create order success not consumed");

        Assert.That(await _harness.Published.Any<InventoryGoodsBookedInWarehouseEventSuccess>(
            e => e.Context.Message.OrderId == Good.Id), Is.True, "Success event was not published.");
        
        Assert.That(await _harness.Published.Any<InventoryGoodsBookedInWarehouseEventFailed>(), Is.False);
    }

    [Test]
    public async Task Should_publish_failed_event_when_inventory_exception_occurs()
    {
        var cartItems = new List<GoodViewModel>() {Good} ;
        var address = "7811 NE Pleasant Valley RdLiberty, Missouri(MO), 64068";

        var availableGood = new Dictionary<Guid, bool>
        {
            { Good.Id, true }
        };

        _mockService
            .Setup(x => x.CheckAvailabilityAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(availableGood);

        _mockService
            .Setup(x => x.BookGoodsAsync(It.IsAny<Dictionary<Guid, int>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection timeout or Out of stock!"));

        await _harness.Bus.Publish(new OrderCreateEventSuccess(Good.Id, UserId, cartItems, address));

        Assert.That(await _harness.Consumed.Any<OrderCreateEventSuccess>(),  "Message create order success not consumed");

        Assert.That(await _harness.Published.Any<InventoryGoodsBookedInWarehouseEventFailed>(
                    e => e.Context.Message.OrderId == Good.Id), Is.True, "Failed event was not published.");

        Assert.That(await _harness.Published.Any<InventoryGoodsBookedInWarehouseEventSuccess>(), Is.False);
    }


    [Test]
    public async Task Should_call_add_good_count_async_when_inventory_failed_event_recived()
    {
        var cartItems = new List<GoodViewModel>() {Good} ;
        await _harness.Bus.Publish(new DeliverySendEventFailed(Good.Id, cartItems));

        Assert.That(await _harness.Consumed.Any<DeliverySendEventFailed>(), Is.True);

        var cartDeliveryFailed = new Dictionary<Guid, int>
        {
            { Good.Id, Good.Count }
        };

        _mockService
            .Setup(x => x.AddGoodsCountAsync(cartDeliveryFailed, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockService.Verify(
            x => x.AddGoodsCountAsync(cartDeliveryFailed, It.IsAny<CancellationToken>()), 
            Times.Once,
            "The AddGoodsCountAsync method was not called or was called with the wrong card.");
    }

}