using Choreography.Order.Models;

public interface IOrderService
{
    Task InsertAsync(OrderCreationModel creationModel, Guid? orderId = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task UpdateStatusCancel(Guid orderId, CancellationToken cancellationToken = default);
}