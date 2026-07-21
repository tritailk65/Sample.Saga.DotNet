

using Shared.Infrastructure.OrderSaga;
using Shared.Models;
using Shared.Services.OrderSaga;
using Shared.StateMachines;
public class OrderServiceImplement(OrderSagaDbContext dbContext) : IOrderSagaService
{
    public Task DeleteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task InsertAsync(OrderCreationModel creationModel, Guid? orderId = null, CancellationToken cancellationToken = default)
    {
        // Check order
        if (creationModel.CartItems.Count == 0)
        {
            throw new ArgumentException("Basket cannot empty");
        }
        var order = new OrderSaga
        {
            Amount = creationModel.Amount,
            CartItems = creationModel.CartItems,
            CreationAtUtc = DateTime.UtcNow,
            Id = orderId ?? Guid.NewGuid(),
            UserId = creationModel.UserId,
            DeliveryAddress = creationModel.DeliveryAddress
        };

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateStatusCancel(Guid orderId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task UpdateStatusComplete(Guid orderId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task UpdateStatusRejected(Guid orderId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}