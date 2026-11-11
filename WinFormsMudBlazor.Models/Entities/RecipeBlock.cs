namespace WinFormsMudBlazor.Models.Entities;

public class RecipeBlock
{
    public int Id { get; set; }
    public int RecipeMixerId { get; set; }
    public int BlockId { get; set; }

    /// <summary>
    /// Ordem/Sequência da instância do bloco dentro daquele MIXER específico da receita (1º, 2º, 3º...)
    /// </summary>
    public int SequenceOrder { get; set; } = 1;

    public RecipeMixer? RecipeMixer { get; set; }
    public Block? Block { get; set; }

    // Valores específicos dos itens para esta instância do bloco no Mixer da receita
    public List<RecipeItemValue> ItemValues { get; set; } = [];
}
