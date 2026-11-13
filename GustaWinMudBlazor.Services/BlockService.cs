using GustaMudBlazor.Models.Entities;
using GustaMudBlazor.Models.Interfaces;
using Microsoft.EntityFrameworkCore;
using GustaMudBlazor.Services.Data;

namespace GustaMudBlazor.Services;

public class BlockService(IDbContextFactory<AppDbContext> dbFactory) : IBlockService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public async Task<List<Block>> GetAllBlocksAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Blocks
            .AsNoTracking()
            .Include(b => b.Items.OrderBy(i => i.Tag))
            .Include(b => b.RecipeBlocks)
            .OrderBy(b => b.MixerType)
            .ThenBy(b => b.Code)
            .ToListAsync();
    }

    public async Task<List<Block>> GetBlocksByMixerAsync(MixerType mixerType)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Blocks
            .AsNoTracking()
            .Where(b => b.MixerType == mixerType)
            .Include(b => b.Items.OrderBy(i => i.Tag))
            .OrderBy(b => b.Code)
            .ToListAsync();
    }

    public async Task<Block?> GetBlockWithItemsAsync(int blockId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Blocks
            .AsNoTracking()
            .Include(b => b.Items.OrderBy(i => i.Tag))
            .Include(b => b.RecipeBlocks)
            .FirstOrDefaultAsync(b => b.Id == blockId);
    }

    public async Task<Block> SaveBlockAsync(Block block)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var normalizedCode = block.Code.Trim().ToLower();
        var duplicate = await db.Blocks.AnyAsync(b => b.Id != block.Id && b.Code.ToLower() == normalizedCode);
        if (duplicate)
        {
            throw new InvalidOperationException($"Já existe um bloco cadastrado com o código '{block.Code}'.");
        }

        if (block.Id == 0)
        {
            db.Blocks.Add(block);
        }
        else
        {
            var existing = await db.Blocks.Include(b => b.RecipeBlocks).FirstOrDefaultAsync(b => b.Id == block.Id);
            if (existing is not null)
            {
                if (existing.MixerType != block.MixerType && existing.RecipeBlocks.Count > 0)
                {
                    throw new InvalidOperationException(
                        $"Este bloco já está sendo usado no {existing.MixerType.GetDisplayName()} de uma receita e não pode mudar para outro Mixer.");
                }

                existing.MixerType = block.MixerType;
                existing.Code = block.Code;
                existing.Name = block.Name;
                existing.Description = block.Description;
                existing.IconDataUrl = block.IconDataUrl;
                existing.IconFileName = block.IconFileName;
            }
        }

        await db.SaveChangesAsync();
        return block;
    }

    public async Task DeleteBlockAsync(int blockId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var block = await db.Blocks.FindAsync(blockId);
        if (block is not null)
        {
            db.Blocks.Remove(block);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<RecipeItem>> GetAllBlockItemsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.RecipeItems
            .AsNoTracking()
            .Include(i => i.Block)
            .OrderBy(i => i.Block!.MixerType)
            .ThenBy(i => i.Block!.Code)
            .ThenBy(i => i.Tag)
            .ToListAsync();
    }

    public async Task<RecipeItem> SaveBlockItemAsync(RecipeItem item)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        if (item.Id == 0)
        {
            db.RecipeItems.Add(item);
            await db.SaveChangesAsync();

            var existingRecipeBlocks = await db.RecipeBlocks
                .Where(rb => rb.BlockId == item.BlockId)
                .ToListAsync();

            if (existingRecipeBlocks.Count > 0)
            {
                var defaultVal = GetDefaultValueForType(item.DataType);
                foreach (var rb in existingRecipeBlocks)
                {
                    db.RecipeItemValues.Add(new RecipeItemValue
                    {
                        RecipeBlockId = rb.Id,
                        RecipeItemId = item.Id,
                        Value = defaultVal
                    });
                }
                await db.SaveChangesAsync();
            }
        }
        else
        {
            var existing = await db.RecipeItems.FindAsync(item.Id);
            if (existing is not null)
            {
                existing.BlockId = item.BlockId;
                existing.Tag = item.Tag;
                existing.Description = item.Description;
                existing.DataType = item.DataType;
                await db.SaveChangesAsync();
            }
        }

        return item;
    }

    public async Task DeleteBlockItemAsync(int itemId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var item = await db.RecipeItems.FindAsync(itemId);
        if (item is not null)
        {
            db.RecipeItems.Remove(item);
            await db.SaveChangesAsync();
        }
    }

    public static string GetDefaultValueForType(ItemDataType dataType) => dataType switch
    {
        ItemDataType.BOOL => "False",
        ItemDataType.INT or ItemDataType.DINT => "0",
        _ => string.Empty
    };
}
