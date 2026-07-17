namespace DevTeam.Core.Models;

/// <summary>
/// Represents an open question between the User and Planner that needs
/// resolution before progress can continue. Questions can originate from
/// either party and carry a status and optional answer.
/// </summary>
public class OpenQuestion
{
    /// <summary>
    /// Unique identifier for this question (e.g. "Q1", "Q2").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Who asked the question ("User" or "Planner").
    /// </summary>
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// The question text.
    /// </summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// Whether the question has been answered or is still pending.
    /// </summary>
    public QuestionStatus Status { get; set; } = QuestionStatus.Pending;

    /// <summary>
    /// The answer text, if the question has been resolved.
    /// </summary>
    public string? Answer { get; set; }
}

/// <summary>
/// Indicates whether an open question has been resolved.
/// </summary>
public enum QuestionStatus
{
    /// <summary>Question has been asked but not yet answered.</summary>
    Pending,

    /// <summary>Question has been answered and is no longer blocking.</summary>
    Answered
}