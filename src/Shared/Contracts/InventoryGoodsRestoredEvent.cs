
namespace Shared.Contracts;

public record InventoryGoodsRestoredEvent(Guid OrderId, Dictionary<Guid, int> Goods);