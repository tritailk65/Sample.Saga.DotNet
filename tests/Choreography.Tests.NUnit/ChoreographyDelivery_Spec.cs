using Choreography.Delivery.Services;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.Contracts;

namespace Choreography.Tests.NUnit;

[TestFixture]
public class DeliveryEventHandlingTesting
{
    private Mock<IDeliveryService> _mockService;
    private ServiceProvider _provider;
    private ITestHarness _harness;

    #region Setup and TearDown
    [SetUp]
    public async Task SetUp()
    {
        _mockService = new Mock<IDeliveryService>();

        _provider = new ServiceCollection()
            .ConfigureMassTransit(x =>
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                            .Where(a => a.FullName.Contains("Choreography.Delivery"))
                            .ToArray();

                x.AddConsumers(assembly);

                //x.AddConsumer<Choreography.Inventory.IntegrationeEvent.EventHandling.OrderCreateEventSuccessHandling>();
            })
            // Uncomment this if need to test exit record in db
            // .AddDbContext<ApplicationDbContext>(options =>
            // {
            //     options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString());
            // })
            .AddSingleton(_mockService.Object)

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
    public async Task Should_publish_event_when_added_delivery_success()
    {
        var cartItems = new List<GoodViewModel>() {Good} ;
        var address = "7811 NE Pleasant Valley RdLiberty, Missouri(MO), 64068";

        _mockService
            .Setup(x => x.SendPackageAsync(It.IsAny<Guid>(), It.IsAny<IList<Guid>>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _harness.Bus.Publish(new InventoryGoodsBookedInWarehouseEventSuccess(Good.Id, UserId, cartItems, address));

        Assert.That(await _harness.Consumed.Any<InventoryGoodsBookedInWarehouseEventSuccess>(),  "Message create order success not consumed");

        Assert.That(await _harness.Published.Any<DeliverySendEventSuccess>(
                    e => e.Context.Message.OrderId == Good.Id), Is.True, "Success event was not published.");

        Assert.That(await _harness.Published.Any<DeliverySendEventFailed>(), Is.False);
    }

    [Test]
    public async Task Should_publish_event_when_add_delivery_failed()
    {
        var cartItems = new List<GoodViewModel>() {Good} ;
        var address = "7811 NE Pleasant Valley RdLiberty, Missouri(MO), 64068";

        _mockService
            .Setup(x => x.SendPackageAsync(It.IsAny<Guid>(), It.IsAny<IList<Guid>>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException($"{Good.Name} cannot be equal zero items"));

        await _harness.Bus.Publish(new InventoryGoodsBookedInWarehouseEventSuccess(Good.Id, UserId, cartItems, address));

        Assert.That(await _harness.Consumed.Any<InventoryGoodsBookedInWarehouseEventSuccess>(),  "Message create order success not consumed");

        Assert.That(await _harness.Published.Any<DeliverySendEventFailed>(
                    e => e.Context.Message.OrderId == Good.Id), Is.True, "Failed event was not published.");

        Assert.That(await _harness.Published.Any<DeliverySendEventSuccess>(), Is.False);
    }

}