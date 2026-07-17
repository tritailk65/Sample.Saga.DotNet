namespace Choreography.Delivery.Infrastructure.Entities;

public class Delivery
{
    public required Guid Id { get; init; }
    public required Guid OrderId { get; init; }
    public required string Address { get; init; } = null!;
    public required Guid UserId { get; init; }
    public required IList<Guid> GoodIds { get; init; } = null!;
}