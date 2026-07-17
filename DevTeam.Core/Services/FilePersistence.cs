using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DevTeam.Core.Events;
using DevTeam.Core.Interfaces;

namespace DevTeam.Core.Services;

/// <summary>
/// File-based implementation of <see cref="IPersistence"/> using a
/// Write-Ahead Log (WAL). Events are serialized as JSON Lines (one JSON
/// object per line) and appended to <c>event_log.wal</c>. On restart,
/// the WAL is read line-by-line to restore the full event stream.
/// </summary>
public class FilePersistence : IPersistence
{
    private readonly string _walFilePath;
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new <see cref="FilePersistence"/> instance writing to
    /// the specified directory.
    /// </summary>
    /// <param name="storageDirectory">Directory for the WAL file. Created if it does not exist.</param>
    public FilePersistence(string storageDirectory = "storage")
    {
        if (!Directory.Exists(storageDirectory))
        {
            Directory.CreateDirectory(storageDirectory);
        }
        _walFilePath = Path.Combine(storageDirectory, "event_log.wal");
    }

    /// <inheritdoc/>
    public async Task SaveEventAsync(Event @event)
    {
        // Use a JSON line format for the Write-Ahead Log (WAL)
        string json = JsonSerializer.Serialize(@event);
        string line = json + Environment.NewLine;

        lock (_lock)
        {
            // We use synchronous write for the WAL to ensure durability
            // In a high-performance system, we would use a BufferedStream or Channel
            File.AppendAllText(_walFilePath, line, Encoding.UTF8);
        }
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<List<Event>> LoadEventsAsync(long afterSequence = 0)
    {
        var events = new List<Event>();

        if (!File.Exists(_walFilePath))
        {
            return events;
        }

        lock (_lock)
        {
            using var reader = new StreamReader(_walFilePath, Encoding.UTF8);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var @event = JsonSerializer.Deserialize<Event>(line);
                if (@event != null && @event.SequenceNumber > afterSequence)
                {
                    events.Add(@event);
                }
            }
        }

        return await Task.FromResult(events);
    }
}