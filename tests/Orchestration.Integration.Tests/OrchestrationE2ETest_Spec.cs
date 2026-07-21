using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts;
using Shared.Infrastructure.Delivery.Infrastructure;
using Shared.Infrastructure.Inventory.Infrastructure;
using Shared.Infrastructure.Inventory.Infrastructure.Entities;
using Shared.Infrastructure.OrderSaga;
using Shared.StateMachines;

namespace Orchestration.Integration.Tests;

[TestFixture]
public class OrchestrationE2ETest
{
    private ServiceProvider _provider;
    private ITestHarness _harness;
    private ISagaStateMachineTestHarness<OrderStateMachine, OrderSaga> _sagaHarness = null!;

    #region Setup and TearDown

    [SetUp]
    public async Task SetUp()
    {
        _provider = new ServiceCollection()
            .ConfigureMassTransit(x =>
            {
                x.AddSagaStateMachine<OrderStateMachine,OrderSaga>();

            })
            .BuildServiceProvider(true);

        await InitializeDatabasesAsync();

        _harness = _provider.GetTestHarness();

        await _harness.Start();

        _sagaHarness = _harness.GetSagaStateMachineHarness<OrderStateMachine, OrderSaga>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
    }

    private async Task InitializeDatabasesAsync()
    {
        using var scope = _provider.CreateScope();
        var orderDb = scope.ServiceProvider.GetRequiredService<OrderSagaDbContext>();
        
        await orderDb.Database.EnsureDeletedAsync(); 
        await orderDb.Database.MigrateAsync(); 

        var iDb = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await iDb.Database.EnsureDeletedAsync();
        await iDb.Database.MigrateAsync();

        var dDb = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
        await dDb.Database.EnsureDeletedAsync();
        await dDb.Database.MigrateAsync();
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
    public async Task E2E_Orchestration_HappyPath_Should_Process_Order_And_Deduct_Inventory()
    {
        var orderId = NewId.NextGuid();
        var cartItems = new List<GoodViewModel>() { Good };
        var address = "7811 NE Pleasant Valley RdLiberty, Missouri(MO), 64068";

        // Step 1: Add inventory
        using (var scope = _provider.CreateScope())
        {
            var _db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            
            var good = new Goods
            {
                Id = Good.Id,
                Name = Good.Name,
                Count = Good.Count
            };
            _db.Goods.Add(good);
            await _db.SaveChangesAsync();
        }

        await _harness.Bus.Publish(new OrderCreateEvent(orderId, UserId, cartItems,address));

        Assert.That(await _harness.Consumed.Any<OrderCreateEvent>(), Is.True, "Message not consumed");

        // await _harness.Bus.Publish(new OrderCreateEventSuccess(orderId, UserId, cartItems, address));
        // await _harness.Bus.Publish(new InventoryGoodsBookedInWarehouseEvent(orderId, UserId, cartItems,address));

        // await _harness.Bus.Publish(new InventoryGoodsBookedInWarehouseEvent(orderId, UserId, cartItems,address));
        // await _harness.Bus.Publish(new InventoryGoodsBookedInWarehouseEventSuccess(orderId, UserId, cartItems, address));
        // await _harness.Bus.Publish(new DeliverySendEventFailed(orderId, cartItems));

        // Assert.That((await _sagaHarness.Exists(orderId, x => x.Canceled)).HasValue, Is.True, "Saga must be in Canceled state first");

        // Assert.That(await _harness.Published.Any<OrderCancelEvent>(), Is.True);

        // using var assertScope = _provider.CreateScope();
        // var orderDb = assertScope.ServiceProvider.GetRequiredService<OrderDbContext>();
        // var inventoryDb = assertScope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        // var deliveryDb = assertScope.ServiceProvider.GetRequiredService<DeliveryDbContext>();

        // var order_in_db = await orderDb.Orders.FirstOrDefaultAsync(o => o.Id == actualOrderId);
        // Assert.That(order_in_db, Is.Not.Null);
        // Assert.That(order_in_db.Status, Is.EqualTo(OrderStatus.Completed));

        // var goods_in_db = await inventoryDb.Goods.FirstOrDefaultAsync(g => g.Id == Good.Id);
        // Assert.That(goods_in_db?.Id, Is.EqualTo(Good.Id));
        // Assert.That(goods_in_db?.Count, Is.EqualTo(0));

        // var delivery_in_db = await deliveryDb.Deliveries.FirstOrDefaultAsync(o => o.OrderId == actualOrderId);
        // Assert.That(delivery_in_db?.GoodIds, Is.EqualTo(new List<Guid>{Good.Id}));
        // Assert.That(delivery_in_db?.UserId, Is.EqualTo(UserId));
    }


}