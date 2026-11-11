using System.Diagnostics;
using System.Text;
using WinFormsMudBlazor.Models.Entities;
using WinFormsMudBlazor.Models.Interfaces;
using WinFormsMudBlazor.Services;

namespace WinFormsMudBlazor.Services;

public class WinFormsPlcExportService(MainForm mainForm, IRecipeService recipeService) : IPlcExportService
{
    private readonly MainForm _mainForm = mainForm;
    private readonly IRecipeService _recipeService = recipeService;

    public async Task<List<PlcExportRow>> GetPlcItemsForRecipeAsync(int recipeId)
    {
        var recipe = await _recipeService.GetRecipeWithMixersAsync(recipeId);
        if (recipe is null) return [];

        var rows = new List<PlcExportRow>();

        foreach (var mixer in recipe.Mixers.OrderBy(m => (int)m.MixerType))
        {
            foreach (var rb in mixer.RecipeBlocks.OrderBy(b => b.SequenceOrder))
            {
                foreach (var iv in rb.ItemValues.OrderBy(v => v.RecipeItem?.Tag ?? string.Empty))
                {
                    if (iv.RecipeItem is null) continue;

                    rows.Add(new PlcExportRow
                    {
                        RecipeId = recipe.Id,
                        RecipeName = recipe.Name,
                        RecipeSku = recipe.Sku,
                        MixerType = mixer.MixerType,
                        RecipeBlockId = rb.Id,
                        BlockSequenceOrder = rb.SequenceOrder,
                        BlockCode = rb.Block?.Code ?? string.Empty,
                        BlockName = rb.Block?.Name ?? string.Empty,
                        RecipeItemId = iv.RecipeItem.Id,
                        RecipeItemValueId = iv.Id,
                        Tag = iv.RecipeItem.Tag,
                        Description = iv.RecipeItem.Description,
                        DataType = iv.RecipeItem.DataType,
                        Value = iv.Value
                    });
                }
            }
        }

        return rows;
    }

    public Task<(bool Success, string Message, string? FilePath)> ExportToCsvAsync(Recipe recipe, IReadOnlyList<PlcExportRow> rows)
    {
        string safeSku = SanitizeFileName(recipe.Sku);
        string defaultFileName = $"PLC_{safeSku}_{DateTime.Now:yyyyMMdd_HHmm}.csv";

        string? chosenPath = ShowSaveFileDialog(
            title: "Exportar Tabela PLC para Arquivo CSV",
            filter: "Arquivo CSV para PLC (*.csv)|*.csv",
            defaultExt: "csv",
            defaultFileName: defaultFileName);

        if (string.IsNullOrWhiteSpace(chosenPath))
        {
            return Task.FromResult((false, "Exportação CSV cancelada pelo usuário.", (string?)null));
        }

        try
        {
            var sb = new StringBuilder();
            // Cabeçalho estruturado pronto para importação em CLP/SCADA e Excel (separador ';')
            sb.AppendLine("RECEITA;SKU;MIXER;ORDEM_INSTANCIA;CODIGO_BLOCO;NOME_BLOCO;TAG_PLC_INSTANCIA;TAG_BASE;DESCRICAO;TIPO_DADO;VALOR");

            foreach (var r in rows)
            {
                sb.AppendLine(string.Join(";",
                    EscapeCsv(r.RecipeName),
                    EscapeCsv(r.RecipeSku),
                    EscapeCsv(r.MixerName),
                    r.BlockSequenceOrder,
                    EscapeCsv(r.BlockCode),
                    EscapeCsv(r.BlockName),
                    EscapeCsv(r.PlcInstanceTag),
                    EscapeCsv(r.Tag),
                    EscapeCsv(r.Description),
                    r.DataType,
                    EscapeCsv(r.Value)));
            }

            // UTF-8 com BOM para abrir acentos perfeitamente no Excel e softwares de CLP
            File.WriteAllText(chosenPath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            return Task.FromResult((true, $"Arquivo CSV salvo com sucesso em: {chosenPath}", (string?)chosenPath));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, $"Erro ao salvar CSV: {ex.Message}", (string?)null));
        }
    }

    public Task<(bool Success, string Message, string? FilePath)> ExportToPdfAsync(Recipe recipe, IReadOnlyList<PlcExportRow> rows)
    {
        string safeSku = SanitizeFileName(recipe.Sku);
        string defaultFileName = $"PLC_{safeSku}_{DateTime.Now:yyyyMMdd_HHmm}.pdf";

        string? chosenPath = ShowSaveFileDialog(
            title: "Exportar Tabela PLC para Arquivo PDF",
            filter: "Documento PDF (*.pdf)|*.pdf",
            defaultExt: "pdf",
            defaultFileName: defaultFileName);

        if (string.IsNullOrWhiteSpace(chosenPath))
        {
            return Task.FromResult((false, "Exportação PDF cancelada pelo usuário.", (string?)null));
        }

        try
        {
            byte[] pdfBytes = PdfReportBuilder.BuildPlcRecipePdf(recipe, rows);
            File.WriteAllBytes(chosenPath, pdfBytes);

            // Abre o PDF gerado no visualizador padrão do Windows para conferência imediata
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = chosenPath,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Ignora caso o sistema não tenha leitor padrão associado
            }

            return Task.FromResult((true, $"Arquivo PDF salvo com sucesso em: {chosenPath}", (string?)chosenPath));
        }
        catch (Exception ex)
        {
            return Task.FromResult((false, $"Erro ao gerar PDF: {ex.Message}", (string?)null));
        }
    }

    private string? ShowSaveFileDialog(string title, string filter, string defaultExt, string defaultFileName)
    {
        string? selectedPath = null;

        void OpenDialog()
        {
            using var dialog = new SaveFileDialog
            {
                Title = title,
                Filter = filter,
                DefaultExt = defaultExt,
                FileName = defaultFileName,
                AddExtension = true,
                OverwritePrompt = true
            };

            if (dialog.ShowDialog(_mainForm) == DialogResult.OK)
            {
                selectedPath = dialog.FileName;
            }
        }

        if (_mainForm.InvokeRequired)
        {
            _mainForm.Invoke(OpenDialog);
        }
        else
        {
            OpenDialog();
        }

        return selectedPath;
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    private static string SanitizeFileName(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "RECEITA";
        var invalid = Path.GetInvalidFileNameChars();
        return new string(input.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
    }
}
