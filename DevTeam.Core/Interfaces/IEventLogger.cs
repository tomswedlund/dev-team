using System.Threading.Tasks;
using DevTeam.Core.Events;

namespace DevTeam.Core.Interfaces;

/// <summary>
/// Logs events to a human-readable format (e.g. a markdown table) for
/// auditing and debugging purposes. The log is append-only and provides
/// a chronological record of all events that have flowed through the
/// event bus.
/// </summary>
public interface IEventLogger
{
    /// <summary>
    /// Appends a single event to the event log in a readable format.
    /// </summary>
    /// <param name="event">The event to log.</param>
    /// <returns>A task that completes when the event has been written.</returns>
    Task LogEventAsync(Event @event);
}