namespace Shared.Contracts;

public record InventoryGoodsBookedInWarehouseEvent(Guid OrderId, Guid UserId, IEnumerable<GoodViewModel> CartItems, string Address);