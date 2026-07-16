using Choreography.Order.Infrastructure;
using Choreography.Order.IntegrationEvent.Events;
using Choreography.Order.Models;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

    [Test]
    public async Task Should_publish_success_event_when_added_order_to_db()
    {
        var cartItems = new List<GoodViewModel>() { OrderConstans.Good };
        var address = "7811 NE Pleasant Valley RdLiberty, Missouri(MO), 64068";

        await _harness.Bus.Publish(new OrderCreateEvent( OrderConstans.UserId, cartItems, address));

        Assert.That(await _harness.Consumed.Any<OrderCreateEvent>(), "Message create order not consumed");

        using var scope = _provider.CreateScope();
        var _db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        var savedOrder = await _db.Orders.FirstOrDefaultAsync();
        Assert.That(savedOrder, Is.Not.Null);
        Assert.That(savedOrder.DeliveryAddress, Is.EqualTo(address));

        Assert.That(await _harness.Published.Any<OrderCreateEventSuccess>(), Is.True);

    }

}