using GustaMudBlazor.Models.Entities;

namespace GustaMudBlazor.Models.Interfaces;

public interface IPlcExportService
{
    Task<List<PlcExportRow>> GetPlcItemsForRecipeAsync(int recipeId);
    Task<(bool Success, string Message, string? FilePath)> ExportToCsvAsync(Recipe recipe, IReadOnlyList<PlcExportRow> rows);
    Task<(bool Success, string Message, string? FilePath)> ExportToPdfAsync(Recipe recipe, IReadOnlyList<PlcExportRow> rows);
}
