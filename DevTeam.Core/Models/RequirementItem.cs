namespace DevTeam.Core.Models;

/// <summary>
/// Represents a single requirement within a project requirements document.
/// Requirements are gathered interactively by the Planner agent from the User
/// and can be functional, non-functional, or constraints.
/// </summary>
public class RequirementItem
{
    /// <summary>
    /// Unique identifier for this requirement (e.g. "R1", "R2").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Category of the requirement.
    /// </summary>
    public RequirementCategory Category { get; set; } = RequirementCategory.Functional;

    /// <summary>
    /// Human-readable description of what the system must do or satisfy.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Priority level indicating how essential this requirement is.
    /// </summary>
    public RequirementPriority Priority { get; set; } = RequirementPriority.Must;
}

/// <summary>
/// Categorizes a requirement by type.
/// </summary>
public enum RequirementCategory
{
    /// <summary>The system must perform this function or behavior.</summary>
    Functional,

    /// <summary>The system must meet this quality attribute (performance, security, etc.).</summary>
    NonFunctional,

    /// <summary>The system must operate within this constraint (platform, budget, timeline, etc.).</summary>
    Constraint
}

/// <summary>
/// Indicates how essential a requirement is, following the MoSCoW method.
/// </summary>
public enum RequirementPriority
{
    /// <summary>Critical — must be delivered for the system to be acceptable.</summary>
    Must,

    /// <summary>Important — should be delivered if possible.</summary>
    Should,

    /// <summary>Desirable — could be delivered if time and resources allow.</summary>
    Could
}