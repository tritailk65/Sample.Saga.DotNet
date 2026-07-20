
namespace Shared.Contracts;

public record DeliverySendEvent(Guid OrderId, IEnumerable<GoodViewModel> Goods, Guid UserId, string Address);