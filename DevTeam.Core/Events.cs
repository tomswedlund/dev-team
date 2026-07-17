using System;
using System.Collections.Generic;

namespace DevTeam.Core.Events;

/// <summary>
/// Identifies the type of event flowing through the event bus. Each
/// event type corresponds to a specific message in the multi-agent
/// communication protocol between the Planner, DevTeam Orchestrator,
/// agents (Coder, Tester, Reviewer), and the Global Project Orchestrator.
/// </summary>
public enum EventType
{
    // Planner Events
    PhasePlanReady,
    TaskRequested,

    // DevTeam Orchestrator Events
    TaskAssigned,
    ImplPlanReady,
    CodeReady,
    TestsReady,
    ReviewApproved,
    ReviewFeedback,
    TestFeedback,

    // Global Events
    TaskComplete,
    TaskReviewFailure,
    PhaseComplete,
    AddendumCreated,

    // System Events
    SystemError,
    UserInterventionRequired
}

/// <summary>
/// The immutable message envelope that flows through the event bus.
/// Every interaction between agents, orchestrators, and services is
/// represented as an <see cref="Event"/>. Events carry a type, sender,
/// recipient, a data payload, a timestamp, and a monotonically
/// increasing sequence number for ordering and replay.
/// </summary>
/// <param name="Type">The type of event (see <see cref="EventType"/>).</param>
/// <param name="From">Identifier of the sender (e.g. "Planner", "Coder").</param>
/// <param name="To">Identifier of the recipient, or "*" for broadcast.</param>
/// <param name="Data">Payload carrying event-specific data (e.g. task ID, code, feedback).</param>
/// <param name="Timestamp">UTC timestamp when the event was created.</param>
/// <param name="SequenceNumber">Monotonically increasing sequence number assigned by the bus.</param>
public record Event(
    EventType Type,
    string From,
    string To,
    Dictionary<string, object> Data,
    DateTime Timestamp,
    long SequenceNumber = 0
)
{
    /// <summary>
    /// Creates a new event with the current UTC timestamp and a
    /// sequence number of 0 (assigned by the bus on publish).
    /// </summary>
    /// <param name="type">The type of event.</param>
    /// <param name="from">Identifier of the sender.</param>
    /// <param name="to">Identifier of the recipient, or "*" for broadcast.</param>
    /// <param name="data">Event-specific payload data.</param>
    public Event(EventType type, string from, string to, Dictionary<string, object> data)
        : this(type, from, to, data, DateTime.UtcNow) {}
}