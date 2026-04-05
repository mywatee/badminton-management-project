using ClosedXML.Excel;
using QuanLySCL.E2E.Model;

namespace QuanLySCL.E2E.Reporting;

public static class ExcelReportWriter
{
    public static string WriteReport(
        string outputDir,
        string? templatePath,
        IReadOnlyCollection<TestRunRecord> records)
    {
        Directory.CreateDirectory(outputDir);

        var fileName = $"QuanLySCL_Playwright_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        var outputPath = Path.Combine(outputDir, fileName);

        using var workbook = TryLoadTemplate(templatePath) ?? new XLWorkbook();

        var worksheetName = "AutoTest";
        var existing = workbook.Worksheets.FirstOrDefault(s => string.Equals(s.Name, worksheetName, StringComparison.OrdinalIgnoreCase));
        existing?.Delete();
        var ordered = records
            .OrderBy(r => r.FunctionName)
            .ThenBy(r => r.CaseId)
            .ThenBy(r => r.RunIndex)
            .ToList();

        // If a template is provided, clone the 1st worksheet that looks like the user's template
        // (the one with "ID" at A7) so AutoTest visually matches it.
        var templateSheet = FindTemplateSheet(workbook);
        var ws = templateSheet is not null
            ? CloneTemplateSheet(templateSheet, worksheetName)
            : workbook.Worksheets.Add(worksheetName);

        if (templateSheet is null)
        {
            WriteHeader(ws);
            WriteRowsSimple(ws, ordered);
            ws.Columns().AdjustToContents();
        }
        else
        {
            WriteSummary(ws, ordered);
            WriteRowsLikeTemplate(ws, ordered);
        }

        workbook.SaveAs(outputPath);
        return outputPath;
    }

    private static XLWorkbook? TryLoadTemplate(string? templatePath)
    {
        if (string.IsNullOrWhiteSpace(templatePath))
            return null;
        if (!File.Exists(templatePath))
            return null;

        return new XLWorkbook(templatePath);
    }

    private static IXLWorksheet? FindTemplateSheet(XLWorkbook workbook)
    {
        foreach (var ws in workbook.Worksheets)
        {
            var a7 = ws.Cell("A7").GetString();
            if (string.Equals(a7, "ID", StringComparison.OrdinalIgnoreCase))
                return ws;
        }

        return null;
    }

    private static IXLWorksheet CloneTemplateSheet(IXLWorksheet templateSheet, string worksheetName)
    {
        // ClosedXML copies styles/merges/validations so the output looks the same as the template.
        return templateSheet.CopyTo(worksheetName);
    }

    private static void WriteSummary(IXLWorksheet ws, IReadOnlyList<TestRunRecord> records)
    {
        var passed = records.Count(r => r.Outcome == TestOutcome.Pass);
        var failed = records.Count(r => r.Outcome == TestOutcome.Fail);

        // Keep the template styling; only update the values.
        ws.Cell("C1").Value = "Project name";
        ws.Cell("D1").Value = "QuanLySCL";

        ws.Cell("C2").Value = "TestCase";
        ws.Cell("D2").Value = "AutoTest";

        ws.Cell("C3").Value = "Creator";
        ws.Cell("D3").Value = Environment.UserName;

        ws.Cell("C4").Value = "Passed";
        ws.Cell("D4").Value = passed;

        ws.Cell("C5").Value = "Failed";
        ws.Cell("D5").Value = failed;
    }

