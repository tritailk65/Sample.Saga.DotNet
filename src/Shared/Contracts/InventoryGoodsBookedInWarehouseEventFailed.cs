namespace Shared.Contracts;

public record InventoryGoodsBookedInWarehouseEventFailed(Guid OrderId, IEnumerable<GoodViewModel> CartItems);