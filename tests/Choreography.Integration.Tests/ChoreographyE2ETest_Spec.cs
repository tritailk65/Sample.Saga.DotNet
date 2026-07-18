using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;

namespace Choreography.Integration.Tests;

[TestFixture]
public class ChoreographyE2ETest()
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
                x.AddConsumer<OrderCreateEventHandling>();
                x.AddConsumer<OrderCreateEventSuccessHandling>();
                x.AddConsumer<InventoryGoodsBookedInWarehouseEventSuccessHandling>(); 
                x.AddConsumer<DeliverySendEventSuccessHandling>();
                x.AddConsumer<InventoryGoodsBookedInWarehouseEventFailedHandling>();
                x.AddConsumer<DeliverySendEventFailedHandling>();
                x.AddConsumer<InventoryGoodsRetoredEventSuccessHandling>();
                
            })
            .AddScoped<IOrderService, OrderServiceImplement>()
            .AddScoped<IInventoryService, InventoryServiceImplement>()
            .AddScoped<IDeliveryService, DeliveryServiceImplement>()
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
        var orderDb = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        
        await orderDb.Database.EnsureDeletedAsync(); 
        await orderDb.Database.MigrateAsync(); 

        var iDb = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        await iDb.Database.EnsureDeletedAsync();
        await iDb.Database.MigrateAsync();

        var dDb = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
        await dDb.Database.EnsureDeletedAsync();
        await dDb.Database.MigrateAsync();
    }

    [Test]
    public async Task E2E_Choreography_HappyPath_Should_Process_Order_And_Deduct_Inventory()
    {
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

        await _harness.Bus.Publish(new OrderCreateEvent( UserId, cartItems, address));

        // Step 2: Create Order
        Assert.That(await _harness.Consumed.Any<OrderCreateEvent>(), Is.True, "Message create order not consumed");
        Assert.That(await _harness.Published.Any<OrderCreateEventSuccess>(), Is.True, "Order không bắn Event Success");

        // Step 3: Get OrderId from OrderCreateEventSuccess
        var orderSuccessEvent = _harness.Published.Select<OrderCreateEventSuccess>().FirstOrDefault();
        Assert.That(orderSuccessEvent, Is.Not.Null, "Không lấy được OrderCreateEventSuccess message");
        var actualOrderId = orderSuccessEvent.Context.Message.OrderId;

        // Step 4: Check consum message OrderCreateEventSuccess in Inventory service
        Assert.That(await _harness.Consumed.Any<OrderCreateEventSuccess>(), Is.True, "Inventory chưa tiêu thụ Event");
        Assert.That(await _harness.Published.Any<InventoryGoodsBookedInWarehouseEventSuccess>(), Is.True, "Inventory bị lỗi trừ kho");

        // Step 5: Check add delivery success 
        Assert.That(await _harness.Published.Any<DeliverySendEventSuccess>(), Is.True, "");
        Assert.That(await _harness.Consumed.Any<DeliverySendEventSuccess>(), Is.True, "");

        using var assertScope = _provider.CreateScope();
        var orderDb = assertScope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var inventoryDb = assertScope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var deliveryDb = assertScope.ServiceProvider.GetRequiredService<DeliveryDbContext>();

        var order_in_db = await orderDb.Orders.FirstOrDefaultAsync(o => o.Id == actualOrderId);
        Assert.That(order_in_db, Is.Not.Null);
        Assert.That(order_in_db.Status, Is.EqualTo(OrderStatus.Completed));

        var goods_in_db = await inventoryDb.Goods.FirstOrDefaultAsync(g => g.Id == Good.Id);
        Assert.That(goods_in_db?.Id, Is.EqualTo(Good.Id));
        Assert.That(goods_in_db?.Count, Is.EqualTo(0));

        var delivery_in_db = await deliveryDb.Deliveries.FirstOrDefaultAsync(o => o.OrderId == actualOrderId);
        Assert.That(delivery_in_db?.GoodIds, Is.EqualTo(new List<Guid>{Good.Id}));
        Assert.That(delivery_in_db?.UserId, Is.EqualTo(UserId));
    }


    [Test]
    public async Task E2E_Choreography_SadPath_Should_Cancel_Order_When_Inventory_Out_Of_Stock()
    {
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
                Count = 0   // Zero --> out of stock
            };
            _db.Goods.Add(good);
            await _db.SaveChangesAsync();
        }
      
        await _harness.Bus.Publish(new OrderCreateEvent( UserId, cartItems, address));

        // Step 2: Create Order
        Assert.That(await _harness.Consumed.Any<OrderCreateEvent>(), Is.True, "Message create order not consumed");
        Assert.That(await _harness.Published.Any<OrderCreateEventSuccess>(), Is.True, "Order không bắn Event Success");

        // Step 3: Get OrderId from OrderCreateEventSuccess
        var orderSuccessEvent = _harness.Published.Select<OrderCreateEventSuccess>().FirstOrDefault();
        Assert.That(orderSuccessEvent, Is.Not.Null, "Không lấy được OrderCreateEventSuccess message");
        var actualOrderId = orderSuccessEvent.Context.Message.OrderId;

        // Step 4: Check consum message OrderCreateEventSuccess in Inventory service
        Assert.That(await _harness.Consumed.Any<OrderCreateEventSuccess>(), Is.True, "Inventory chưa tiêu thụ Event");
        Assert.That(await _harness.Published.Any<InventoryGoodsBookedInWarehouseEventFailed>(), Is.True, "Lỗi hàm check trừ kho");

        // Waiting still message inventory book goods fails
        Assert.That(await _harness.Consumed.Any<InventoryGoodsBookedInWarehouseEventFailed>(), Is.True, "");

        using var assertScope = _provider.CreateScope();
        var orderDb = assertScope.ServiceProvider.GetRequiredService<OrderDbContext>();

        var order_in_db = await orderDb.Orders.FirstOrDefaultAsync(o => o.Id == actualOrderId);
        Assert.That(order_in_db?.Status, Is.EqualTo(OrderStatus.Cancled));
    }

    [Test]
    public async Task E2E_choreography_sadpath_should_refill_inventory_and_cancel_order_when_delivery_send_fail()
    {
        var cartItems = new List<GoodViewModel>() { Good };
        var address = "Invalid Address"; 

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
      
        await _harness.Bus.Publish(new OrderCreateEvent( UserId, cartItems, address));

        using var assertScope = _provider.CreateScope();
        var orderDb = assertScope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var inventoryDb = assertScope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var deliveryDb = assertScope.ServiceProvider.GetRequiredService<DeliveryDbContext>();

        Assert.That(await _harness.Consumed.Any<OrderCreateEvent>(), Is.True, "Message create order not consumed");
        Assert.That(await _harness.Published.Any<OrderCreateEventSuccess>(), Is.True, "Order không bắn Event Success");

        var orderSuccessEvent = _harness.Published.Select<OrderCreateEventSuccess>().FirstOrDefault();
        Assert.That(orderSuccessEvent, Is.Not.Null, "Không lấy được OrderCreateEventSuccess message");
        var actualOrderId = orderSuccessEvent.Context.Message.OrderId;

        Assert.That(await _harness.Consumed.Any<OrderCreateEventSuccess>(), Is.True, "Inventory chưa tiêu thụ Event");
        Assert.That(await _harness.Published.Any<InventoryGoodsBookedInWarehouseEventSuccess>(), Is.True, "Lỗi hàm check trừ kho");

        // Waiting still message inventory book goods fails
        Assert.That(await _harness.Published.Any<DeliverySendEventFailed>(), Is.True, "");
        Assert.That(await _harness.Consumed.Any<DeliverySendEventFailed>(), Is.True, "");

        Assert.That(await _harness.Published.Any<InventoryGoodsRestoredEventSuccess>(), Is.True);
        Assert.That(await _harness.Consumed.Any<InventoryGoodsRestoredEventSuccess>(), Is.True);
        
        var delivery_in_db = await deliveryDb.Deliveries.FirstOrDefaultAsync(o => o.OrderId == actualOrderId);
        Assert.That(delivery_in_db, Is.Null);

        var goods_in_db = await inventoryDb.Goods.FirstOrDefaultAsync(g => g.Id == Good.Id);
        Assert.That(goods_in_db?.Id, Is.EqualTo(Good.Id));
        Assert.That(goods_in_db?.Count, Is.EqualTo(1)); // Goods return -> count = 1

        var order_in_db = await orderDb.Orders.FirstOrDefaultAsync(o => o.Id == actualOrderId);
        Assert.That(order_in_db, Is.Not.Null);
        Assert.That(order_in_db.Status, Is.EqualTo(OrderStatus.Cancled));
    }

}