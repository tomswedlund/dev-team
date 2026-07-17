using System.Threading.Tasks;
using DevTeam.Core.Events;

namespace DevTeam.Core.Interfaces;

/// <summary>
/// Logs the full prompt sent to an LLM and the response received, along
/// with the triggering event. This creates an audit trail of every LLM
/// interaction for debugging, cost tracking, and reproducibility.
/// </summary>
public interface IPromptLogger
{
    /// <summary>
    /// Records a single LLM interaction — the system prompt, the generated
    /// prompt, the LLM response, and the event that triggered the call.
    /// </summary>
    /// <param name="agentName">Name of the agent that made the LLM call.</param>
    /// <param name="prompt">The full prompt text sent to the LLM.</param>
    /// <param name="response">The response text returned by the LLM.</param>
    /// <param name="triggerEvent">The event that triggered this LLM call.</param>
    /// <returns>A task that completes when the interaction has been logged.</returns>
    Task LogPromptAsync(string agentName, string prompt, string response, Event triggerEvent);
}