using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DevTeam.Core.Events;
using DevTeam.Core.Interfaces;

namespace DevTeam.Core.Services;

/// <summary>
/// File-based implementation of <see cref="IEventLogger"/> that appends
/// events as rows in a markdown table (<c>events.md</c>). Each row
/// shows the sequence number, timestamp, event type, sender, recipient,
/// and a summary of the data payload. The file is initialized with a
/// markdown header if it does not already exist.
/// </summary>
public class FileEventLogger : IEventLogger
{
    private readonly string _logFilePath;
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new <see cref="FileEventLogger"/> writing to the specified
    /// directory.
    /// </summary>
    /// <param name="storageDirectory">Directory for the log file. Created if it does not exist.</param>
    public FileEventLogger(string storageDirectory = "storage")
    {
        if (!Directory.Exists(storageDirectory))
        {
            Directory.CreateDirectory(storageDirectory);
        }
        _logFilePath = Path.Combine(storageDirectory, "events.md");
        InitializeLog();
    }

    private void InitializeLog()
    {
        if (!File.Exists(_logFilePath))
        {
            string header = "# Event Log\n\n| Sequence | Timestamp | Type | From | To | Data |\n|---|---|---|---|---|---|\n";
            File.WriteAllText(_logFilePath, header, Encoding.UTF8);
        }
    }

    /// <inheritdoc/>
    public async Task LogEventAsync(Event @event)
    {
        string dataSummary = @event.Data != null ? string.Join(", ", @event.Data.Keys) : "None";
        string row = $"| {@event.SequenceNumber} | {@event.Timestamp:yyyy-MM-dd HH:mm:ss} | {@event.Type} | {@event.From} | {@event.To} | {dataSummary} |\n";

        lock (_lock)
        {
            File.AppendAllText(_logFilePath, row, Encoding.UTF8);
        }
        await Task.CompletedTask;
    }
}