using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace QuanLySCL.E2E.Specs;

public sealed record FunctionSpec(string Name, string RawText);

public static class ChucNangDocxParser
{
    public static IReadOnlyList<FunctionSpec> ExtractTopFunctions(string docxPath, int maxFunctions)
    {
        if (!File.Exists(docxPath))
            throw new FileNotFoundException("Word spec not found.", docxPath);

        var allText = ReadAllText(docxPath);
        var functions = SplitIntoNumberedSections(allText)
            .Take(maxFunctions)
            .ToList();

        return functions;
    }

    private static string ReadAllText(string docxPath)
    {
        using var doc = WordprocessingDocument.Open(docxPath, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body is null)
            return "";

        var sb = new StringBuilder();
        foreach (var t in body.Descendants<Text>())
        {
            if (sb.Length > 0)
                sb.Append('\n');
            sb.Append(t.Text);
        }

        return sb.ToString();
    }

    private static IEnumerable<FunctionSpec> SplitIntoNumberedSections(string text)
    {
        // Expected headings like: "1. Phần hệ ...", "2. Phần hệ ..."
        // Make it robust against inconsistent newlines by normalizing whitespace.
        var normalized = text
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Replace("\t", " ", StringComparison.Ordinal);

        var lines = normalized
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var currentHeader = (string?)null;
        var currentBody = new List<string>();

        foreach (var line in lines)
        {
            if (IsHeaderLine(line, out var header))
            {
                if (currentHeader is not null)
                {
                    yield return new FunctionSpec(currentHeader, string.Join("\n", currentBody));
                }

                currentHeader = header;
                currentBody = [];
                continue;
            }

            if (currentHeader is null)
                continue;

            currentBody.Add(line);
        }

        if (currentHeader is not null)
            yield return new FunctionSpec(currentHeader, string.Join("\n", currentBody));
    }

    private static bool IsHeaderLine(string line, out string header)
    {
        // Fast path: starts with "N." where N is 1-9
        header = "";
        if (line.Length < 3)
            return false;

        var dotIndex = line.IndexOf('.', StringComparison.Ordinal);
        if (dotIndex <= 0 || dotIndex > 2)
            return false;

        var numPart = line[..dotIndex];
        if (!int.TryParse(numPart, out var num) || num is < 1 or > 50)
            return false;

        header = line.Trim();
        return true;
    }
}

