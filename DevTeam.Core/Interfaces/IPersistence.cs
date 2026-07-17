using System.Collections.Generic;
using System.Threading.Tasks;
using DevTeam.Core.Events;

namespace DevTeam.Core.Interfaces;

/// <summary>
/// Provides durable storage for events using a Write-Ahead Log (WAL).
/// Events are appended synchronously before being delivered, ensuring
/// they survive process crashes. On restart, all stored events can be
/// replayed to restore system state.
/// </summary>
public interface IPersistence
{
    /// <summary>
    /// Appends an event to the persistent WAL. The write must be durable
    /// (flushed to disk) before this method returns.
    /// </summary>
    /// <param name="event">The event to persist.</param>
    /// <returns>A task that completes when the event has been written.</returns>
    Task SaveEventAsync(Event @event);

    /// <summary>
    /// Loads all events from the WAL that have a sequence number greater
    /// than the specified value. Used for replaying events after a restart
    /// or for incremental reads.
    /// </summary>
    /// <param name="afterSequence">Only return events with a sequence number greater than this value. Defaults to 0 (all events).</param>
    /// <returns>A list of events ordered by sequence number.</returns>
    Task<List<Event>> LoadEventsAsync(long afterSequence = 0);
}