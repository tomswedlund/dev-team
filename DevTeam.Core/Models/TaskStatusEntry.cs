using System;

namespace DevTeam.Core.Models;

/// <summary>
/// Represents the status of a single task within a phase, including
/// its current state and the timestamp of the last update.
/// </summary>
public class TaskStatusEntry
{
    /// <summary>
    /// Unique identifier of the task (e.g. "A-04").
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// Current state of the task (e.g. "planned", "coding", "testing", "review", "done").
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the phase the task belongs to.
    /// </summary>
    public string PhaseId { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp of the last status update.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}