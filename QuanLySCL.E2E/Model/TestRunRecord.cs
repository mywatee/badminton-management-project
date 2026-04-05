namespace QuanLySCL.E2E.Model;

public sealed record TestRunRecord(
    DateTimeOffset StartedAt,
    TimeSpan Duration,
    string FunctionName,
    string CaseId,
    string Title,
    string Steps,
    string Expected,
    int RunIndex,
    TestOutcome Outcome,
    string Actual,
    string Note);
