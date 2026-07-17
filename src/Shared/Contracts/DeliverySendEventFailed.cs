namespace Shared.Contracts;

public record DeliverySendEventFailed(Guid OrderId, IEnumerable<GoodViewModel> CartItems);