namespace Shared.Contracts;


public record InventoryGoodsBookedRejectedEvent(Guid OrderId, Dictionary<Guid, int> Goods);