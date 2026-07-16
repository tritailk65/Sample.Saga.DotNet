namespace Choreography.Delivery.IntegrationEvent.Events;

public record DeliverySendEventFailed(Guid OrderId, IEnumerable<GoodViewModel> CartItems);