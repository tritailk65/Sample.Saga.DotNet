namespace Choreography.Inventory.IntegrationeEvent.Events;


public record InventoryGoodsBookedInWarehouseEventSuccess(Guid OrderId, Guid UserId, IEnumerable<GoodViewModel> CartItems, string Address);