namespace DevTeam.Core.Models;

/// <summary>
/// Represents the lifecycle state of a task within the multi-agent
/// orchestrator. Tasks transition through these states as they move
/// from planning through coding, testing, review, and completion.
/// </summary>
public enum TaskState
{
    /// <summary>Task has been defined but work has not started.</summary>
    NotStarted,

    /// <summary>A DevTeam has been created and work is beginning.</summary>
    Assigned,

    /// <summary>An agent (Coder) is actively working on the task.</summary>
    InProgress,

    /// <summary>The Tester agent is writing or running tests.</summary>
    Testing,

    /// <summary>The Reviewer agent is reviewing the code.</summary>
    Reviewing,

    /// <summary>Sent back for changes — feedback iteration in progress.</summary>
    Backlog,

    /// <summary>Code has been tested and reviewed successfully.</summary>
    Approved,

    /// <summary>Maximum iterations exceeded — task could not be completed.</summary>
    Failed,

    /// <summary>Waiting for clarification from the User before proceeding.</summary>
    Blocked
}