namespace Choreography.Tests.NUnit;

using Choreography.Order.IntegrationEvent.EventHandling;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.Contracts;
using Shared.Infrastructure.Order.Infrastructure;
using Shared.Models;
using Shared.Services.Order;

[TestFixture]
public class OrderCreateEventHandlingTests
{
    private Mock<IOrderService> _mockService;
    private ServiceProvider _provider;
    private ITestHarness _harness;

    #region Setup and TearDown

    [SetUp]
    public async Task SetUp()
    {
        _mockService = new Mock<IOrderService>();
        _provider = new ServiceCollection()
            .ConfigureMassTransit(x =>
            {       
                x.AddConsumer<OrderCreateEventHandling>();
                x.AddConsumer<InventoryGoodsBookedInWarehouseEventFailedHandling>();
            })
            .AddDbContext<OrderDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString());
            })
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
    public async Task Should_publish_success_event_when_added()
    {
        var orderId = NewId.NextGuid();

        var cartItems = new List<GoodViewModel>() { Good };
        var address = "7811 NE Pleasant Valley RdLiberty, Missouri(MO), 64068";

        await _harness.Bus.Publish(new OrderCreateEvent( orderId, UserId, cartItems, address));

        Assert.That(await _harness.Consumed.Any<OrderCreateEvent>(), "Message create order not consumed");

        Assert.That(await _harness.Published.Any<OrderCreateEventSuccess>(), Is.True);
    }

    [Test]
    public async Task Should_publish_failed_event_when_added_error()
    {   
        var orderId = NewId.NextGuid();
        var cartItems = new List<GoodViewModel>();  // Empty basket
        var address = "7811 NE Pleasant Valley RdLiberty, Missouri(MO), 64068";

        _mockService
            .Setup(x => x.InsertAsync(It.IsAny<OrderCreationModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Add to db failed"));

        await _harness.Bus.Publish(new OrderCreateEvent(orderId, UserId, cartItems, address));

        Assert.That(await _harness.Consumed.Any<OrderCreateEvent>(), Is.True);

        Assert.That(await _harness.Published.Any<OrderCreateEventFailed>(), Is.True);

    }

    [Test]
    public async Task Should_call_delete_async_when_inventory_failed_event_recived()
    {
        await _harness.Bus.Publish(new InventoryGoodsBookedInWarehouseEventFailed(Good.Id, new List<GoodViewModel>{Good}));

        Assert.That(await _harness.Consumed.Any<InventoryGoodsBookedInWarehouseEventFailed>(), Is.True);

        _mockService
            .Setup(x => x.DeleteAsync(Good.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockService.Verify(
            x => x.UpdateStatusCancel(Good.Id, It.IsAny<CancellationToken>()), 
            Times.Once,
            "The DeleteAsync method was not called or was called with the wrong OrderId.");
    }

}