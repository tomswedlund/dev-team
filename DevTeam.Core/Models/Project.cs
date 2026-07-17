using System;
using System.Collections.Generic;
using System.Linq;

namespace DevTeam.Core.Models;

/// <summary>
/// The root aggregate for everything the orchestrator manages. A
/// <see cref="Project"/> holds the requirements, phase plans (each with
/// their tasks), open questions, configuration, and the final review
/// report. It is the single object that gets serialised to disk so a
/// paused or crashed session can resume by loading it back.
/// <para>
/// Two serialisation formats are supported:
/// <list type="bullet">
///   <item><b>Markdown</b> — human-readable and reviewable in a PR.</item>
///   <item><b>JSON</b> — machine-readable for fast, lossless resume.</item>
/// </list>
/// </para>
/// </summary>
public class Project
{
    /// <summary>
    /// Name of the project.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Short description of the project's purpose.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The approved (or draft) requirements for the project.
    /// </summary>
    public ProjectRequirements Requirements { get; set; } = new();

    /// <summary>
    /// Project-level configuration (iteration limits, git settings, LLM settings).
    /// </summary>
    public ProjectConfig Config { get; set; } = new();

    /// <summary>
    /// All phase plans for the project, each carrying its tasks and
    /// runtime status.
    /// </summary>
    public List<PhasePlan> Phases { get; set; } = [];

    /// <summary>
    /// Open questions between the User and Planner that need resolution.
    /// </summary>
    public List<OpenQuestion> OpenQuestions { get; set; } = [];

    /// <summary>
    /// The final review report, produced by the Planner after all
    /// phases are complete. Null until the final review starts.
    /// </summary>
    public FinalReviewReport? FinalReview { get; set; }

    /// <summary>
    /// UTC timestamp of the last modification to the project state.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    // ── Convenience accessors ─────────────────────────────────────────

    /// <summary>
    /// Finds a phase by its identifier.
    /// </summary>
    /// <param name="phaseId">Phase identifier (e.g. "A").</param>
    /// <returns>The matching phase, or null if not found.</return>
    public PhasePlan? GetPhase(string phaseId) =>
        Phases.FirstOrDefault(p => p.PhaseId == phaseId);

    /// <summary>
    /// Finds a task by its identifier across all phases.
    /// </summary>
    /// <param name="taskId">Task identifier (e.g. "A-04").</param>
    /// <returns>The matching task, or null if not found.</returns>
    public TaskSpec? GetTask(string taskId) =>
        Phases.SelectMany(p => p.Tasks).FirstOrDefault(t => t.TaskId == taskId);

    /// <summary>
    /// Total number of phases in the project.
    /// </summary>
    public int TotalPhases => Phases.Count;

    /// <summary>
    /// Number of phases marked as complete.
    /// </summary>
    public int CompletedPhases => Phases.Count(p => p.Status == PhaseStatus.Complete);

    /// <summary>
    /// All tasks across all phases (flattened).
    /// </summary>
    public IEnumerable<TaskSpec> AllTasks => Phases.SelectMany(p => p.Tasks);

    /// <summary>
    /// Overall progress percentage (0–100), based on the ratio of
    /// completed phases to total phases.
    /// </summary>
    public double ProgressPercent =>
        Phases.Count == 0 ? 0 : Math.Round((double)CompletedPhases / Phases.Count * 100, 0);
}