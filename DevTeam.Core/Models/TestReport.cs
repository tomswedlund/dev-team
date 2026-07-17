using System;
using System.Collections.Generic;

namespace DevTeam.Core.Models;

/// <summary>
/// Report produced by the Tester agent after running tests for a task.
/// Includes the tests written, coverage summary, any skipped tests, and
/// errors or bugs discovered during testing.
/// </summary>
public class TestReport
{
    /// <summary>
    /// Identifier of the task this test report covers.
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// List of test entries with file and line references.
    /// </summary>
    public List<TestEntry> Tests { get; set; } = [];

    /// <summary>
    /// Summary of code coverage (e.g. "85% lines, 72% branches").
    /// </summary>
    public string CoverageSummary { get; set; } = string.Empty;

    /// <summary>
    /// Tests that were skipped and the reason why.
    /// </summary>
    public List<(string TestName, string Reason)> SkippedTests { get; set; } = [];

    /// <summary>
    /// Errors or bugs discovered during testing.
    /// </summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// Whether all tests passed.
    /// </summary>
    public bool AllPassed { get; set; }

    /// <summary>
    /// Timestamp (UTC) when the test report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a single test case in a test report.
/// </summary>
public class TestEntry
{
    /// <summary>
    /// Name of the test.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// File path where the test is defined.
    /// </summary>
    public string File { get; set; } = string.Empty;

    /// <summary>
    /// Line number where the test starts.
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Whether the test passed, failed, or was skipped.
    /// </summary>
    public TestResult Result { get; set; } = TestResult.Passed;

    /// <summary>
    /// Error message if the test failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Outcome of a single test execution.
/// </summary>
public enum TestResult
{
    /// <summary>Test executed and passed.</summary>
    Passed,

    /// <summary>Test executed and failed.</summary>
    Failed,

    /// <summary>Test was not executed (skipped).</summary>
    Skipped
}