namespace DevTeam.Core.Models;

/// <summary>
/// Represents the status of a single phase in the project lifecycle.
/// </summary>
public class PhaseStatusEntry
{
    /// <summary>
    /// Unique identifier of the phase (e.g. "A", "B", "C").
    /// </summary>
    public string PhaseId { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the phase (e.g. "pending", "in_progress", "complete").
    /// </summary>
    public string Status { get; set; } = string.Empty;
}