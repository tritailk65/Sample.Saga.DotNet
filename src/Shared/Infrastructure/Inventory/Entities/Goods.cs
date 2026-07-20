namespace Shared.Infrastructure.Inventory.Infrastructure.Entities;


public class Goods
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public int Count { get; set; }
}