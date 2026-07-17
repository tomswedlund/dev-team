using System.Collections.Generic;
using System.Threading.Tasks;
using DevTeam.Core.Models;

namespace DevTeam.Core.Interfaces;

/// <summary>
/// Provides access to task specifications — the detailed definitions
/// of each task in a phase, including requirements, acceptance criteria,
/// dependencies, and estimated effort. Task specs are loaded from
/// YAML files and cached for quick access.
/// </summary>
public interface ITaskSpecService
{
    /// <summary>
    /// Retrieves the full specification for a given task.
    /// </summary>
    /// <param name="taskId">Unique identifier of the task (e.g. "A-04").</param>
    /// <returns>The <see cref="TaskSpec"/> for the requested task, or null if not found.</returns>
    Task<TaskSpec?> GetTaskSpecificationAsync(string taskId);

    /// <summary>
    /// Retrieves all task specifications for a given phase.
    /// </summary>
    /// <param name="phaseId">Unique identifier of the phase (e.g. "A").</param>
    /// <returns>A list of <see cref="TaskSpec"/> objects for the phase, or an empty list if none found.</returns>
    Task<List<TaskSpec>> GetPhaseTasksAsync(string phaseId);

    /// <summary>
    /// Saves a phase plan (the planner's output) to persistent storage.
    /// </summary>
    /// <param name="plan">The phase plan to save.</param>
    /// <returns>A task that completes when the plan has been saved.</returns>
    Task SavePhasePlanAsync(PhasePlan plan);
}