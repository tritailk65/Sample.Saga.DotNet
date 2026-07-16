using Choreography.Inventory.Infrastructure;
using Choreography.Inventory.Infrastructure.Entities;
using Choreography.Inventory.IntegrationeEvent.Events;
using Choreography.Inventory.Services;
using Choreography.Order.Infrastructure;
using Choreography.Order.Infrastructure.Enums;
using Choreography.Order.IntegrationEvent.Events;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                            .Where(a => a.FullName.Contains("Choreography.Order") 
                                        && a.FullName.Contains("Choreography.Inventory") 
                                        && a.FullName.Contains("Choreography.Delivery"))
                            .ToArray();

                x.AddConsumers(assembly);
            })
            .AddScoped<IOrderService, OrderServiceImplement>()
            .AddScoped<IInventoryService, InventoryServiceImplement>()
            
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
    public async Task E2E_Choreography_HappyPath_Should_Process_Order_And_Deduct_Inventory()
    {
        var cartItems = new List<GoodViewModel>() { Good };
        var address = "7811 NE Pleasant Valley RdLiberty, Missouri(MO), 64068";

        // Step 1: Add inventory
        // Nhap kho
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

        // Step 2: Create Order
        await _harness.Bus.Publish(new OrderCreateEvent( UserId, cartItems, address));

        Assert.That(await _harness.Consumed.Any<OrderCreateEvent>(), "Message create order not consumed");

        Assert.That(await _harness.Consumed.Any<OrderCreateEventFailed>(), Is.True);

        // Assert.That(await _harness.Consumed.Any<InventoryGoodsBookedInWarehouseEventSuccess>(), Is.True);

        // // using var scope = _provider.CreateScope();
        // using var assertScope = _provider.CreateScope();
        // var orderDb = assertScope.ServiceProvider.GetRequiredService<OrderDbContext>();
        // var inventoryDb = assertScope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        // // var deliveryDb = assertScope.ServiceProvider.GetRequiredService<DeliveryDbContext>();

        // var order_in_db = await orderDb.Orders.FirstOrDefaultAsync(o => o.Id == Good.Id);
        // Assert.That(order_in_db, Is.Not.Null);
        // Assert.That(order_in_db.Status, Is.EqualTo(OrderStatus.Refunded));

        // var goods_in_db = await inventoryDb.Goods.FirstOrDefaultAsync(g => g.Id == Good.Id);
        // Assert.That(goods_in_db?.Count, Is.EqualTo(100));
    }
}