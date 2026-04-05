using System.Text.RegularExpressions;
using QuanLySCL.E2E.Model;

namespace QuanLySCL.E2E.Specs;

public static class TestCaseFactory
{
    public static IReadOnlyList<TestCaseDefinition> Create10CasesPerFunction(IReadOnlyList<FunctionSpec> functions)
    {
        var results = new List<TestCaseDefinition>();

        for (var i = 0; i < functions.Count; i++)
        {
            var function = functions[i];
            var baseId = i + 1;
            var titles = ExtractCandidateScenarios(function.RawText).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            for (var j = 0; j < 10; j++)
            {
                var caseId = $"F{baseId:00}-TC{j + 1:00}";
                var title = j < titles.Count
                    ? titles[j]
                    : $"Scenario {j + 1}";

                results.Add(new TestCaseDefinition(
                    FunctionName: function.Name,
                    CaseId: caseId,
                    Title: title,
                    Steps: "Mở hệ thống, thực hiện các bước theo kịch bản, kiểm tra giao diện và dữ liệu.",
                    Expected: "Chức năng hoạt động đúng theo đặc tả trong file Word."));
            }
        }

        return results;
    }

    private static IEnumerable<string> ExtractCandidateScenarios(string rawSectionText)
    {
        if (string.IsNullOrWhiteSpace(rawSectionText))
            yield break;

        foreach (var line in rawSectionText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            // Remove leading bullets/colons and keep short-ish lines as scenario titles.
            var cleaned = Regex.Replace(line, @"^\W+", "");
            if (cleaned.Length < 6)
                continue;
            if (cleaned.Length > 140)
                continue;

            // Skip obvious noise
            if (cleaned.Contains("http", StringComparison.OrdinalIgnoreCase))
                continue;

            yield return cleaned;
        }
    }
}
