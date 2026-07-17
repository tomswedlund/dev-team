using System;
using System.Collections.Generic;

namespace DevTeam.Core.Models;

/// <summary>
/// Report produced by the Reviewer agent after reviewing a task's code.
/// Contains the review verdict, specific findings, and recommended actions.
/// </summary>
public class ReviewReport
{
    /// <summary>
    /// Identifier of the task this review covers.
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// The reviewer's overall verdict.
    /// </summary>
    public ReviewVerdict Verdict { get; set; } = ReviewVerdict.RequestChanges;

    /// <summary>
    /// Overall summary of the review.
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Specific findings identified during the review, categorized by severity.
    /// </summary>
    public List<ReviewFinding> Findings { get; set; } = [];

    /// <summary>
    /// Timestamp (UTC) when the review was performed.
    /// </summary>
    public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// The reviewer's overall judgment on the code.
/// </summary>
public enum ReviewVerdict
{
    /// <summary>Code meets standards — task can proceed to completion.</summary>
    Approved,

    /// <summary>Code needs changes — task goes back for another iteration.</summary>
    RequestChanges
}

/// <summary>
/// A single issue or observation identified during code review.
/// </summary>
public class ReviewFinding
{
    /// <summary>
    /// What category of issue this is (e.g. style, correctness, coverage).
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Severity of the finding.
    /// </summary>
    public FindingSeverity Severity { get; set; } = FindingSeverity.Info;

    /// <summary>
    /// Description of the issue.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Specific file and line reference, if applicable (e.g. "src/buses.cs:42").
    /// </summary>
    public string? Location { get; set; }
}

/// <summary>
/// Severity level for review findings.
/// </summary>
public enum FindingSeverity
{
    /// <summary>Informational — no action required.</summary>
    Info,

    /// <summary>Suggestion — improvement recommended.</summary>
    Suggestion,

    /// <summary>Warning — should be addressed but not blocking.</summary>
    Warning,

    /// <summary>Error — must be fixed before approval.</summary>
    Error
}