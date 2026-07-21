namespace Orchestration.Order.IntegrationeEvent.EventHandling;

public class InventoryGoodsBookedRejectedHandling(
    ILogger<InventoryGoodsBookedRejectedHandling> logger,
    IOrderService orderService) : IConsumer<InventoryGoodsBookedRejectedEvent>
{
    public async Task Consume (ConsumeContext<InventoryGoodsBookedRejectedEvent> context)
    {
        await orderService.UpdateStatusRejected(context.Message.OrderId, context.CancellationToken);
        logger.LogInformation($"[{nameof(InventoryGoodsBookedRejectedHandling)}] Message: Reject order because not enough inventory. Order: {context.Message.OrderId}");
      
    }
}