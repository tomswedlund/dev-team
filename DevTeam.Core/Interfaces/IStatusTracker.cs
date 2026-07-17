using System.Threading.Tasks;
using DevTeam.Core.Models;

namespace DevTeam.Core.Interfaces;

/// <summary>
/// Provides read and update operations for task and phase status within
/// a <see cref="Project"/>. Acts as the live, in-memory state manager that
/// persists changes to disk after every mutation.
/// </summary>
public interface IStatusTracker
{
    /// <summary>
    /// Updates the state of a specific task within its phase.
    /// If the task exists, its state and iteration are updated; otherwise
    /// the task is not found and the call is a no-op.
    /// </summary>
    /// <param name="taskId">Unique identifier of the task (e.g. "A-04").</param>
    /// <param name="state">The new task state.</param>
    /// <returns>A task that completes when the status has been updated and persisted.</returns>
    Task UpdateTaskStatusAsync(string taskId, TaskState state);

    /// <summary>
    /// Reads the current state of a specific task.
    /// </summary>
    /// <param name="taskId">Unique identifier of the task.</param>
    /// <returns>The task's current state, or <see cref="TaskState.NotStarted"/> if not found.</returns>
    Task<TaskState> GetTaskStatusAsync(string taskId);

    /// <summary>
    /// Updates the overall status of a phase.
    /// </summary>
    /// <param name="phaseId">Unique identifier of the phase.</param>
    /// <param name="status">The new phase status.</param>
    /// <returns>A task that completes when the status has been updated and persisted.</returns>
    Task UpdatePhaseStatusAsync(string phaseId, PhaseStatus status);

    /// <summary>
    /// Gets the current live <see cref="Project"/> object.
    /// </summary>
    /// <returns>The project instance being tracked.</returns>
    Task<Project> GetProjectAsync();

    /// <summary>
    /// Saves the current project state to disk in both markdown and JSON formats.
    /// </summary>
    /// <returns>A task that completes when persistence is done.</returns>
    Task SaveAsync();
}