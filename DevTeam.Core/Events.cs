namespace DevTeam.Core.Events;

public enum EventType
{
    // Planner Events
    PhasePlanReady,
    TaskRequested,
    
    // DevTeam Orchestrator Events
    TaskAssigned,
    ImplPlanReady,
    CodeReady,
    TestsReady,
    ReviewApproved,
    ReviewFeedback,
    TestFeedback,
    
    // Global Events
    TaskComplete,
    TaskReviewFailure,
    PhaseComplete,
    AddendumCreated,
    
    // System Events
    SystemError,
    UserInterventionRequired
}

public record Event(
    EventType Type,
    string From,
    string To,
    Dictionary<string, object> Data,
    DateTime Timestamp,
    long SequenceNumber = 0
)
{
    public Event(EventType type, string from, string to, Dictionary<string, object> data) 
        : this(type, from, to, data, DateTime.UtcNow) {}
}
