using System.Reflection;
using Choreography.Order.IntegrationEvent.EventHandling;
using Choreography.Order.IntegrationEvent.Events;
using Choreography.Order.Models;
using MassTransit.Testing;

namespace Choreography.Tests.NUnit;

public class when_a_booking_is_added
{
    [Test]
    public async Task Should_publish_success_event_when_added()
    {
        await using var provider = new ServiceCollection()
            .ConfigureMassTransit(x =>
            {
                // var assembly = Assembly.GetAssembly(typeof(Choreography.Order.IntegrationEvent.Events.OrderCreateEvent));
                // x.AddConsumer(assembly);
            })
            .BuildServiceProvider(true);

        var harness = provider.GetTestHarness();
        await harness.Start();
        
        var orderId = NewId.NextGuid();

        //Arrange
        var cartItems = new List<GoodViewModel>() { Constans.Good };
        var address = "7811 NE Pleasant Valley RdLiberty, Missouri(MO), 64068";

        await harness.Bus.Publish(new OrderCreateEvent( Constans.UserId, cartItems, address));

    }
}