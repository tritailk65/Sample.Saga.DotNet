namespace Choreography.Inventory.Services;

using Choreography.Inventory.Infrastructure.Entities;

public interface IInventoryService
{
    Task<Dictionary<Guid, bool>> CheckAvailabilityAsync(IEnumerable<Guid> goodIds, CancellationToken cancellationToken = default);
    Task BookGoodsAsync(Dictionary<Guid, int> goodDictionary, CancellationToken cancellationToken = default);
    Task AddGoodsCountAsync(Dictionary<Guid, int> goodDictionary, CancellationToken cancellationToken = default);
    Task InsertAsync(IEnumerable<Goods> goods, CancellationToken cancellationToken = default);
}