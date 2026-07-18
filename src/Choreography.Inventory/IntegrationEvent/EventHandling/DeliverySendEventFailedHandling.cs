using Choreography.Inventory.Services;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

namespace Choreography.Inventory.IntegrationeEvent.EventHandling;


public class DeliverySendEventFailedHandling(
    ILogger<DeliverySendEventFailedHandling> logger, 
    IInventoryService inventoryService) : IConsumer<DeliverySendEventFailed>
{
    public async Task Consume(ConsumeContext<DeliverySendEventFailed> context)
    {
        try
        {
            await inventoryService.AddGoodsCountAsync(context.Message.CartItems.ToDictionary(x => x.Id, i => i.Count), context.CancellationToken);
            logger.LogInformation($"[{nameof(DeliverySendEventFailedHandling)}] Message: Cancellation of the reservation of goods on order by id {context.Message.OrderId}");

            await context.Publish(new InventoryGoodsRestoredEventSuccess(context.Message.OrderId));
        }
        catch
        {
            logger.LogInformation($"[{nameof(DeliverySendEventFailedHandling)}] Message: Fail to return goods on order by id {context.Message.OrderId}");
            await context.Publish(new InventoryGoodsBookedInWarehouseEventFailed(context.Message.OrderId));
        }
            
    }
}