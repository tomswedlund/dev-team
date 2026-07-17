using System.Threading.Tasks;

namespace DevTeam.Core.Interfaces;

/// <summary>
/// Tracks the status of tasks and phases in a markdown file (<c>status.md</c>).
/// Provides read and update operations for both individual tasks and
/// overall phase status, enabling the orchestrator and agents to query
/// and modify project progress.
/// </summary>
public interface IStatusTracker
{
    /// <summary>
    /// Updates the status of a specific task. If the task already exists
    /// in the status file, its state is updated; otherwise, a new row
    /// is appended.
    /// </summary>
    /// <param name="taskId">Unique identifier of the task.</param>
    /// <param name="state">New state value (e.g. "planned", "coding", "testing", "review", "done").</param>
    /// <param name="phaseId">Identifier of the phase the task belongs to.</param>
    /// <returns>A task that completes when the status has been updated.</returns>
    Task UpdateTaskStatusAsync(string taskId, string state, string phaseId);

    /// <summary>
    /// Reads the current status of a specific task.
    /// </summary>
    /// <param name="taskId">Unique identifier of the task.</param>
    /// <returns>The task's current state, or "Unknown" if not found.</returns>
    Task<string> GetTaskStatusAsync(string taskId);

    /// <summary>
    /// Updates the overall status of a phase (e.g. "in_progress",
    /// "complete"). If the phase already exists in the status file,
    /// its status is updated; otherwise, a new row is inserted.
    /// </summary>
    /// <param name="phaseId">Unique identifier of the phase.</param>
    /// <param name="status">New phase status (e.g. "pending", "in_progress", "complete").</param>
    /// <returns>A task that completes when the status has been updated.</returns>
    Task UpdatePhaseStatusAsync(string phaseId, string status);
}