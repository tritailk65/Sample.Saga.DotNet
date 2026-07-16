namespace Choreography.Inventory.IntegrationeEvent.Events;

public record DeliverySendEventFailed(Guid OrderId, IEnumerable<GoodViewModel> CartItems);