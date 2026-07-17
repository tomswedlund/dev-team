using System;
using System.Threading.Tasks;
using DevTeam.Core.Events;

namespace DevTeam.Core.Interfaces;

/// <summary>
/// In-memory event bus that provides pub/sub messaging between agents,
/// orchestrators, and services. Supports topic filtering by event type,
/// direct (point-to-point) and broadcast delivery, and a shared context
/// dictionary for cross-component state.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Registers a handler that will be invoked whenever an event of the
    /// specified type is published to the bus.
    /// </summary>
    /// <param name="eventType">The event type to listen for.</param>
    /// <param name="subscriberId">Unique identifier of the subscriber (e.g. agent name).</param>
    /// <param name="handler">Async callback invoked with the published event.</param>
    void Subscribe(EventType eventType, string subscriberId, Func<Event, Task> handler);

    /// <summary>
    /// Removes a previously registered handler for the given event type
    /// and subscriber.
    /// </summary>
    /// <param name="eventType">The event type to stop listening to.</param>
    /// <param name="subscriberId">Unique identifier of the subscriber to remove.</param>
    void Unsubscribe(EventType eventType, string subscriberId);

    /// <summary>
    /// Publishes an event to all subscribed handlers. For direct events
    /// (where <see cref="Event.To"/> is set), only the matching subscriber
    /// receives the event. For broadcast events, all subscribers of that
    /// event type receive it.
    /// </summary>
    /// <param name="event">The event to publish.</param>
    /// <returns>A task that completes when all handlers have finished.</returns>
    Task PublishAsync(Event @event);

    /// <summary>
    /// Retrieves a shared context value by key. Context is used to pass
    /// state between components without explicit event data (e.g. current
    /// phase ID, configuration values).
    /// </summary>
    /// <typeparam name="T">The expected type of the context value.</typeparam>
    /// <param name="key">The context key to look up.</param>
    /// <param name="defaultValue">Value to return if the key is not found.</param>
    /// <returns>The context value, or <paramref name="defaultValue"/> if not set.</returns>
    T GetContext<T>(string key, T defaultValue = default!);

    /// <summary>
    /// Sets a shared context value that can be retrieved by any component
    /// with access to the same bus instance.
    /// </summary>
    /// <param name="key">The context key.</param>
    /// <param name="value">The value to store.</param>
    void SetContext(string key, object value);
}