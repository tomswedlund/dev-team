using System;
using System.Collections.Generic;

namespace DevTeam.Core.Models;

/// <summary>
/// Configuration for a project managed by the orchestrator. Loaded from
/// <c>config.yaml</c> at project startup and used to configure the event bus,
/// agent behavior, git settings, and iteration limits.
/// </summary>
public class ProjectConfig
{
    /// <summary>
    /// Name of the project.
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of review/feedback iterations per task before it is
    /// marked as FAILED.
    /// </summary>
    public int MaxIterationsPerTask { get; set; } = 3;

    /// <summary>
    /// Whether to automatically create PRs when a phase completes.
    /// If false, the GPO will notify the Planner but not create a PR.
    /// </summary>
    public bool AutoCreatePRs { get; set; } = true;

    /// <summary>
    /// The base branch to create phase branches from (e.g. "main", "master").
    /// </summary>
    public string BaseBranch { get; set; } = "main";

    /// <summary>
    /// LLM provider settings (model name, API key reference, temperature, etc.).
    /// Stored as a dictionary to remain provider-agnostic.
    /// </summary>
    public Dictionary<string, string> LlmSettings { get; set; } = [];

    /// <summary>
    /// Relative path to the config file (e.g. "config.yaml").
    /// </summary>
    public string FilePath { get; set; } = "config.yaml";
}