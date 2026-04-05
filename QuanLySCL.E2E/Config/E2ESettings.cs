using System.Globalization;

namespace QuanLySCL.E2E.Config;

public sealed record E2ESettings(
    Uri? BaseUrl,
    string? WordSpecPath,
    string? ExcelTemplatePath,
    string OutputDir,
    int RepeatEachCase)
{
    public static E2ESettings Load()
    {
        static string? GetEnv(string name)
            => Environment.GetEnvironmentVariable(name);

        Uri? baseUrl = null;
        var baseUrlRaw = GetEnv("E2E_BASE_URL");
        if (!string.IsNullOrWhiteSpace(baseUrlRaw) && Uri.TryCreate(baseUrlRaw.Trim(), UriKind.Absolute, out var parsed))
        {
            baseUrl = parsed;
        }

        var outputDir = GetEnv("E2E_OUTPUT_DIR");
        if (string.IsNullOrWhiteSpace(outputDir))
        {
            outputDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TestResults");
            outputDir = Path.GetFullPath(outputDir);
        }

        var repeatRaw = GetEnv("E2E_REPEAT");
        var repeat = 1;
        if (!string.IsNullOrWhiteSpace(repeatRaw) &&
            int.TryParse(repeatRaw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedRepeat) &&
            parsedRepeat > 0)
        {
            repeat = parsedRepeat;
        }

        var wordSpec = GetEnv("E2E_WORD_SPEC");
        if (string.IsNullOrWhiteSpace(wordSpec))
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var defaultPath = Path.Combine(desktop, "chucnang.docx");
            if (File.Exists(defaultPath))
                wordSpec = defaultPath;
        }

        var excelTemplate = GetEnv("E2E_EXCEL_TEMPLATE");
        if (string.IsNullOrWhiteSpace(excelTemplate))
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var defaultPath = Path.Combine(desktop, "TemplateTest.xlsx");
            if (File.Exists(defaultPath))
                excelTemplate = defaultPath;
        }

        return new E2ESettings(
            BaseUrl: baseUrl,
            WordSpecPath: wordSpec,
            ExcelTemplatePath: excelTemplate,
            OutputDir: outputDir,
            RepeatEachCase: repeat);
    }
}

