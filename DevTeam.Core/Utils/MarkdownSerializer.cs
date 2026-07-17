using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DevTeam.Core.Models;

namespace DevTeam.Core.Utils;

/// <summary>
/// Utility for serializing and deserializing project status data to and
/// from a markdown file (<c>status.md</c>). The markdown format contains
/// two tables — one for phase statuses and one for task statuses — making
/// the status human-readable while remaining machine-parseable.
/// </summary>
public static class MarkdownSerializer
{
    /// <summary>
    /// Serializes phase and task status entries into a markdown string
    /// with two tables under "## Phases" and "## Tasks" headers.
    /// </summary>
    /// <param name="phases">Collection of phase status entries to serialize.</param>
    /// <param name="tasks">Collection of task status entries to serialize.</param>
    /// <returns>A markdown string representing the full project status.</returns>
    public static string SerializeStatus(
        IEnumerable<PhaseStatusEntry> phases,
        IEnumerable<TaskStatusEntry> tasks)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Project Status");
        sb.AppendLine();

        // Phases table
        sb.AppendLine("## Phases");
        sb.AppendLine("| Phase | Status |");
        sb.AppendLine("|---|---|");
        foreach (var phase in phases)
        {
            sb.AppendLine($"| {phase.PhaseId} | {phase.Status} |");
        }

        sb.AppendLine();

        // Tasks table
        sb.AppendLine("## Tasks");
        sb.AppendLine("| Task ID | State | Phase | Update |");
        sb.AppendLine("|---|---|---|---|");
        foreach (var task in tasks)
        {
            sb.AppendLine($"| {task.TaskId} | {task.State} | {task.PhaseId} | {task.LastUpdated:yyyy-MM-dd HH:mm} |");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Parses a markdown status string (as produced by <see cref="SerializeStatus"/>)
    /// back into phase and task status entry collections.
    /// </summary>
    /// <param name="markdown">The markdown content to parse.</param>
    /// <returns>A tuple of (phases, tasks) parsed from the markdown.</returns>
    public static (List<PhaseStatusEntry> Phases, List<TaskStatusEntry> Tasks) ParseStatus(string markdown)
    {
        var phases = new List<PhaseStatusEntry>();
        var tasks = new List<TaskStatusEntry>();

        var lines = markdown.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        string? section = null;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r').Trim();

            if (line.StartsWith("## Phases"))
            {
                section = "phases";
                continue;
            }
            if (line.StartsWith("## Tasks"))
            {
                section = "tasks";
                continue;
            }
            if (string.IsNullOrEmpty(section) || !line.StartsWith("|"))
                continue;

            // Skip separator rows (|---|---|)
            if (line.Contains("---"))
                continue;

            var parts = line.Split('|', StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim())
                            .ToArray();

            if (section == "phases" && parts.Length >= 2)
            {
                phases.Add(new PhaseStatusEntry
                {
                    PhaseId = parts[0],
                    Status = parts[1]
                });
            }
            else if (section == "tasks" && parts.Length >= 4)
            {
                DateTime.TryParseExact(parts[3], "yyyy-MM-dd HH:mm",
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal,
                    out var lastUpdated);

                tasks.Add(new TaskStatusEntry
                {
                    TaskId = parts[0],
                    State = parts[1],
                    PhaseId = parts[2],
                    LastUpdated = lastUpdated
                });
            }
        }

        return (phases, tasks);
    }

    /// <summary>
    /// Loads and parses a status markdown file from disk.
    /// </summary>
    /// <param name="filePath">Path to the markdown status file.</param>
    /// <returns>A tuple of (phases, tasks) parsed from the file, or empty collections if the file does not exist.</returns>
    public static (List<PhaseStatusEntry> Phases, List<TaskStatusEntry> Tasks) LoadStatus(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return (new List<PhaseStatusEntry>(), new List<TaskStatusEntry>());
        }

        string content = File.ReadAllText(filePath, Encoding.UTF8);
        return ParseStatus(content);
    }

    /// <summary>
    /// Serializes phase and task status entries to a markdown file on disk.
    /// </summary>
    /// <param name="filePath">Path to the output markdown file.</param>
    /// <param name="phases">Collection of phase status entries to write.</param>
    /// <param name="tasks">Collection of task status entries to write.</param>
    public static void SaveStatus(string filePath, IEnumerable<PhaseStatusEntry> phases, IEnumerable<TaskStatusEntry> tasks)
    {
        string content = SerializeStatus(phases, tasks);
        File.WriteAllText(filePath, content, Encoding.UTF8);
    }
}