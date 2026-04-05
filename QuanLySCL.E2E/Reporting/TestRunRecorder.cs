using System.Collections.Concurrent;
using QuanLySCL.E2E.Model;

namespace QuanLySCL.E2E.Reporting;

public static class TestRunRecorder
{
    private static readonly ConcurrentBag<TestRunRecord> _records = [];

    public static IReadOnlyCollection<TestRunRecord> Records => _records.ToArray();

    public static void Add(TestRunRecord record) => _records.Add(record);
}

