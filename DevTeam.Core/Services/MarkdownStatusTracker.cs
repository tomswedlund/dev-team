using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevTeam.Core.Interfaces;

namespace DevTeam.Core.Services;

/// <summary>
/// File-based implementation of <see cref="IStatusTracker"/> that reads
/// and writes project status to <c>status.md</c>. The file contains two
/// markdown tables — one for phase statuses and one for task statuses —
/// which are updated in place or appended to as tasks and phases
/// progress through their lifecycle.
/// </summary>
public class MarkdownStatusTracker : IStatusTracker
{
    private readonly string _statusFilePath;
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new <see cref="MarkdownStatusTracker"/> writing to the
    /// specified directory.
    /// </summary>
    /// <param name="storageDirectory">Directory for the status file. Created if it does not exist.</param>
    public MarkdownStatusTracker(string storageDirectory = "storage")
    {
        if (!Directory.Exists(storageDirectory))
        {
            Directory.CreateDirectory(storageDirectory);
        }
        _statusFilePath = Path.Combine(storageDirectory, "status.md");
        InitializeStatusFile();
    }

    private void InitializeStatusFile()
    {
        if (!File.Exists(_statusFilePath))
        {
            string header = "# Project Status\n\n## Phases\n| Phase | Status |\n|---|---|\n\n## Tasks\n| Task ID | State | Phase | Update |\n|---|---|---|---|\n";
            File.WriteAllText(_statusFilePath, header, Encoding.UTF8);
        }
    }

    /// <inheritdoc/>
    public async Task UpdateTaskStatusAsync(string taskId, string state, string phaseId)
    {
        lock (_lock)
        {
            var lines = File.ReadAllLines(_statusFilePath).ToList();
            bool found = false;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains($"| {taskId} |"))
                {
                    // Simple table row replacement for updates
                    // Expected format: | Task ID | State | Phase | Update |
                    var parts = lines[i].Split('|');
                    if (parts.Length >= 4)
                    {
                        lines[i] = $"| {taskId} | {state} | {phaseId} | {DateTime.UtcNow:yyyy-MM-dd HH:mm} |";
                        found = true;
                    }
                }
            }

            if (!found)
            {
                lines.Add($"| {taskId} | {state} | {phaseId} | {DateTime.UtcNow:yyyy-MM-dd HH:mm} |");
            }

            File.WriteAllLines(_statusFilePath, lines, Encoding.UTF8);
        }
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<string> GetTaskStatusAsync(string taskId)
    {
        lock (_lock)
        {
            var lines = File.ReadAllLines(_statusFilePath);
            foreach (var line in lines)
            {
                if (line.Contains($"| {taskId} |"))
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 3) return parts[2].Trim();
                }
            }
        }
        return "Unknown";
    }

    /// <inheritdoc/>
    public async Task UpdatePhaseStatusAsync(string phaseId, string status)
    {
        lock (_lock)
        {
            var lines = File.ReadAllLines(_statusFilePath).ToList();
            bool found = false;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains($"| {phaseId} |"))
                {
                    var parts = lines[i].Split('|');
                    if (parts.Length >= 3)
                    {
                        lines[i] = $"| {phaseId} | {status} |";
                        found = true;
                    }
                }
            }

            if (!found)
            {
                // Find the header "## Phases" and insert under it
                int headerIdx = lines.FindIndex(l => l.Contains("## Phases"));
                if (headerIdx != -1)
                {
                    lines.Insert(headerIdx + 2, $"| {phaseId} | {status} |");
                }
            }

            File.WriteAllLines(_statusFilePath, lines, Encoding.UTF8);
        }
        await Task.CompletedTask;
    }
}