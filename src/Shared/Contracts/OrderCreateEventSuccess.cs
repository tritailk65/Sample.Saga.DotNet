namespace Shared.Contracts;

public record OrderCreateEventSuccess(Guid OrderId, Guid UserId, IEnumerable<GoodViewModel> CartItems, string Address);