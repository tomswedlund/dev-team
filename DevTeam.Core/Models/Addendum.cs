using System;
using System.Collections.Generic;

namespace DevTeam.Core.Models;

/// <summary>
/// Represents a set of addendum tasks added to a phase after the User
/// requests changes to an already-completed or in-progress phase. Each
/// addendum tracks its reason, the tasks it adds, and its own lifecycle.
/// </summary>
public class Addendum
{
    /// <summary>
    /// Unique identifier for this addendum within its phase (e.g. "A", "B").
    /// </summary>
    public string AddendumId { get; set; } = string.Empty;

    /// <summary>
    /// The phase this addendum belongs to.
    /// </summary>
    public string PhaseId { get; set; } = string.Empty;

    /// <summary>
    /// The User's original request that triggered this addendum.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// The additional tasks created by the Planner in response to the request.
    /// </summary>
    public List<TaskSpec> Tasks { get; set; } = [];

    /// <summary>
    /// Current status of the addendum (e.g. "in_progress", "complete").
    /// </summary>
    public string Status { get; set; } = "not_started";

    /// <summary>
    /// Timestamp (UTC) when the addendum was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}