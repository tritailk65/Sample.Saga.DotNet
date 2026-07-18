using Choreography.Order.Infrastructure;
using Choreography.Order.Infrastructure.Entities;
using Choreography.Order.Infrastructure.Enums;
using Choreography.Order.Models;
using Microsoft.EntityFrameworkCore;

public class OrderServiceImplement(OrderDbContext dbContext) : IOrderService
{
    // Insert Order to db
    public async Task InsertAsync(OrderCreationModel creationModel, Guid? orderId = null, CancellationToken cancellationToken = default)
    {    
        // Check order
        if (creationModel.CartItems.Count == 0)
        {
            throw new ArgumentException("Basket cannot empty");
        }
        var order = new Order
        {
            Amount = creationModel.Amount,
            CartItems = creationModel.CartItems,
            CreationAtUtc = DateTime.UtcNow,
            Status = OrderStatus.Preparing,
            Id = orderId ?? Guid.NewGuid(),
            UserId = creationModel.UserId,
            DeliveryAddress = creationModel.DeliveryAddress
        };

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatusCancel(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order == null) throw new Exception("OrderID not found");
        order.Status = OrderStatus.Cancled;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatusComplete(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order == null) throw new Exception("OrderID not found");
        order.Status = OrderStatus.Completed;
        await dbContext.SaveChangesAsync(cancellationToken);
    }


    // Delete Order
    public async Task DeleteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var firstOrDefault = await dbContext.Orders.FirstOrDefaultAsync(f => f.Id == orderId, cancellationToken: cancellationToken);
        if (firstOrDefault is null)
        {
            return;
        }
        
        dbContext.Remove(firstOrDefault);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}