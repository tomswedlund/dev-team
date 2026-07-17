using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DevTeam.Core.Events;
using DevTeam.Core.Interfaces;

namespace DevTeam.Core.Services;

/// <summary>
/// File-based implementation of <see cref="IPromptLogger"/> that appends
/// LLM prompt/response pairs to <c>prompts.md</c>. Each entry includes
/// the triggering event, agent name, timestamp, the full prompt sent to
/// the LLM, and the full response received. The file is initialized with
/// a markdown header if it does not already exist.
/// </summary>
public class FilePromptLogger : IPromptLogger
{
    private readonly string _logFilePath;
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new <see cref="FilePromptLogger"/> writing to the specified
    /// directory.
    /// </summary>
    /// <param name="storageDirectory">Directory for the log file. Created if it does not exist.</param>
    public FilePromptLogger(string storageDirectory = "storage")
    {
        if (!Directory.Exists(storageDirectory))
        {
            Directory.CreateDirectory(storageDirectory);
        }
        _logFilePath = Path.Combine(storageDirectory, "prompts.md");
        InitializeLog();
    }

    private void InitializeLog()
    {
        if (!File.Exists(_logFilePath))
        {
            string header = "# Prompt Log\n\n";
            File.WriteAllText(_logFilePath, header, Encoding.UTF8);
        }
    }

    /// <inheritdoc/>
    public async Task LogPromptAsync(string agentName, string prompt, string response, Event triggerEvent)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"## Event: {triggerEvent.Type} (Seq: {triggerEvent.SequenceNumber})");
        sb.AppendLine($"**Agent:** {agentName}");
        sb.AppendLine($"**Timestamp:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("\n### Prompt");
        sb.AppendLine("```text");
        sb.AppendLine(prompt);
        sb.AppendLine("```");
        sb.AppendLine("\n### Response");
        sb.AppendLine("```text");
        sb.AppendLine(response);
        sb.AppendLine("```");
        sb.AppendLine("\n---\n");

        lock (_lock)
        {
            File.AppendAllText(_logFilePath, sb.ToString(), Encoding.UTF8);
        }
        await Task.CompletedTask;
    }
}