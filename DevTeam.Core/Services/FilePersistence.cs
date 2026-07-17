using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DevTeam.Core.Events;
using DevTeam.Core.Interfaces;

namespace DevTeam.Core.Services;

public class FilePersistence : IPersistence
{
    private readonly string _walFilePath;
    private readonly object _lock = new();

    public FilePersistence(string storageDirectory = "storage")
    {
        if (!Directory.Exists(storageDirectory))
        {
            Directory.CreateDirectory(storageDirectory);
        }
        _walFilePath = Path.Combine(storageDirectory, "event_log.wal");
    }

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
