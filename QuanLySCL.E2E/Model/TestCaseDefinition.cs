namespace QuanLySCL.E2E.Model;

public sealed record TestCaseDefinition(
    string FunctionName,
    string CaseId,
    string Title,
    string Steps,
    string Expected);

