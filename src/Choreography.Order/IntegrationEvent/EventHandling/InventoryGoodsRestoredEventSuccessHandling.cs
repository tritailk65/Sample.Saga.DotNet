namespace Choreography.Order.IntegrationEvent.EventHandling;

using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

public class InventoryGoodsRetoredEventSuccessHandling(
    ILogger<InventoryGoodsRetoredEventSuccessHandling> logger, 
    IOrderService orderService) : IConsumer<InventoryGoodsRestoredEventSuccess>
{
    public async Task Consume(ConsumeContext<InventoryGoodsRestoredEventSuccess> context)
    {
        await orderService.UpdateStatusCancel(context.Message.OrderId, context.CancellationToken);
        logger.LogInformation($"[{nameof(InventoryGoodsRetoredEventSuccessHandling)}] Message: Cancel order because delivery send fail. Order: {context.Message.OrderId}");
        
    }
}