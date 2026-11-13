using GustaMudBlazor.Models.Entities;

namespace GustaMudBlazor.Models.Interfaces;

public interface IPlcExportDataService
{
    Task<List<PlcExportRow>> GetPlcItemsForRecipeAsync(int recipeId);
}
