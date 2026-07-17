using System.IO;
using System.Threading.Tasks;
using DevTeam.Core.Interfaces;
using DevTeam.Core.Models;
using DevTeam.Core.Utils;

namespace DevTeam.Core.Services;

/// <summary>
/// In-memory implementation of <see cref="IStatusTracker"/> that wraps a
/// <see cref="Project"/> and persists state changes to disk after every
/// mutation. State is stored in two formats:
/// <list type="bullet">
///   <item><b>status.md</b> — human-readable markdown (via <see cref="MarkdownSerializer"/>).</item>
///   <item><b>status.json</b> — lossless JSON for session resume.</item>
/// </list>
/// </summary>
public class StatusTracker : IStatusTracker
{
    private readonly string _storageDirectory;
    private readonly string _markdownPath;
    private readonly string _jsonPath;
    private readonly object _lock = new();

    /// <summary>
    /// The live project state managed by this tracker.
    /// </summary>
    public Project Project { get; private set; }

    /// <summary>
    /// Creates a new <see cref="StatusTracker"/>. If a <c>status.json</c>
    /// file exists in <paramref name="storageDirectory"/>, the project
    /// is loaded from it; otherwise a fresh <see cref="Project"/> is
    /// created.
    /// </summary>
    /// <param name="storageDirectory">Directory for status files. Created if it does not exist.</param>
    /// <param name="project">Optional pre-built project (skips loading from disk).</param>
    public StatusTracker(string storageDirectory = "storage", Project? project = null)
    {
        if (!Directory.Exists(storageDirectory))
            Directory.CreateDirectory(storageDirectory);

        _storageDirectory = storageDirectory;
        _markdownPath = Path.Combine(storageDirectory, "status.md");
        _jsonPath = Path.Combine(storageDirectory, "status.json");

        if (project is not null)
        {
            Project = project;
        }
        else if (File.Exists(_jsonPath))
        {
            Project = MarkdownSerializer.LoadJson(_jsonPath);
        }
        else
        {
            Project = new Project();
        }
    }

    /// <inheritdoc/>
    public Task UpdateTaskStatusAsync(string taskId, TaskState state)
    {
        lock (_lock)
        {
            var task = Project.GetTask(taskId);
            if (task is not null)
            {
                task.State = state;
                Project.LastUpdated = System.DateTime.UtcNow;
                SaveToDisk();
            }
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<TaskState> GetTaskStatusAsync(string taskId)
    {
        lock (_lock)
        {
            var task = Project.GetTask(taskId);
            return Task.FromResult(task?.State ?? TaskState.NotStarted);
        }
    }

    /// <inheritdoc/>
    public Task UpdatePhaseStatusAsync(string phaseId, PhaseStatus status)
    {
        lock (_lock)
        {
            var phase = Project.GetPhase(phaseId);
            if (phase is not null)
            {
                phase.Status = status;
                Project.LastUpdated = System.DateTime.UtcNow;
                SaveToDisk();
            }
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<Project> GetProjectAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(Project);
        }
    }

    /// <inheritdoc/>
    public Task SaveAsync()
    {
        lock (_lock)
        {
            Project.LastUpdated = System.DateTime.UtcNow;
            SaveToDisk();
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Serializes the current project state to a markdown string
    /// (does not write to disk).
    /// </summary>
    /// <returns>A markdown representation of the project.</returns>
    public string ToMarkdown()
    {
        lock (_lock)
        {
            return MarkdownSerializer.ToMarkdown(Project);
        }
    }

    private void SaveToDisk()
    {
        MarkdownSerializer.SaveMarkdown(_markdownPath, Project);
        MarkdownSerializer.SaveJson(_jsonPath, Project);
    }
}