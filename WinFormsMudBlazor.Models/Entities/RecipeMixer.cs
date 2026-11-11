namespace WinFormsMudBlazor.Models.Entities;

public class RecipeMixer
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public MixerType MixerType { get; set; } = MixerType.MIXER100;

    public string Name => MixerType.GetDisplayName();

    public Recipe? Recipe { get; set; }

    // Instâncias de blocos inseridas especificamente neste MIXER da receita
    public List<RecipeBlock> RecipeBlocks { get; set; } = [];
}
