using System;
using System.Collections.Generic;
using DevTeam.Core.Events;

namespace DevTeam.Core.Models;

/// <summary>
/// Human-readable entry in the event audit log (<c>events.md</c>). Each entry
/// corresponds to a single <see cref="Event"/> that flowed through the event bus,
/// captured in markdown table format for user review.
/// </summary>
public class EventLogEntry
{
    /// <summary>
    /// The event's monotonically increasing sequence number.
    /// </summary>
    public long Sequence { get; set; }

    /// <summary>
    /// UTC timestamp when the event was published.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The type of event (see <see cref="EventType"/>).
    /// </summary>
    public EventType Type { get; set; }

    /// <summary>
    /// Identifier of the sender (e.g. "Planner", "Coder").
    /// </summary>
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the recipient, or "*" for broadcast.
    /// </summary>
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Keys present in the event's data payload, summarized for readability.
    /// </summary>
    public List<string> DataKeys { get; set; } = [];
}

/// <summary>
/// Human-readable entry in the prompt audit log (<c>prompts.md</c>). Each entry
/// captures the full LLM prompt and response for a single agent interaction.
/// </summary>
public class PromptLogEntry
{
    /// <summary>
    /// The type of event that triggered this LLM call.
    /// </summary>
    public EventType TriggerEvent { get; set; }

    /// <summary>
    /// The sequence number of the triggering event.
    /// </summary>
    public long EventSequence { get; set; }

    /// <summary>
    /// Name of the agent that made the LLM call (e.g. "Coder", "Tester").
    /// </summary>
    public string AgentName { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp (UTC) when the prompt was sent.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The full system + user prompt sent to the LLM.
    /// </summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// The full response received from the LLM.
    /// </summary>
    public string Response { get; set; } = string.Empty;
}