using NUnit.Framework;
using QuanLySCL.E2E.Config;
using QuanLySCL.E2E.Reporting;

namespace QuanLySCL.E2E;

[SetUpFixture]
public sealed class TestRunReportHooks
{
    [OneTimeTearDown]
    public void WriteExcelReport()
    {
        var settings = E2ESettings.Load();
        var records = TestRunRecorder.Records;
        if (records.Count == 0)
            return;

        var output = ExcelReportWriter.WriteReport(settings.OutputDir, settings.ExcelTemplatePath, records);
        TestContext.Progress.WriteLine($"Excel report written: {output}");
    }
}

