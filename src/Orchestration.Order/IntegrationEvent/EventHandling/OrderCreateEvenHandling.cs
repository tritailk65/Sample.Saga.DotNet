namespace Orchestration.Order.IntegrationeEvent.EventHandling;

public class OrderCreateEventHandling(
    ILogger<OrderCreateEventHandling> logger,
    IOrderService orderService) : IConsumer<OrderCreateEvent>
{
    public async Task Consume(ConsumeContext<OrderCreateEvent> context)
    {
        var orderId = context.Message.OrderId;

        var orderCreationModel = new OrderCreationModel
        {
            UserId = context.Message.UserId,
            DeliveryAddress = context.Message.Address,
            CartItems = context.Message.CartItems.Select(x => x.Name).ToList(),
            Amount = context.Message.CartItems.Sum(x => x.Price * x.Count)
        };

        try
        {
            await orderService.InsertAsync(orderCreationModel, orderId, context.CancellationToken);
        } 
        catch (Exception e)
        {
            logger.LogError($"[{nameof(OrderCreateEventHandling)}]. Message: {e.Message}");
            await context.Publish(new OrderCreateEventFailed(orderId, context.Message.CartItems));
            return;
        }

        await context.Publish(new OrderCreateEventSuccess(orderId, context.Message.UserId, context.Message.CartItems, context.Message.Address));
        logger.LogInformation($"[{nameof(OrderCreateEventHandling)}]. Message: Successfully create order {orderId}");
    }
}