    private static void WriteHeader(IXLWorksheet ws)
    {
        var headers = new[]
        {
            "Ngày giờ",
            "Chức năng",
            "Mã test case",
            "Tiêu đề",
            "Các bước thực hiện",
            "Kết quả mong đợi",
            "Kết quả thực tế",
            "Kết luận",
            "Ghi chú",
            "Lần chạy",
            "Thời gian (ms)",
        };

        for (var col = 1; col <= headers.Length; col++)
        {
            ws.Cell(1, col).Value = headers[col - 1];
            ws.Cell(1, col).Style.Font.Bold = true;
            ws.Cell(1, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");
            ws.Cell(1, col).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }

        ws.SheetView.FreezeRows(1);
    }

    private static void WriteRowsSimple(IXLWorksheet ws, IReadOnlyList<TestRunRecord> records)
    {
        var row = 2;
        foreach (var r in records)
        {
            ws.Cell(row, 1).Value = r.StartedAt.LocalDateTime;
            ws.Cell(row, 2).Value = r.FunctionName;
            ws.Cell(row, 3).Value = r.CaseId;
            ws.Cell(row, 4).Value = r.Title;
            ws.Cell(row, 5).Value = r.Steps;
            ws.Cell(row, 6).Value = r.Expected;
            ws.Cell(row, 7).Value = r.Actual;
            ws.Cell(row, 8).Value = OutcomeToText(r.Outcome);
            ws.Cell(row, 9).Value = r.Note;
            ws.Cell(row, 10).Value = r.RunIndex;
            ws.Cell(row, 11).Value = (int)r.Duration.TotalMilliseconds;

            if (r.Outcome == TestOutcome.Pass)
                ws.Cell(row, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#C6EFCE");
            else if (r.Outcome == TestOutcome.Fail)
                ws.Cell(row, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFC7CE");
            else if (r.Outcome == TestOutcome.Skipped)
                ws.Cell(row, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFEB9C");

            row++;
        }
    }

    private static string OutcomeToText(TestOutcome outcome) => outcome switch
    {
        TestOutcome.Pass => "Đạt",
        TestOutcome.Fail => "Không đạt",
        TestOutcome.Skipped => "Bỏ qua",
        _ => "Chưa test",
    };

    private static void WriteRowsLikeTemplate(IXLWorksheet ws, IReadOnlyList<TestRunRecord> records)
    {
        // Template has header at row 7 and sample data starts at row 8.
        const int headerRow = 7;
        const int firstDataRow = 8;

        var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 19;
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? firstDataRow;

        // Clear sample contents but keep formatting/validations.
        if (lastRow >= firstDataRow)
        {
            ws.Range(firstDataRow, 1, lastRow, lastCol).Clear(XLClearOptions.Contents);
        }

        var headerMap = BuildHeaderMap(ws, headerRow, lastCol);
        var idCol = GetCol(headerMap, "ID") ?? 1;
        var itemsCol = GetCol(headerMap, "Items") ?? 2;
        var descCol = GetCol(headerMap, "Description");
        var preCol = GetCol(headerMap, "PreCondition");
        var stepsCol = GetCol(headerMap, "Steps to Excute") ?? GetCol(headerMap, "Steps to Execute");
        var expectedCol = GetCol(headerMap, "Expected output");
        var dataCol = GetCol(headerMap, "Test Data/Parameters") ?? GetCol(headerMap, "Test Data Parameters");
        var chromeCol = GetCol(headerMap, "Chrome");
        var dateCol = GetCol(headerMap, "Date");
        var noteCol = GetCol(headerMap, "Note");

        var row = firstDataRow;
        foreach (var r in records)
        {
            EnsureRowExists(ws, row, lastCol, templateRow: firstDataRow);

            ws.Row(row).Height = 33;

            ws.Cell(row, idCol).Value = $"{r.CaseId}-L{r.RunIndex:00}";
            ws.Cell(row, itemsCol).Value = SimplifyFunctionName(r.FunctionName);

            if (descCol is not null)
                ws.Cell(row, descCol.Value).Value = r.Title;
            if (preCol is not null)
                ws.Cell(row, preCol.Value).Value = "";
            if (stepsCol is not null)
                ws.Cell(row, stepsCol.Value).Value = r.Steps;
            if (expectedCol is not null)
                ws.Cell(row, expectedCol.Value).Value = r.Expected;
            if (dataCol is not null)
                ws.Cell(row, dataCol.Value).Value = r.Actual;

            if (chromeCol is not null)
                ws.Cell(row, chromeCol.Value).Value = OutcomeToMatrix(r.Outcome);

            if (dateCol is not null)
            {
                var cell = ws.Cell(row, dateCol.Value);
                cell.Value = r.StartedAt.LocalDateTime.Date;
                cell.Style.DateFormat.Format = "dd-MM-yy";
            }

            if (noteCol is not null)
                ws.Cell(row, noteCol.Value).Value = r.Note;

            ApplyRowTint(ws, row, lastCol, r.Outcome);
            row++;
        }

        // No auto-fit: avoid changing the template's look.
    }

    private static Dictionary<string, int> BuildHeaderMap(IXLWorksheet ws, int headerRow, int lastCol)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var col = 1; col <= lastCol; col++)
        {
            var text = ws.Cell(headerRow, col).GetString().Trim();
            if (string.IsNullOrWhiteSpace(text))
                continue;
            if (!map.ContainsKey(text))
                map[text] = col;
        }
        return map;
    }

    private static int? GetCol(Dictionary<string, int> headerMap, string header)
        => headerMap.TryGetValue(header, out var col) ? col : null;

    private static void EnsureRowExists(IXLWorksheet ws, int rowNumber, int lastCol, int templateRow)
    {
        var currentLastRow = ws.LastRowUsed()?.RowNumber() ?? templateRow;
        while (currentLastRow < rowNumber)
        {
            // Insert below the current last row and inherit style from the row above.
            ws.Row(currentLastRow).InsertRowsBelow(1);
            currentLastRow++;

            // Ensure the inserted row has the same base styling as template row.
            ws.Range(currentLastRow, 1, currentLastRow, lastCol).Style = ws.Range(templateRow, 1, templateRow, lastCol).Style;
        }
    }

    private static void ApplyRowTint(IXLWorksheet ws, int row, int lastCol, TestOutcome outcome)
    {
        var color = outcome switch
        {
            TestOutcome.Pass => XLColor.FromHtml("#E2F0D9"),
            TestOutcome.Fail => XLColor.FromHtml("#F8CBAD"),
            TestOutcome.Skipped => XLColor.FromHtml("#FFF2CC"),
            _ => XLColor.FromHtml("#FFFFFF"),
        };

        // Tint the main table area (A..S in the template).
        ws.Range(row, 1, row, lastCol).Style.Fill.BackgroundColor = color;
    }

    private static string OutcomeToMatrix(TestOutcome outcome) => outcome switch
    {
        TestOutcome.Pass => "Pass",
        TestOutcome.Fail => "Fail",
        _ => "Untested",
    };

    private static string SimplifyFunctionName(string functionName)
    {
        // E.g. "1. Phần hệ ..." -> "Phần hệ ..."
        var trimmed = functionName.Trim();
        var dotIndex = trimmed.IndexOf('.', StringComparison.Ordinal);
        if (dotIndex > 0 && dotIndex < 4)
        {
            var rest = trimmed[(dotIndex + 1)..].Trim();
            return string.IsNullOrWhiteSpace(rest) ? trimmed : rest;
        }

        return trimmed;
    }
}
