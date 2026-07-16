namespace Choreography.Delivery.Services;

public interface IDeliveryService
{
    Task SendPackageAsync(Guid orderId, ICollection<Guid> goodIds, Guid UserId, string address, CancellationToken cancellationToken = default);
}