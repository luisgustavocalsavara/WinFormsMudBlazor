using GustaMudBlazor.Models.Entities;

namespace GustaMudBlazor.Models.Interfaces;

public interface IRecipeService
{
    Task<List<Recipe>> GetAllRecipesAsync();
    Task<Recipe?> GetRecipeWithMixersAsync(int recipeId);
    Task<Recipe> SaveRecipeAsync(Recipe recipe);
    Task DeleteRecipeAsync(int recipeId);

    // Adiciona um bloco a um MIXER específico da receita (validando se o Bloco pertence àquele MixerType)
    Task<RecipeBlock> AddBlockToMixerAsync(int recipeMixerId, int blockId, int? targetOrder = null);
    Task RemoveRecipeBlockInstanceAsync(int recipeBlockId);

    // Altera a ordem dos blocos dentro daquele MIXER específico
    Task MoveRecipeBlockOrderAsync(int recipeBlockId, int newOrder);

    // Atualiza o valor de um item específico dentro de uma instância de bloco do Mixer
    Task UpdateRecipeItemValueAsync(int recipeItemValueId, string newValue);
}
