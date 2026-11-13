using GustaMudBlazor.Models.Entities;
using Microsoft.EntityFrameworkCore;
using GustaMudBlazor.Models.Interfaces;
using GustaMudBlazor.Services.Data;

namespace GustaMudBlazor.Services;

public class RecipeService(IDbContextFactory<AppDbContext> dbFactory) : IRecipeService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    private static readonly MixerType[] AllMixerTypes =
    [
        MixerType.MIXER100,
        MixerType.MIXER200,
        MixerType.MIXER300,
        MixerType.MIXER400
    ];

    public async Task<List<Recipe>> GetAllRecipesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Recipes
            .AsNoTracking()
            .Include(r => r.Mixers)
                .ThenInclude(m => m.RecipeBlocks.OrderBy(rb => rb.SequenceOrder))
                    .ThenInclude(rb => rb.Block)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Recipe?> GetRecipeWithMixersAsync(int recipeId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // Garante que a receita tenha os 4 Mixers criados no banco
        await EnsureFourMixersExistAsync(db, recipeId);

        var recipe = await db.Recipes
            .AsNoTracking()
            .Include(r => r.Mixers)
                .ThenInclude(m => m.RecipeBlocks.OrderBy(rb => rb.SequenceOrder))
                    .ThenInclude(rb => rb.Block)
                        .ThenInclude(b => b!.Items.OrderBy(i => i.Tag))
            .Include(r => r.Mixers)
                .ThenInclude(m => m.RecipeBlocks)
                    .ThenInclude(rb => rb.ItemValues)
                        .ThenInclude(iv => iv.RecipeItem)
            .FirstOrDefaultAsync(r => r.Id == recipeId);

        if (recipe is not null)
        {
            recipe.Mixers = recipe.Mixers
                .OrderBy(m => (int)m.MixerType)
                .ToList();

            foreach (var mixer in recipe.Mixers)
            {
                mixer.RecipeBlocks = mixer.RecipeBlocks
                    .OrderBy(rb => rb.SequenceOrder)
                    .ToList();

                foreach (var rb in mixer.RecipeBlocks)
                {
                    rb.ItemValues = rb.ItemValues
                        .OrderBy(iv => iv.RecipeItem?.Tag ?? string.Empty)
                        .ToList();
                }
            }
        }

        return recipe;
    }

    private static async Task EnsureFourMixersExistAsync(AppDbContext db, int recipeId)
    {
        var existingTypes = await db.RecipeMixers
            .Where(m => m.RecipeId == recipeId)
            .Select(m => m.MixerType)
            .ToListAsync();

        bool added = false;
        foreach (var type in AllMixerTypes)
        {
            if (!existingTypes.Contains(type))
            {
                db.RecipeMixers.Add(new RecipeMixer
                {
                    RecipeId = recipeId,
                    MixerType = type
                });
                added = true;
            }
        }

        if (added)
        {
            await db.SaveChangesAsync();
        }
    }

    public async Task<Recipe> SaveRecipeAsync(Recipe recipe)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        if (recipe.Id == 0)
        {
            recipe.CreatedAt = DateTime.Now;
            // Cria automaticamente os 4 MIXERS (MIXER 100, MIXER 200, MIXER 300, MIXER 400) para a nova receita
            recipe.Mixers = AllMixerTypes.Select(t => new RecipeMixer { MixerType = t }).ToList();
            db.Recipes.Add(recipe);
        }
        else
        {
            var existing = await db.Recipes.FindAsync(recipe.Id);
            if (existing is not null)
            {
                existing.Name = recipe.Name;
                existing.Sku = recipe.Sku;
            }
        }

        await db.SaveChangesAsync();
        return recipe;
    }

    public async Task DeleteRecipeAsync(int recipeId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var recipe = await db.Recipes.FindAsync(recipeId);
        if (recipe is not null)
        {
            db.Recipes.Remove(recipe);
            await db.SaveChangesAsync();
        }
    }

    public async Task<RecipeBlock> AddBlockToMixerAsync(int recipeMixerId, int blockId, int? targetOrder = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var recipeMixer = await db.RecipeMixers.FindAsync(recipeMixerId)
            ?? throw new InvalidOperationException("Mixer da receita não encontrado.");

        var block = await db.Blocks
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == blockId)
            ?? throw new InvalidOperationException("Bloco não encontrado.");

        // Validação de Segurança: Um bloco de um MIXER (ex: MIXER 100) NÃO PODE ser utilizado em outro MIXER (ex: MIXER 200)
        if (block.MixerType != recipeMixer.MixerType)
        {
            throw new InvalidOperationException(
                $"O bloco '{block.Code}' pertence ao {block.MixerType.GetDisplayName()} e não pode ser utilizado no {recipeMixer.MixerType.GetDisplayName()}.");
        }

        var existingBlocks = await db.RecipeBlocks
            .Where(rb => rb.RecipeMixerId == recipeMixerId)
            .OrderBy(rb => rb.SequenceOrder)
            .ToListAsync();

        int maxAllowedOrder = existingBlocks.Count + 1;
        int insertOrder = targetOrder.HasValue
            ? Math.Clamp(targetOrder.Value, 1, maxAllowedOrder)
            : maxAllowedOrder;

        // Desloca os blocos seguintes dentro daquele Mixer
        foreach (var eb in existingBlocks.Where(x => x.SequenceOrder >= insertOrder))
        {
            eb.SequenceOrder++;
        }

        var recipeBlock = new RecipeBlock
        {
            RecipeMixerId = recipeMixerId,
            BlockId = blockId,
            SequenceOrder = insertOrder,
            ItemValues = block.Items.Select(item => new RecipeItemValue
            {
                RecipeItemId = item.Id,
                Value = BlockService.GetDefaultValueForType(item.DataType)
            }).ToList()
        };

        db.RecipeBlocks.Add(recipeBlock);
        await db.SaveChangesAsync();

        await NormalizeSequenceOrderAsync(db, recipeMixerId);

        return recipeBlock;
    }

    public async Task MoveRecipeBlockOrderAsync(int recipeBlockId, int newOrder)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var target = await db.RecipeBlocks.FindAsync(recipeBlockId);
        if (target is null) return;

        var list = await db.RecipeBlocks
            .Where(rb => rb.RecipeMixerId == target.RecipeMixerId)
            .OrderBy(rb => rb.SequenceOrder)
            .ToListAsync();

        if (list.Count <= 1) return;

        int clampedOrder = Math.Clamp(newOrder, 1, list.Count);
        int currentIndex = list.FindIndex(rb => rb.Id == recipeBlockId);
        int targetIndex = clampedOrder - 1;

        if (currentIndex < 0 || currentIndex == targetIndex) return;

        list.RemoveAt(currentIndex);
        list.Insert(targetIndex, target);

        for (int i = 0; i < list.Count; i++)
        {
            list[i].SequenceOrder = i + 1;
        }

        await db.SaveChangesAsync();
    }

    public async Task RemoveRecipeBlockInstanceAsync(int recipeBlockId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var recipeBlock = await db.RecipeBlocks.FindAsync(recipeBlockId);
        if (recipeBlock is not null)
        {
            var recipeMixerId = recipeBlock.RecipeMixerId;
            db.RecipeBlocks.Remove(recipeBlock);
            await db.SaveChangesAsync();

            await NormalizeSequenceOrderAsync(db, recipeMixerId);
        }
    }

    private static async Task NormalizeSequenceOrderAsync(AppDbContext db, int recipeMixerId)
    {
        var remaining = await db.RecipeBlocks
            .Where(rb => rb.RecipeMixerId == recipeMixerId)
            .OrderBy(rb => rb.SequenceOrder)
            .ThenBy(rb => rb.Id)
            .ToListAsync();

        for (int i = 0; i < remaining.Count; i++)
        {
            remaining[i].SequenceOrder = i + 1;
        }
        await db.SaveChangesAsync();
    }

    public async Task UpdateRecipeItemValueAsync(int recipeItemValueId, string newValue)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var itemValue = await db.RecipeItemValues.FindAsync(recipeItemValueId);
        if (itemValue is not null)
        {
            itemValue.Value = newValue.Trim();
            await db.SaveChangesAsync();
        }
    }
}
