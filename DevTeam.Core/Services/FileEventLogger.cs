using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DevTeam.Core.Events;
using DevTeam.Core.Interfaces;

namespace DevTeam.Core.Services;

public class FileEventLogger : IEventLogger
{
    private readonly string _logFilePath;
    private readonly object _lock = new();

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
