namespace Shared.Services.Delivery.Services;

public interface IDeliveryService
{
    Task SendPackageAsync(Guid orderId, IList<Guid> goodIds, Guid UserId, string address, CancellationToken cancellationToken = default);
}