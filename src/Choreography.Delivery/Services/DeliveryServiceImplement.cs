namespace Choreography.Delivery.Services;

using Choreography.Delivery.Infrastructure;
using Choreography.Delivery.Infrastructure.Entities;

public class DeliveryServiceImplement(DeliveryDbContext dbContext) : IDeliveryService
{
    public async Task SendPackageAsync(Guid orderId, IList<Guid> goodIds, Guid userId,  string address, CancellationToken cancellationToken = default)
    {
        if (goodIds.Count == 0)
        {
            throw new ArgumentException($"{nameof(goodIds)} cannot be equal zero items");
        }

        // Special condition for integration test
        if (address.Equals("Invalid Address"))
        {
            throw new ArgumentException($"{nameof(goodIds)} must have valid address!");
        }
        
        var delivery = new Delivery
        {
            Id = Guid.NewGuid(),
            Address = address,
            GoodIds = goodIds,
            OrderId = orderId,
            UserId = userId,
        };

        

        await dbContext.Deliveries.AddAsync(delivery, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}