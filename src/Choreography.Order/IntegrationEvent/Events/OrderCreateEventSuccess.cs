namespace Choreography.Order.IntegrationEvent.Events;


public record OrderCreateEventSuccess(Guid OrderId, Guid UserId, IEnumerable<GoodViewModel> CartItems, string Address);