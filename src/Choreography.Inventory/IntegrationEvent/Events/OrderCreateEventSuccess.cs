namespace Choreography.Inventory.IntegrationeEvent;

public record OrderCreateEventSuccess(Guid OrderId, Guid UserId, IEnumerable<GoodViewModel> CartItems, string Address);

public class GoodViewModel
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required decimal Price { get; init; }
    public int Count { get; set; }
}