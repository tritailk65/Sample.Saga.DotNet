namespace Choreography.Inventory.Services;

using Choreography.Inventory.Infrastructure;
using Choreography.Inventory.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

public class InventoryServiceImplement(InventoryDbContext dbContext) : IInventoryService
{

    // Check tồn kho
    public async Task<Dictionary<Guid, bool>> CheckAvailabilityAsync(IEnumerable<Guid> goodIds, CancellationToken cancellationToken = default)
    {
        var goods = await dbContext.Goods
            .Where(good => goodIds.Contains(good.Id))
            .ToDictionaryAsync(
                good => good.Id, 
                good => good.Count > 0, 
                cancellationToken);

        // Creating a result based on missing items (false for them)
        return goodIds.ToDictionary(
            id => id, 
            id => goods.TryGetValue(id, out var isAvailable) && isAvailable);
    }

    // Trừ tồn kho, ném lỗi không đủ tồn kho nếu vượt quá số lượng
    public async Task BookGoodsAsync(Dictionary<Guid, int> goodDictionary, CancellationToken cancellationToken = default)
    {
        var ids = goodDictionary.Select(s => s.Key);
        var goods = await dbContext.Goods
            .Where(good => ids.Contains(good.Id))
            .ToListAsync(cancellationToken);

        foreach (var good in goods)
        {
            var count = goodDictionary[good.Id];
            if (good.Count < count)
            {
                throw new ArgumentOutOfRangeException($"{count-good.Count} units of {good.Name} good are missing");
            }

            good.Count -= count;
        }
        
        dbContext.Goods.UpdateRange(goods);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // Thêm vào tồn kho, xử lý cho trường hợp cancle
    public async Task AddGoodsCountAsync(Dictionary<Guid, int> goodDictionary, CancellationToken cancellationToken = default)
    {
        var ids = goodDictionary.Select(s => s.Key);
        var goods = await dbContext.Goods
            .Where(good => ids.Contains(good.Id))
            .ToListAsync(cancellationToken);

        foreach (var good in goods)
        {
            good.Count += goodDictionary[good.Id];
        }
        
        dbContext.Goods.UpdateRange(goods);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    //add record inventory
    public Task InsertAsync(IEnumerable<Goods> goods, CancellationToken cancellationToken = default)
        => dbContext.Goods.AddRangeAsync(goods, cancellationToken);
}

