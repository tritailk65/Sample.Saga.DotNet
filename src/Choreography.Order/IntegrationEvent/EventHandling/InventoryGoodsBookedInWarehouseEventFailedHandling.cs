using Choreography.Order.IntegrationEvent.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Choreography.Order.IntegrationEvent.EventHandling;

public class InventoryGoodsBookedInWarehouseEventFailedHandling(
    ILogger<InventoryGoodsBookedInWarehouseEventFailedHandling> logger, 
    IOrderService orderService) : IConsumer<InventoryGoodsBookedInWarehouseEventFailed>
{
    public async Task Consume(ConsumeContext<InventoryGoodsBookedInWarehouseEventFailed> context)
    {
        await orderService.DeleteAsync(context.Message.OrderId, context.CancellationToken);
        logger.LogInformation($@"[{nameof(InventoryGoodsBookedInWarehouseEventFailedHandling)}] Message: Delete order by id {context.Message.OrderId}. 
                        Event: {nameof(InventoryGoodsBookedInWarehouseEventFailed)}");
    }
}