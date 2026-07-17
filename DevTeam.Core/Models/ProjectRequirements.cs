using System;
using System.Collections.Generic;

namespace DevTeam.Core.Models;

/// <summary>
/// Represents the full requirements document for a project, produced
/// interactively by the Planner agent. Requirements are gathered from the
/// User, refined through clarifying questions, and then approved before
/// phase/task decomposition begins.
/// </summary>
public class ProjectRequirements
{
    /// <summary>
    /// Name of the project these requirements describe.
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// High-level summary of the project's purpose and goals.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The individual requirement items that make up the specification.
    /// </summary>
    public List<RequirementItem> Requirements { get; set; } = [];

    /// <summary>
    /// Current approval status of the requirements document.
    /// </summary>
    public RequirementsStatus Status { get; set; } = RequirementsStatus.Draft;

    /// <summary>
    /// Identifier of the user who approved or rejected the requirements.
    /// </summary>
    public string? ApprovedBy { get; set; }

    /// <summary>
    /// Timestamp (UTC) when the requirements were approved or rejected.
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Relative path to the requirements document (e.g. "requirements/REQUIREMENTS.md").
    /// </summary>
    public string FilePath { get; set; } = "requirements/REQUIREMENTS.md";
}

/// <summary>
/// Represents the lifecycle state of a requirements document.
/// </summary>
public enum RequirementsStatus
{
    /// <summary>Being written — not yet presented to the User for approval.</summary>
    Draft,

    /// <summary>Presented to the User, awaiting their decision.</summary>
    PendingApproval,

    /// <summary>User has approved the requirements; decomposition can begin.</summary>
    Approved,

    /// <summary>User has rejected or requested changes; requirements need revision.</summary>
    Rejected
}