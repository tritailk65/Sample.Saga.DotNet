
using Shared.Infrastructure.Inventory.Infrastructure.Entities;

namespace Shared.Services.Inventory.Services;


public interface IInventoryService
{
    Task<Dictionary<Guid, bool>> CheckAvailabilityAsync(IEnumerable<Guid> goodIds, CancellationToken cancellationToken = default);
    Task BookGoodsAsync(Dictionary<Guid, int> goodDictionary, CancellationToken cancellationToken = default);
    Task AddGoodsCountAsync(Dictionary<Guid, int> goodDictionary, CancellationToken cancellationToken = default);
    Task InsertAsync(IEnumerable<Goods> goods, CancellationToken cancellationToken = default);
}