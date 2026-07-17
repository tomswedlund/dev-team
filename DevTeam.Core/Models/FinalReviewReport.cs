using System;
using System.Collections.Generic;

namespace DevTeam.Core.Models;

/// <summary>
/// Comprehensive final review report produced by the Planner agent after all
/// phases are complete. Reviews the entire codebase against the original
/// requirements, checking for style consistency, documentation, and test coverage.
/// Saved as <c>reports/FINAL-REVIEW.md</c>.
/// </summary>
public class FinalReviewReport
{
    /// <summary>
    /// Name of the project being reviewed.
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Overall summary of the project and its completion status.
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Whether the project passed final review.
    /// </summary>
    public FinalReviewStatus Status { get; set; } = FinalReviewStatus.NotStarted;

    /// <summary>
    /// Assessment of code style consistency across the codebase.
    /// </summary>
    public string StyleAssessment { get; set; } = string.Empty;

    /// <summary>
    /// Assessment of documentation quality and completeness.
    /// </summary>
    public string DocumentationAssessment { get; set; } = string.Empty;

    /// <summary>
    /// Assessment of test coverage and quality.
    /// </summary>
    public string TestCoverageAssessment { get; set; } = string.Empty;

    /// <summary>
    /// Assessment of whether the implementation meets the original requirements.
    /// </summary>
    public string RequirementsComplianceAssessment { get; set; } = string.Empty;

    /// <summary>
    /// Remaining issues or recommendations after the final review.
    /// </summary>
    public List<string> OutstandingIssues { get; set; } = [];

    /// <summary>
    /// Per-phase review summaries.
    /// </summary>
    public List<PhaseReviewSummary> PhaseReviews { get; set; } = [];

    /// <summary>
    /// Timestamp (UTC) when the final review was produced.
    /// </summary>
    public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Status of the final project review.
/// </summary>
public enum FinalReviewStatus
{
    /// <summary>Final review has not yet started.</summary>
    NotStarted,

    /// <summary>Final review is in progress.</summary>
    InProgress,

    /// <summary>Final review completed and project is approved.</summary>
    Passed,

    /// <summary>Final review found issues that need addressing.</summary>
    Failed
}

/// <summary>
/// Summary of a single phase's review within the final project review.
/// </summary>
public class PhaseReviewSummary
{
    /// <summary>
    /// Identifier of the phase reviewed.
    /// </summary>
    public string PhaseId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the phase reviewed.
    /// </summary>
    public string PhaseName { get; set; } = string.Empty;

    /// <summary>
    /// PR number associated with this phase, if any.
    /// </summary>
    public int? PrNumber { get; set; }

    /// <summary>
    /// Merge status of the phase's PR.
    /// </summary>
    public string PrStatus { get; set; } = string.Empty;

    /// <summary>
    /// Number of tasks in the phase.
    /// </summary>
    public int TaskCount { get; set; }

    /// <summary>
    /// Number of tasks that were approved.
    /// </summary>
    public int ApprovedCount { get; set; }

    /// <summary>
    /// Summary of the phase's review findings.
    /// </summary>
    public string Summary { get; set; } = string.Empty;
}