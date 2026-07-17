using System.Threading.Tasks;

namespace DevTeam.Core.Interfaces;

/// <summary>
/// Abstracts LLM (Large Language Model) communication so that agents
/// can be tested with mock implementations and swapped between
/// providers (OpenAI, Anthropic, local models, etc.) without changing
/// agent code.
/// </summary>
public interface ILlmClient
{
    /// <summary>
    /// Sends a system prompt and user prompt to the LLM and returns
    /// the generated response text.
    /// </summary>
    /// <param name="systemPrompt">System-level instructions that define the agent's role and constraints.</param>
    /// <param name="userPrompt">The user-facing prompt containing the task or question.</param>
    /// <returns>The LLM's response text.</returns>
    Task<string> GenerateResponseAsync(string systemPrompt, string userPrompt);
}