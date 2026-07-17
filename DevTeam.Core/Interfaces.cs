using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevTeam.Core.Events;

namespace DevTeam.Core.Interfaces;

public interface IEventBus
{
    void Subscribe(EventType eventType, string subscriberId, Func<Event, Task> handler);
    void Unsubscribe(EventType eventType, string subscriberId);
    Task PublishAsync(Event @event);
    T GetContext<T>(string key, T defaultValue = default!) ;
    void SetContext(string key, object value);
}

public interface IPersistence
{
    Task SaveEventAsync(Event @event);
    Task<List<Event>> LoadEventsAsync(long afterSequence = 0);
}

public interface IEventLogger
{
    Task LogEventAsync(Event @event);
}

public interface IPromptLogger
{
    Task LogPromptAsync(string agentName, string prompt, string response, Event triggerEvent);
}

public interface IStatusTracker
{
    Task UpdateTaskStatusAsync(string taskId, string state, string phaseId);
    Task<string> GetTaskStatusAsync(string taskId);
    Task UpdatePhaseStatusAsync(string phaseId, string status);
}

public interface ILlmClient
{
    Task<string> GenerateResponseAsync(string systemPrompt, string userPrompt);
}

public interface IRepoManager
{
    Task CreateBranchAsync(string branchName, string baseBranch);
    Task CommitFilesAsync(IEnumerable<(string path, string content)> files, string message);
    Task CreatePullRequestAsync(string head, string baseBranch, string title, string body);
}

public interface ITaskSpecService
{
    Task<Dictionary<string, object>> GetTaskSpecificationAsync(string taskId);
}
