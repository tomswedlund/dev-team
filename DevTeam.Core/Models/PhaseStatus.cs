namespace DevTeam.Core.Models;

/// <summary>
/// Represents the lifecycle state of a phase within the project.
/// Phases progress from planning through execution, PR review, and
/// completion.
/// </summary>
public enum PhaseStatus
{
    /// <summary>Phase plan has been created but not yet started.</summary>
    NotStarted,

    /// <summary>Tasks in this phase are being executed by DevTeams.</summary>
    InProgress,

    /// <summary>All tasks complete; PR is ready for User review.</summary>
    Reviewing,

    /// <summary>Phase PR has been merged and the phase is complete.</summary>
    Complete,

    /// <summary>Phase failed — one or more tasks exceeded max iterations.</summary>
    Failed
}