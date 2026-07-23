using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orchestration.Delivery.IntegrationEvent.EventHandling;
using Orchestration.Inventory.IntegrationEvent.EventHandling;
using Orchestration.Order.IntegrationeEvent.EventHandling;
using Shared.Contracts;
using Shared.Infrastructure.Delivery.Infrastructure;
using Shared.Infrastructure.Inventory.Infrastructure;
using Shared.Infrastructure.Inventory.Infrastructure.Entities;
using Shared.Infrastructure.Order.Infrastructure;
using Shared.Infrastructure.Order.Infrastructure.Enums;
using Shared.Infrastructure.OrderSaga;
using Shared.Services.Delivery.Services;
using Shared.Services.Inventory.Services;
using Shared.Services.Order;
using Shared.StateMachines;
using OrderEntity = Shared.Infrastructure.Order.Infrastructure.Entities.Order;

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
                x.AddConsumer<OrderCreateEventHandling>();
                x.AddConsumer<InventoryGoodsBookedInWarehouseEventHandling>();
                x.AddConsumer<DeliverySendEventHandling>();
                x.AddConsumer<DeliverySendEventSuccessHandling>();
                x.AddConsumer<OrderCancelEventHandling>();
                x.AddConsumer<InventoryGoodsRestoredEventHandling>();
                x.AddConsumer<InventoryGoodsBookedRejectedHandling>();
            })
            .AddScoped<IOrderService, OrderServiceImplement>()
            .AddScoped<IInventoryService, InventoryServiceImplement>()
            .AddScoped<IDeliveryService, DeliveryServiceImplement>()
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
        var sagaDb = scope.ServiceProvider.GetRequiredService<OrderSagaDbContext>();
        
        await sagaDb.Database.EnsureDeletedAsync(); 
        await sagaDb.Database.MigrateAsync(); 

        var orderDb = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        await orderDb.Database.EnsureDeletedAsync();
        await orderDb.Database.EnsureCreatedAsync();

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
        Assert.That(await _harness.Published.Any<OrderCreateEventSuccess>(), Is.True, "Order service did not publish success");
        Assert.That(await _harness.Published.Any<InventoryGoodsBookedInWarehouseEvent>(), Is.True, "Saga did not request inventory booking");
        Assert.That(await _harness.Published.Any<InventoryGoodsBookedInWarehouseEventSuccess>(), Is.True, "Inventory service did not publish success");
        Assert.That(await _harness.Published.Any<DeliverySendEvent>(), Is.True, "Saga did not request delivery");
        Assert.That(await _harness.Published.Any<DeliverySendEventSuccess>(), Is.True, "Delivery service did not publish success");
        Assert.That(await _harness.Consumed.Any<DeliverySendEventSuccess>(), Is.True, "order service did not consume success");


        Assert.That((await _sagaHarness.Exists(orderId, x => x.Final)).HasValue, Is.True, "Saga did not reach final state");

        var orderInDb = await WaitForOrderStatusAsync(orderId, OrderStatus.Completed);
        Assert.That(orderInDb, Is.Not.Null);
        Assert.That(orderInDb!.Status, Is.EqualTo(OrderStatus.Completed));

        using var assertScope = _provider.CreateScope();
        var inventoryDb = assertScope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var deliveryDb = assertScope.ServiceProvider.GetRequiredService<DeliveryDbContext>();

        var goodsInDb = await inventoryDb.Goods.FirstOrDefaultAsync(g => g.Id == Good.Id);
        Assert.That(goodsInDb, Is.Not.Null);
        Assert.That(goodsInDb!.Count, Is.EqualTo(0));

        var deliveryInDb = await deliveryDb.Deliveries.FirstOrDefaultAsync(o => o.OrderId == orderId);
        Assert.That(deliveryInDb, Is.Not.Null);
        Assert.That(deliveryInDb!.GoodIds, Is.EqualTo(new List<Guid>{Good.Id}));
        Assert.That(deliveryInDb.UserId, Is.EqualTo(UserId));

    }

    [Test]
    public async Task E2E_Orchestration_SadPath_Should_Rejected_Order_When_Inventory_Out_Of_Stock()
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
                Count = 0
            };
            _db.Goods.Add(good);
            await _db.SaveChangesAsync();
        }

        await _harness.Bus.Publish(new OrderCreateEvent(orderId, UserId, cartItems,address));

        Assert.That(await _harness.Consumed.Any<OrderCreateEvent>(), Is.True, "Message not consumed");
        Assert.That(await _harness.Published.Any<OrderCreateEventSuccess>(), Is.True, "Order service did not publish success");
        Assert.That(await _harness.Published.Any<InventoryGoodsBookedInWarehouseEvent>(), Is.True, "Saga did not request inventory booking");
        Assert.That(await _harness.Published.Any<InventoryGoodsBookedRejectedEvent>(), Is.True, "Inventory service did not publish rejected when out of stock");
        Assert.That(await _harness.Consumed.Any<InventoryGoodsBookedRejectedEvent>(), Is.True, "Inventory service did not publish rejected when out of stock");


        var orderInDb = await WaitForOrderStatusAsync(orderId, OrderStatus.Rejected);
        Assert.That(orderInDb, Is.Not.Null);
        Assert.That(orderInDb!.Status, Is.EqualTo(OrderStatus.Rejected));

        using var assertScope = _provider.CreateScope();
        var inventoryDb = assertScope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        var goodsInDb = await inventoryDb.Goods.FirstOrDefaultAsync(g => g.Id == Good.Id);
        Assert.That(goodsInDb, Is.Not.Null);
        Assert.That(goodsInDb!.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task E2E_Orchestration_sadpath_should_refill_inventory_and_cancel_order_when_delivery_send_fail()
    {
        var orderId = NewId.NextGuid();
        var cartItems = new List<GoodViewModel>() { Good };

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

        await _harness.Bus.Publish(new OrderCreateEvent(orderId, UserId, cartItems, "Invalid Address"));

        Assert.That(await _harness.Consumed.Any<OrderCreateEvent>(), Is.True, "Message not consumed");
        Assert.That(await _harness.Published.Any<OrderCreateEventSuccess>(), Is.True, "Order service did not publish success");
        Assert.That(await _harness.Published.Any<InventoryGoodsBookedInWarehouseEvent>(), Is.True, "Saga did not request inventory booking");
        Assert.That(await _harness.Published.Any<InventoryGoodsBookedInWarehouseEventSuccess>(), Is.True, "Inventory service did not publish success");
        Assert.That(await _harness.Published.Any<DeliverySendEvent>(), Is.True, "Saga did not request delivery");
        Assert.That(await _harness.Published.Any<DeliverySendEventFailed>(), Is.True, "Delivery service did not publish fail");
        Assert.That(await _harness.Consumed.Any<OrderCancelEvent>(), Is.True);
        Assert.That(await _harness.Consumed.Any<InventoryGoodsRestoredEvent>(), Is.True);

        

        var orderInDb = await WaitForOrderStatusAsync(orderId, OrderStatus.Cancled);
        var goodsInDb = await WaitForInventoryCountAsync(Good.Id, 1);

        Assert.That(orderInDb, Is.Not.Null);
        Assert.That(orderInDb!.Status, Is.EqualTo(OrderStatus.Cancled));

        using var assertScope = _provider.CreateScope();
        var deliveryDb = assertScope.ServiceProvider.GetRequiredService<DeliveryDbContext>();

        var deliveryInDb = await deliveryDb.Deliveries.FirstOrDefaultAsync(o => o.OrderId == orderId);
        Assert.That(deliveryInDb, Is.Null);

        Assert.That(goodsInDb, Is.Not.Null);
        Assert.That(goodsInDb!.Count, Is.EqualTo(1));
    }

    private async Task<OrderEntity> WaitForOrderStatusAsync(Guid orderId, OrderStatus expectedStatus)
    {
        OrderEntity order = null;
        var deadline = DateTime.UtcNow.AddSeconds(5);

        while (DateTime.UtcNow < deadline)
        {
            using var scope = _provider.CreateScope();
            var orderDb = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            order = await orderDb.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId);

            if (order?.Status == expectedStatus)
            {
                return order;
            }

            await Task.Delay(50);
        }

        return order;
    }

    private async Task<Goods> WaitForInventoryCountAsync(Guid goodId, int expectedCount)
    {
        Goods goods = null;
        var deadline = DateTime.UtcNow.AddSeconds(5);

        while (DateTime.UtcNow < deadline)
        {
            using var scope = _provider.CreateScope();
            var inventoryDb = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            goods = await inventoryDb.Goods.AsNoTracking().FirstOrDefaultAsync(g => g.Id == goodId);

            if (goods?.Count == expectedCount)
            {
                return goods;
            }

            await Task.Delay(50);
        }

        return goods;
    }


}
