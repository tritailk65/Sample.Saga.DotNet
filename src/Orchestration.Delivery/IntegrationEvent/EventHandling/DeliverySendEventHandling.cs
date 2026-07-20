namespace Orchestration.Delivery.IntegrationEvent.EventHandling;

using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Contracts;
using Shared.Services.Delivery.Services;

public class DeliverySendEventHandling(
    ILogger<DeliverySendEventHandling> logger,
    IDeliveryService deliveryService) : IConsumer<DeliverySendEvent>
{
    public async Task Consume (ConsumeContext<DeliverySendEvent> context)
    {
        try
        {
            await deliveryService.SendPackageAsync(context.Message.OrderId, context.Message.Goods.Select(x => x.Id).ToList(),
                context.Message.UserId, context.Message.Address, context.CancellationToken);    
        }
        catch (Exception e)
        {
            logger.LogError($"[{nameof(DeliverySendEventSuccess)}]. Message: {e.Message}");
            await context.Publish(new DeliverySendEventFailed(context.Message.OrderId, context.Message.Goods));
            return;
        }

        await context.Publish(new DeliverySendEventSuccess(context.Message.OrderId), context.CancellationToken);
        logger.LogInformation($"[{nameof(DeliverySendEventHandling)}]. Message: Successfully send delivery by orderId {context.Message.OrderId}");
    }
}