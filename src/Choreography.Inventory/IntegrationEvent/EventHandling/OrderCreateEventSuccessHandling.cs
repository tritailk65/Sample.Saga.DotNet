namespace Choreography.Inventory.IntegrationeEvent.EventHandling;

using Choreography.Inventory.Services;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

public class OrderCreateEventSuccessHandling (
    ILogger<OrderCreateEventSuccessHandling> logger,
    IInventoryService inventoryService) : IConsumer<OrderCreateEventSuccess>
{
    public async Task Consume(ConsumeContext<OrderCreateEventSuccess> context)
    {
        // Checking the availability of goods
        var goodIds = context.Message.CartItems.Select(s => s.Id);
        var availabilityGoods = await inventoryService.CheckAvailabilityAsync(goodIds, context.CancellationToken);

        try
        {
            ValidateGoodsAvailability(context.Message.CartItems, availabilityGoods);
            await inventoryService.BookGoodsAsync(context.Message.CartItems.ToDictionary(x => x.Id, i => i.Count), context.CancellationToken);    
        }
        catch (Exception e)
        {
            logger.LogError($"[{nameof(OrderCreateEventSuccessHandling)}]. Message: Goods booked by orderId fail {e.Message}");

            await context.Publish(new InventoryGoodsBookedInWarehouseEventFailed(context.Message.OrderId));
            return;
        }

        //publish add iventory success
        await context.Publish(new InventoryGoodsBookedInWarehouseEventSuccess(context.Message.OrderId, context.Message.UserId, context.Message.CartItems, context.Message.Address));
        logger.LogInformation($"[{nameof(OrderCreateEventSuccessHandling)}]. Message: Successfully goods booked by orderId {context.Message.OrderId}");
    }

    private void ValidateGoodsAvailability(
        IEnumerable<GoodViewModel> goods,
        Dictionary<Guid, bool> availabilityDictionary)
    {
        //Collect all the good IDs for a quick search
        var goodsIds = new HashSet<Guid>(goods.Select(g => g.Id));

        // Check that all products are in the availability dictionary.
        if (goodsIds.Except(availabilityDictionary.Keys).Any())
        {
            var missingIds = goodsIds.Except(availabilityDictionary.Keys);
            throw new InvalidOperationException(
                $"Some good are missing from the availability list: {string.Join(", ", missingIds)}");
        }

        // Check that all good are available (true)
        var unavailableGoods = goods
            .Where(g => !availabilityDictionary[g.Id])
            .Select(g => g.Name)
            .ToList();

        if (unavailableGoods.Any())
        {
            throw new InvalidOperationException(
                $"Some goods are not available: {string.Join(", ", unavailableGoods)}");
        }
    }
}
