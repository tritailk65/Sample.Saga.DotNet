namespace Orchestration.Order.IntegrationeEvent.EventHandling;

public class DeliverySendEventSuccessHandling(
    ILogger<DeliverySendEventSuccessHandling> logger,
    IOrderService orderService) : IConsumer<DeliverySendEventSuccess>
{
    public async Task Consume(ConsumeContext<DeliverySendEventSuccess> context)
    {
        await orderService.UpdateStatusComplete(context.Message.OrderId, context.CancellationToken);
        logger.LogInformation($"[{nameof(DeliverySendEventSuccessHandling)}] Message: Change status to completed. Order: {context.Message.OrderId}");
    }
}
