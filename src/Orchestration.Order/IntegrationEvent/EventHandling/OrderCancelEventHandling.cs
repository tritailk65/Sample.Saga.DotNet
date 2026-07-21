namespace Orchestration.Order.IntegrationeEvent.EventHandling;


public class OrderCancelEventHandling(
    ILogger<OrderCancelEventHandling> logger,
    IOrderService orderService) : IConsumer<OrderCancelEvent>
{
    public async Task Consume(ConsumeContext<OrderCancelEvent> context)
    {
        // await context.Publish(new OrderCreateEventSuccess(orderId, context.Message.UserId, context.Message.CartItems, context.Message.Address));
        await orderService. UpdateStatusCancel(context.Message.OrderId, context.CancellationToken);
        logger.LogInformation($"[{nameof(OrderCreateEventHandling)}]. Message: Successfully cancel order {context.Message.OrderId}");
    }
}