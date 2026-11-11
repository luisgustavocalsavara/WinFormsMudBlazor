namespace WinFormsMudBlazor.Models.Entities;

public class RecipeItemValue
{
    public int Id { get; set; }
    public int RecipeBlockId { get; set; }
    public int RecipeItemId { get; set; }
    public string Value { get; set; } = string.Empty;

    public RecipeBlock? RecipeBlock { get; set; }
    public RecipeItem? RecipeItem { get; set; }
}
