using System.Collections.Generic;

namespace DevTeam.Core.Models;

/// <summary>
/// Represents the full specification of a single task within a phase,
/// including its definition, acceptance criteria, dependencies, and
/// runtime status. Task specs are loaded from YAML/markdown files and
/// carry their own lifecycle state so the system can be paused and
/// resumed without losing progress.
/// </summary>
public class TaskSpec
{
    /// <summary>
    /// Unique identifier of the task (e.g. "A-04").
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name of the task (e.g. "Implement FilePersistence").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The phase this task belongs to (e.g. "A", "B", "C").
    /// </summary>
    public string PhaseId { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of what needs to be done, including
    /// technical requirements and constraints.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// List of task IDs that must be completed before this task can
    /// start. An empty list means the task has no dependencies.
    /// </summary>
    public List<string> Dependencies { get; set; } = [];

    /// <summary>
    /// Criteria that must be met for the task to be considered
    /// complete (e.g. "Build succeeds", "All tests pass").
    /// </summary>
    public List<string> AcceptanceCriteria { get; set; } = [];

    /// <summary>
    /// Estimated effort in person-days (e.g. 0.5, 1.0, 2.5).
    /// </summary>
    public double EstimatedEffortDays { get; set; }

    /// <summary>
    /// The concrete deliverable produced by this task (e.g.
    /// "FilePersistence.cs with WAL append and replay").
    /// </summary>
    public string Deliverable { get; set; } = string.Empty;

    // ── Runtime Status (persisted for resume capability) ──────────────

    /// <summary>
    /// Current lifecycle state of the task.
    /// </summary>
    public TaskState State { get; set; } = TaskState.NotStarted;

    /// <summary>
    /// Current iteration count — incremented each time the task goes
    /// through a review/feedback cycle.
    /// </summary>
    public int Iteration { get; set; }

    /// <summary>
    /// Maximum iterations allowed before the task is marked as Failed.
    /// </summary>
    public int MaxIterations { get; set; } = 3;

    /// <summary>
    /// Identifier of the DevTeam assigned to this task (e.g. "DT-001"),
    /// or null if no team has been assigned yet.
    /// </summary>
    public string? AssignedDevTeamId { get; set; }

    /// <summary>
    /// Free-text notes — typically review feedback or status details
    /// from the most recent iteration.
    /// </summary>
    public string? Notes { get; set; }
}