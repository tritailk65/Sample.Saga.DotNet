namespace Choreography.Tests.NUnit;

using Choreography.Order.Infrastructure;
using Choreography.Order.IntegrationEvent.Events;
using Choreography.Order.Models;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

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
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                            .Where(a => a.FullName.Contains("Choreography.Order"))
                            .ToArray();

                x.AddConsumers(assembly);
            })
            .AddDbContext<OrderDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString());
            })
            // .AddScoped<IOrderService, OrderServiceImplement>()
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

        var cartItems = new List<GoodViewModel>() { OrderConstans.Good };
        var address = "7811 NE Pleasant Valley RdLiberty, Missouri(MO), 64068";

        await _harness.Bus.Publish(new OrderCreateEvent( OrderConstans.UserId, cartItems, address));

        Assert.That(await _harness.Consumed.Any<OrderCreateEvent>(), "Message create order not consumed");

        // using var scope = _provider.CreateScope();
        // var _db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // var savedOrder = await _db.Orders.FirstOrDefaultAsync();
        // Assert.That(savedOrder, Is.Not.Null);
        // Assert.That(savedOrder.DeliveryAddress, Is.EqualTo(address));

        // Assert.Multiple(() => 
        // {
        //     Assert.That(savedOrder, Is.Not.Null);
        //     Assert.That(savedOrder.DeliveryAddress, Is.EqualTo(address));
        // });

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

        await _harness.Bus.Publish(new OrderCreateEvent( OrderConstans.UserId, cartItems, address));

        Assert.That(await _harness.Consumed.Any<OrderCreateEvent>(), Is.True);

        Assert.That(await _harness.Published.Any<OrderCreateEventFailed>(), Is.True);

    }

    [Test]
    public async Task Should_call_delete_async_when_inventory_failed_event_recived()
    {
        await _harness.Bus.Publish(new InventoryGoodsBookedInWarehouseEventFailed(Good.Id));

        Assert.That(await _harness.Consumed.Any<InventoryGoodsBookedInWarehouseEventFailed>(), Is.True);

        _mockService
            .Setup(x => x.DeleteAsync(Good.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockService.Verify(
            x => x.DeleteAsync(Good.Id, It.IsAny<CancellationToken>()), 
            Times.Once,
            "The DeleteAsync method was not called or was called with the wrong OrderId.");
    }

}