namespace GustaMudBlazor.Models.Entities;

public enum ItemDataType
{
    INT,
    DINT,
    BOOL,
    STRING
}

public class RecipeItem
{
    public int Id { get; set; }
    public int BlockId { get; set; }
    public string Tag { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ItemDataType DataType { get; set; } = ItemDataType.INT;

    // Propriedade de navegação para o Bloco pai
    public Block? Block { get; set; }

    // Relacionamento 1 para Muitos com a tabela de Valores dos Itens nas Receitas
    public List<RecipeItemValue> ItemValues { get; set; } = [];
}
