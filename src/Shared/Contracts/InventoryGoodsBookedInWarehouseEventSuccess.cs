namespace Shared.Contracts;


public record InventoryGoodsBookedInWarehouseEventSuccess(Guid OrderId, Guid UserId, IEnumerable<GoodViewModel> CartItems, string Address);