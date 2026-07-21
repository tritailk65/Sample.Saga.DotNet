using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Contracts;
using Shared.Services.Inventory.Services;

namespace Orchestration.Inventory.IntegrationEvent.EventHandling;



public class InventoryGoodsRestoredEventHandling (
    ILogger<InventoryGoodsRestoredEventHandling> logger,
    IInventoryService inventoryService) : IConsumer<InventoryGoodsRestoredEvent>
{
    public async Task Consume (ConsumeContext<InventoryGoodsRestoredEvent> context)
    {
        await inventoryService.AddGoodsCountAsync(context.Message.Goods, context.CancellationToken);
        logger.LogInformation($"[{nameof(InventoryGoodsRestoredEventHandling)}]. Message: Retored Goods successfully");

    }
}