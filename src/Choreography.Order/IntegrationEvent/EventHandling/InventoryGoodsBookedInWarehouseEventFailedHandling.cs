using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Contracts;
using Shared.Services.Order;

namespace Choreography.Order.IntegrationEvent.EventHandling;

public class InventoryGoodsBookedInWarehouseEventFailedHandling(
    ILogger<InventoryGoodsBookedInWarehouseEventFailedHandling> logger, 
    IOrderService orderService) : IConsumer<InventoryGoodsBookedInWarehouseEventFailed>
{
    public async Task Consume(ConsumeContext<InventoryGoodsBookedInWarehouseEventFailed> context)
    {
        // await orderService.DeleteAsync(context.Message.OrderId, context.CancellationToken);
        await orderService.UpdateStatusCancel(context.Message.OrderId, context.CancellationToken);
        logger.LogInformation($@"[{nameof(InventoryGoodsBookedInWarehouseEventFailedHandling)}] Message: Canceled order by id {context.Message.OrderId}. 
                        Event: {nameof(InventoryGoodsBookedInWarehouseEventFailed)}");
    }
}