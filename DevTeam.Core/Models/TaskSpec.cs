using System.Collections.Generic;

namespace DevTeam.Core.Models;

/// <summary>
/// Represents the full specification of a single task within a phase.
/// Task specs are loaded from YAML files (one per phase) and contain
/// everything an agent team needs to implement the task: the task
/// identifier, human-readable name, description, acceptance criteria,
/// dependencies on other tasks, estimated effort, and the phase it
/// belongs to.
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
}