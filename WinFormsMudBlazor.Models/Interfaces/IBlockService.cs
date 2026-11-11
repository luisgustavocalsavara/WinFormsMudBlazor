using WinFormsMudBlazor.Models.Entities;

namespace WinFormsMudBlazor.Models.Interfaces;

public interface IBlockService
{
    Task<List<Block>> GetAllBlocksAsync();
    Task<List<Block>> GetBlocksByMixerAsync(MixerType mixerType);
    Task<Block?> GetBlockWithItemsAsync(int blockId);
    Task<Block> SaveBlockAsync(Block block);
    Task DeleteBlockAsync(int blockId);

    Task<List<RecipeItem>> GetAllBlockItemsAsync();
    Task<RecipeItem> SaveBlockItemAsync(RecipeItem item);
    Task DeleteBlockItemAsync(int itemId);
}
