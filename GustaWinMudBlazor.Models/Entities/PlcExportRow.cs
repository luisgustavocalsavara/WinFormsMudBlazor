namespace GustaMudBlazor.Models.Entities;

public class PlcExportRow
{
    public int RecipeId { get; set; }
    public string RecipeName { get; set; } = string.Empty;
    public string RecipeSku { get; set; } = string.Empty;

    public MixerType MixerType { get; set; }
    public string MixerName => MixerType.GetDisplayName();

    public int RecipeBlockId { get; set; }
    public int BlockSequenceOrder { get; set; }
    public string BlockCode { get; set; } = string.Empty;
    public string BlockName { get; set; } = string.Empty;

    /// <summary>
    /// Identificação clara da instância do bloco (ex: "MIXER 100 - #2 [BLK-M100-AGIT]")
    /// </summary>
    public string InstanceLabel => $"{MixerName} | Instância #{BlockSequenceOrder} ({BlockCode})";

    /// <summary>
    /// Tag formatada para envio ao PLC diferenciando a instância quando houver blocos repetidos
    /// </summary>
    public string PlcInstanceTag => $"{MixerType}_INST{BlockSequenceOrder}_{Tag}";

    public int RecipeItemId { get; set; }
    public int RecipeItemValueId { get; set; }
    public string Tag { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ItemDataType DataType { get; set; }
    public string Value { get; set; } = string.Empty;
}
