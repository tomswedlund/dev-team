using System.Collections.Generic;
using System.Threading.Tasks;

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
    /// <returns>A dictionary containing the task's specification fields.</returns>
    Task<Dictionary<string, object>> GetTaskSpecificationAsync(string taskId);
}