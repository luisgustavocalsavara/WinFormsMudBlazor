using System.Globalization;
using System.Text;
using WinFormsMudBlazor.Models.Entities;

namespace WinFormsMudBlazor.Services;

/// <summary>
/// Gerador nativo de arquivos PDF (padrão ISO 32000-1 PDF 1.4 com fonte WinAnsi Encoding para acentuação em português)
/// sem necessidade de pacotes externos de terceiros ou licenças pagas.
/// </summary>
public static class PdfReportBuilder
{
    public static byte[] BuildPlcRecipePdf(Recipe recipe, IReadOnlyList<PlcExportRow> rows)
    {
        const int rowsPerPage = 28;
        var pages = new List<List<PlcExportRow>>();

        if (rows.Count == 0)
        {
            pages.Add([]);
        }
        else
        {
            for (int i = 0; i < rows.Count; i += rowsPerPage)
            {
                pages.Add(rows.Skip(i).Take(rowsPerPage).ToList());
            }
        }

        // Objetos PDF:
        // 1: Catalog
        // 2: Pages
        // 3: Font Helvetica (Regular)
        // 4: Font Helvetica-Bold
        // 5..N: Page & Content pairs
        var objects = new List<byte[]>();

        int totalPages = pages.Count;
        var pageObjectIds = new List<int>();

        // Reservamos índices 0..3 para objetos 1..4
        objects.Add([]); // Obj 1 placeholder
        objects.Add([]); // Obj 2 placeholder
        objects.Add(Encoding.ASCII.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"));
        objects.Add(Encoding.ASCII.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>"));

        for (int pIndex = 0; pIndex < totalPages; pIndex++)
        {
            int pageObjId = objects.Count + 1;
            int contentObjId = objects.Count + 2;
            pageObjectIds.Add(pageObjId);

            byte[] contentBytes = BuildPageContentStream(recipe, pages[pIndex], pIndex + 1, totalPages, rows.Count);
            string pageDict = $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentObjId} 0 R >>";

            objects.Add(Encoding.ASCII.GetBytes(pageDict));

            using var msStream = new MemoryStream();
            var header = Encoding.ASCII.GetBytes($"<< /Length {contentBytes.Length} >>\nstream\n");
            var footer = Encoding.ASCII.GetBytes("\nendstream");
            msStream.Write(header);
            msStream.Write(contentBytes);
            msStream.Write(footer);
            objects.Add(msStream.ToArray());
        }

        string kidsArray = string.Join(" ", pageObjectIds.Select(id => $"{id} 0 R"));
        objects[0] = Encoding.ASCII.GetBytes("<< /Type /Catalog /Pages 2 0 R >>");
        objects[1] = Encoding.ASCII.GetBytes($"<< /Type /Pages /Kids [{kidsArray}] /Count {totalPages} >>");

        using var pdfOut = new MemoryStream();
        WriteAscii(pdfOut, "%PDF-1.4\n%\u00e2\u00e3\u00cf\u00d3\n");

        var offsets = new List<long>();
        for (int i = 0; i < objects.Count; i++)
        {
            offsets.Add(pdfOut.Position);
            WriteAscii(pdfOut, $"{i + 1} 0 obj\n");
            pdfOut.Write(objects[i]);
            WriteAscii(pdfOut, "\nendobj\n");
        }

        long xrefOffset = pdfOut.Position;
        WriteAscii(pdfOut, $"xref\n0 {objects.Count + 1}\n");
        WriteAscii(pdfOut, "0000000000 65535 f \n");
        foreach (var off in offsets)
        {
            WriteAscii(pdfOut, $"{off:D10} 00000 n \n");
        }

        WriteAscii(pdfOut, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
        return pdfOut.ToArray();
    }

    private static byte[] BuildPageContentStream(
        Recipe recipe,
        List<PlcExportRow> pageRows,
        int pageNumber,
        int totalPages,
        int totalItems)
    {
        // Página A4 Paisagem (842 x 595 pt) para caber confortavelmente todas as colunas do PLC
        var sb = new StringBuilder();

        // Cabeçalho Azul Escuro
        sb.AppendLine("0.12 0.16 0.28 rg");
        sb.AppendLine("30 525 782 45 re f");

        // Título no Cabeçalho
        sb.AppendLine("1 1 1 rg");
        DrawText(sb, "F2", 14, 42, 550, $"TABELA DE PARAMETROS PLC - RECEITA: {recipe.Name} (SKU: {recipe.Sku})");
        DrawText(sb, "F1", 9, 42, 534, $"Data de Criacao da Receita: {recipe.CreatedAt:dd/MM/yyyy HH:mm}   |   Exportado em: {DateTime.Now:dd/MM/yyyy HH:mm:ss}   |   Total de Tags: {totalItems}   |   Pagina {pageNumber}/{totalPages}");

        // Cabeçalho da Tabela
        double tableTop = 500;
        sb.AppendLine("0.22 0.28 0.42 rg");
        sb.AppendLine($"30 {F(tableTop)} 782 20 re f");

        sb.AppendLine("1 1 1 rg");
        DrawText(sb, "F2", 8.5, 36, tableTop + 6, "MIXER");
        DrawText(sb, "F2", 8.5, 105, tableTop + 6, "INST. / BLOCO");
        DrawText(sb, "F2", 8.5, 250, tableTop + 6, "TAG PLC (INSTANCIA)");
        DrawText(sb, "F2", 8.5, 445, tableTop + 6, "TAG ORIGINAL");
        DrawText(sb, "F2", 8.5, 575, tableTop + 6, "DESCRICAO");
        DrawText(sb, "F2", 8.5, 715, tableTop + 6, "TIPO");
        DrawText(sb, "F2", 8.5, 755, tableTop + 6, "VALOR");

        double y = tableTop - 16;
        for (int i = 0; i < pageRows.Count; i++)
        {
            var r = pageRows[i];

            // Zebra striping
            if (i % 2 == 0)
            {
                sb.AppendLine("0.95 0.96 0.98 rg");
                sb.AppendLine($"30 {F(y - 4)} 782 16 re f");
            }

            // Linha divisória sutil
            sb.AppendLine("0.82 0.84 0.88 RG 0.5 w");
            sb.AppendLine($"30 {F(y - 4)} m 812 {F(y - 4)} l S");

            sb.AppendLine("0.1 0.1 0.15 rg");
            DrawText(sb, "F2", 8, 36, y, Truncate(r.MixerName, 12));
            DrawText(sb, "F1", 8, 105, y, Truncate($"#{r.BlockSequenceOrder} - {r.BlockCode}", 26));
            DrawText(sb, "F2", 8, 250, y, Truncate(r.PlcInstanceTag, 34));
            DrawText(sb, "F1", 8, 445, y, Truncate(r.Tag, 23));
            DrawText(sb, "F1", 8, 575, y, Truncate(r.Description, 26));
            DrawText(sb, "F2", 8, 715, y, r.DataType.ToString());
            DrawText(sb, "F2", 8, 755, y, Truncate(r.Value, 12));

            y -= 16;
        }

        // Rodapé
        sb.AppendLine("0.4 0.4 0.45 rg");
        DrawText(sb, "F1", 8, 30, 18, "Gerado pelo Sistema WinForms Blazor MudBlazor - Pronto para integracao com CLP / PLC.");

        // Converte para Latin1/WinAnsi para preservar acentos em português
        Encoding latin1 = Encoding.Latin1;
        return latin1.GetBytes(sb.ToString());
    }

    private static void DrawText(StringBuilder sb, string font, double size, double x, double y, string text)
    {
        string escaped = EscapePdfString(text);
        sb.AppendLine($"BT /{font} {F(size)} Tf {F(x)} {F(y)} Td ({escaped}) Tj ET");
    }

    private static string EscapePdfString(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace("\r", " ")
            .Replace("\n", " ");
    }

    private static string Truncate(string? value, int maxChars)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= maxChars ? value : value[..(maxChars - 2)] + "..";
    }

    private static string F(double val) => val.ToString("0.##", CultureInfo.InvariantCulture);

    private static void WriteAscii(Stream stream, string text)
    {
        var bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }
}
