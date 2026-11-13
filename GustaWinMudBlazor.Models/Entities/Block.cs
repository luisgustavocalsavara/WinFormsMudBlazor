namespace GustaMudBlazor.Models.Entities;

public class Block
{
    public int Id { get; set; }

    /// <summary>
    /// Define a qual MIXER este Bloco pertence (MIXER 100, 200, 300 ou 400).
    /// Um bloco do MIXER 100 só pode ser utilizado no MIXER 100.
    /// </summary>
    public MixerType MixerType { get; set; } = MixerType.MIXER100;

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Armazena o arquivo .ico em formato Data URI (data:image/x-icon;base64,...)
    /// </summary>
    public string IconDataUrl { get; set; } = string.Empty;
    public string IconFileName { get; set; } = string.Empty;

    // Relacionamento 1 para Muitos (Um Bloco define vários Itens/Tags estruturais)
    public List<RecipeItem> Items { get; set; } = [];

    // Instâncias deste bloco vinculadas nos Mixers das Receitas
    public List<RecipeBlock> RecipeBlocks { get; set; } = [];
}
