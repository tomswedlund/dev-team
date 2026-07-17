using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using DevTeam.Core.Models;

namespace DevTeam.Core.Utils;

/// <summary>
/// Utility for serializing and deserializing a <see cref="Project"/> to and
/// from two formats:
/// <list type="bullet">
///   <item><b>Markdown</b> — human-readable for PR review and manual inspection.</item>
///   <item><b>JSON</b> — machine-readable for fast, lossless session resume.</item>
/// </list>
/// </summary>
public static class MarkdownSerializer
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // ═══════════════════════════════════════════════════════════════════
    //  Markdown serialisation
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Serializes a <see cref="Project"/> into a human-readable markdown
    /// string containing project metadata, requirements, phases with
    /// tasks tables, addenda, open questions, and a progress summary.
    /// </summary>
    public static string ToMarkdown(Project project)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine($"# {project.Name}");
        sb.AppendLine();
        if (!string.IsNullOrEmpty(project.Description))
        {
            sb.AppendLine(project.Description);
            sb.AppendLine();
        }
        sb.AppendLine($"*Last updated: {project.LastUpdated:yyyy-MM-dd HH:mm}Z*");
        sb.AppendLine();

        // Requirements
        sb.AppendLine("## Requirements");
        sb.AppendLine();
        sb.AppendLine($"- Status: {project.Requirements.Status}");
        sb.AppendLine($"- Document: {project.Requirements.FilePath}");
        if (project.Requirements.ApprovedBy is not null)
        {
            var approvedAt = project.Requirements.ApprovedAt?.ToString("yyyy-MM-dd") ?? "";
            sb.AppendLine($"- Approved by: {project.Requirements.ApprovedBy} ({approvedAt})");
        }
        if (project.Requirements.Requirements.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("| ID | Category | Priority | Description |");
            sb.AppendLine("|---|---|---|---|");
            foreach (var req in project.Requirements.Requirements)
            {
                sb.AppendLine($"| {req.Id} | {req.Category} | {req.Priority} | {req.Description} |");
            }
        }
        sb.AppendLine();

        // Phases
        sb.AppendLine("## Phases");
        sb.AppendLine();
        foreach (var phase in project.Phases)
        {
            sb.AppendLine($"### Phase {phase.PhaseId}: {phase.Name}");
            sb.AppendLine();
            sb.AppendLine($"- Status: {phase.Status}");
            if (phase.PrNumber is not null)
                sb.AppendLine($"- PR: #{phase.PrNumber} ({phase.PrStatus ?? "pending"})");
            sb.AppendLine($"- Goal: {phase.Goal}");
            sb.AppendLine();

            if (phase.Tasks.Count > 0)
            {
                sb.AppendLine("| Task ID | Name | State | Iter | Max | DevTeam | Notes |");
                sb.AppendLine("|---|---|---|---|---|---|---|");
                foreach (var task in phase.Tasks)
                {
                    var notes = task.Notes ?? "";
                    var devTeam = task.AssignedDevTeamId ?? "";
                    sb.AppendLine($"| {task.TaskId} | {task.Name} | {task.State} | {task.Iteration} | {task.MaxIterations} | {devTeam} | {notes} |");
                }
                sb.AppendLine();
            }

            if (phase.Addenda.Count > 0)
            {
                sb.AppendLine("#### Addenda");
                sb.AppendLine();
                foreach (var addendum in phase.Addenda)
                {
                    sb.AppendLine($"**Addendum {addendum.AddendumId}** — {addendum.Reason} ({addendum.Status})");
                    sb.AppendLine();
                    if (addendum.Tasks.Count > 0)
                    {
                        sb.AppendLine("| Task ID | Name | State | Notes |");
                        sb.AppendLine("|---|---|---|---|");
                        foreach (var task in addendum.Tasks)
                        {
                            sb.AppendLine($"| {task.TaskId} | {task.Name} | {task.State} | {task.Notes ?? ""} |");
                        }
                        sb.AppendLine();
                    }
                }
            }

            if (phase.PhaseGates.Count > 0)
            {
                sb.AppendLine("**Phase Gates:**");
                foreach (var gate in phase.PhaseGates)
                {
                    sb.AppendLine($"- [ ] {gate}");
                }
                sb.AppendLine();
            }
        }

        // Open Questions
        if (project.OpenQuestions.Count > 0)
        {
            sb.AppendLine("## Open Questions");
            sb.AppendLine();
            sb.AppendLine("| # | From | Question | Status | Answer |");
            sb.AppendLine("|---|---|----------|--------|--------|");
            foreach (var q in project.OpenQuestions)
            {
                sb.AppendLine($"| {q.Id} | {q.From} | {q.Question} | {q.Status} | {q.Answer ?? ""} |");
            }
            sb.AppendLine();
        }

        // Final Review
        if (project.FinalReview is not null)
        {
            sb.AppendLine("## Final Review");
            sb.AppendLine();
            sb.AppendLine($"- Status: {project.FinalReview.Status}");
            sb.AppendLine($"- Summary: {project.FinalReview.Summary}");
            sb.AppendLine();
        }

        // Summary
        sb.AppendLine("## Project Summary");
        sb.AppendLine($"- Total Phases: {project.TotalPhases}");
        sb.AppendLine($"- Completed: {project.CompletedPhases}");
        sb.AppendLine($"- Overall Progress: {project.ProgressPercent}%");
        sb.AppendLine($"- Final Review Status: {project.FinalReview?.Status ?? FinalReviewStatus.NotStarted}");

        return sb.ToString();
    }

    /// <summary>
    /// Writes a <see cref="Project"/> as markdown to the specified file path.
    /// </summary>
    public static void SaveMarkdown(string filePath, Project project)
    {
        var content = ToMarkdown(project);
        var dir = Path.GetDirectoryName(filePath);
        if (dir is not null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(filePath, content, Encoding.UTF8);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  JSON serialisation (lossless, for resume)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Serializes a <see cref="Project"/> to a JSON string (indented, camelCase).
    /// This is the lossless format for saving/loading session state.
    /// </summary>
    public static string ToJson(Project project) =>
        JsonSerializer.Serialize(project, JsonOpts);

    /// <summary>
    /// Deserializes a JSON string back into a <see cref="Project"/>.
    /// </summary>
    public static Project FromJson(string json) =>
        JsonSerializer.Deserialize<Project>(json, JsonOpts)
            ?? throw new InvalidDataException("Failed to deserialize Project from JSON.");

    /// <summary>
    /// Saves a <see cref="Project"/> as JSON to the specified file path.
    /// </summary>
    public static void SaveJson(string filePath, Project project)
    {
        var content = ToJson(project);
        var dir = Path.GetDirectoryName(filePath);
        if (dir is not null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(filePath, content, Encoding.UTF8);
    }

    /// <summary>
    /// Loads a <see cref="Project"/> from a JSON file.
    /// </summary>
    public static Project LoadJson(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Project JSON file not found.", filePath);
        var json = File.ReadAllText(filePath, Encoding.UTF8);
        return FromJson(json);
    }
}