using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DevTeam.Core.Interfaces;

namespace DevTeam.Core.Services;

public class MarkdownStatusTracker : IStatusTracker
{
    private readonly string _statusFilePath;
    private readonly object _lock = new();

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
