
namespace Shared.Services.Order;

using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Order.Infrastructure;
using Shared.Models;
using Shared.Infrastructure.Order.Infrastructure.Entities;
using Shared.Infrastructure.Order.Infrastructure.Enums;

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

    public async Task UpdateStatusRejected(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order == null) throw new Exception("OrderID not found");
        order.Status = OrderStatus.Rejected;
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