using Choreography.Delivery.Services;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

namespace Choreography.Delivery.IntegrationEvent.EventHandling;

public class InventoryGoodsBookedInWarehouseEventSuccessHandling(
    ILogger<InventoryGoodsBookedInWarehouseEventSuccessHandling> logger,
    IDeliveryService deliveryService) : IConsumer<InventoryGoodsBookedInWarehouseEventSuccess>
{
    public async Task Consume (ConsumeContext<InventoryGoodsBookedInWarehouseEventSuccess> context)
    {
        try
        {
            await deliveryService.SendPackageAsync(context.Message.OrderId, context.Message.CartItems.Select(x => x.Id).ToList(),
                context.Message.UserId, context.Message.Address, context.CancellationToken);    
        }
        catch (Exception e)
        {
            logger.LogError($"[{nameof(InventoryGoodsBookedInWarehouseEventSuccessHandling)}]. Message: {e.Message}");
            await context.Publish(new DeliverySendEventFailed(context.Message.OrderId, context.Message.CartItems));
            return;
        }

        await context.Publish(new DeliverySendEventSuccess(context.Message.OrderId), context.CancellationToken);
        logger.LogInformation($"[{nameof(InventoryGoodsBookedInWarehouseEventSuccessHandling)}]. Message: Successfully send delivery by orderId {context.Message.OrderId}");
    }
}