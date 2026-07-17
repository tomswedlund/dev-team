using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DevTeam.Core.Interfaces;
using DevTeam.Core.Models;
using DevTeam.Core.Utils;

namespace DevTeam.Core.Services;

/// <summary>
/// In-memory implementation of <see cref="IStatusTracker"/> that maintains
/// the current status of all phases and tasks. State is persisted to disk
/// by delegating serialization to <see cref="MarkdownSerializer"/>, so the
/// tracker itself is format-agnostic and can be used with any persistence
/// strategy by replacing the serializer.
/// </summary>
public class StatusTracker : IStatusTracker
{
    private readonly string _statusFilePath;
    private readonly object _lock = new();

    private readonly Dictionary<string, PhaseStatusEntry> _phases = [];
    private readonly Dictionary<string, TaskStatusEntry> _tasks = [];

    /// <summary>
    /// Creates a new <see cref="StatusTracker"/> that persists status to
    /// <c>status.md</c> in the specified directory. If the file already
    /// exists, its contents are loaded into memory.
    /// </summary>
    /// <param name="storageDirectory">Directory for the status file. Created if it does not exist.</param>
    public StatusTracker(string storageDirectory = "storage")
    {
        if (!Directory.Exists(storageDirectory))
        {
            Directory.CreateDirectory(storageDirectory);
        }
        _statusFilePath = Path.Combine(storageDirectory, "status.md");
        LoadFromDisk();
    }

    private void LoadFromDisk()
    {
        var (phases, tasks) = MarkdownSerializer.LoadStatus(_statusFilePath);
        foreach (var phase in phases)
        {
            _phases[phase.PhaseId] = phase;
        }
        foreach (var task in tasks)
        {
            _tasks[task.TaskId] = task;
        }
    }

    private void SaveToDisk()
    {
        MarkdownSerializer.SaveStatus(_statusFilePath, _phases.Values, _tasks.Values);
    }

    /// <inheritdoc/>
    public Task UpdateTaskStatusAsync(string taskId, string state, string phaseId)
    {
        lock (_lock)
        {
            if (_tasks.TryGetValue(taskId, out var existing))
            {
                existing.State = state;
                existing.PhaseId = phaseId;
                existing.LastUpdated = System.DateTime.UtcNow;
            }
            else
            {
                _tasks[taskId] = new TaskStatusEntry
                {
                    TaskId = taskId,
                    State = state,
                    PhaseId = phaseId,
                    LastUpdated = System.DateTime.UtcNow
                };
            }
            SaveToDisk();
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<string> GetTaskStatusAsync(string taskId)
    {
        lock (_lock)
        {
            if (_tasks.TryGetValue(taskId, out var entry))
            {
                return Task.FromResult(entry.State);
            }
        }
        return Task.FromResult("Unknown");
    }

    /// <inheritdoc/>
    public Task UpdatePhaseStatusAsync(string phaseId, string status)
    {
        lock (_lock)
        {
            if (_phases.TryGetValue(phaseId, out var existing))
            {
                existing.Status = status;
            }
            else
            {
                _phases[phaseId] = new PhaseStatusEntry
                {
                    PhaseId = phaseId,
                    Status = status
                };
            }
            SaveToDisk();
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Serializes the current in-memory status to a markdown string.
    /// </summary>
    /// <returns>A markdown representation of all phase and task statuses.</returns>
    public string ToMarkdown()
    {
        lock (_lock)
        {
            return MarkdownSerializer.SerializeStatus(_phases.Values, _tasks.Values);
        }
    }
}