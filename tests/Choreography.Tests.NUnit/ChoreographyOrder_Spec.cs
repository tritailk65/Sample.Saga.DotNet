
namespace Choreography.Tests.NUnit;

[TestFixture]
public class when_a_booking_is_added
{
    [Test]
    public async Task Should_publish_success_event_when_added()
    {
        await using var provider = new ServiceCollection()
            .ConfigureMassTransit(x =>
            {
                //var assembly = Assembly.GetAssembly(typeof(Choreography.Order.IntegrationEvent.EventHandling.OrderCreateEventHandling));
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                            .Where(a => a.FullName.Contains("Choreography.Order"))
                            .ToArray();

                x.AddConsumers(assembly);
            })
            .AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString());
            })
            .AddScoped<IOrderService, OrderServiceImplement>()
            .BuildServiceProvider(true);

        var harness = provider.GetTestHarness();

        await harness.Start();
        
        var orderId = NewId.NextGuid();

        //Arrange
        var cartItems = new List<GoodViewModel>() { Constans.Good };
        var address = "7811 NE Pleasant Valley RdLiberty, Missouri(MO), 64068";

        await harness.Bus.Publish(new OrderCreateEvent( Constans.UserId, cartItems, address));

        Assert.That(await harness.Consumed.Any<OrderCreateEvent>(), "Message create order not consumed");

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var savedOrder = await db.Orders.FirstOrDefaultAsync();
        Assert.That(savedOrder, Is.Not.Null);
        Assert.That(savedOrder.DeliveryAddress, Is.EqualTo(address));

        Assert.That(await harness.Published.Any<OrderCreateEventSuccess>(), Is.True);
    }
}