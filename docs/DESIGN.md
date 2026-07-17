# Multi-Agent Development Orchestrator — Design Document

## Overview

A multi-agent system for collaborative software development with human-in-the-loop oversight. Agents are orchestrated through event-driven messaging to plan, implement, test, review, and integrate code into a Git repository. Every decision, message, and prompt is logged for auditability.

---

## 1. High-Level Architecture

```
┌──────────────────────────────────────────────────────────────────────┐
│                          USER                                         │
│                     (Human-in-the-Loop)                               │
└──────┬───────────────────────────┬────────────────────────┬──────────┘
       │                           │                        │
       ▼                           ▼                        ▼
┌──────────────────────────────────────────────────────────────────────┐
│                     GPO (coordinator)                                 │
│                                                                       │
│   event router  •  subscriber table  •  phase tracking                │
│   pure code — no LLM calls                                            │
└────┬───────────────────────┬────────────────────┬─────────────────────┘
     │                       │                    │
     ▼                       ▼                    ▼
┌────────────┐    ┌──────────────────┐   ┌──────────────────┐
│  PLANNER   │    │ DEV TEAM #1      │   │ DEV TEAM #2      │
│            │    │                  │   │                  │
│ • LLM      │    │  DT Orchestrator │   │  DT Orchestrator │
│ • LLM      │    │   (coordinator)  │   │   (coordinator)  │
│            │    │     ▲            │   │     ▲            │
│            │    │    /│\           │   │    /│\           │
└─────┬──────┘    │ Coder│Tester│Reviewer │ Coder│Tester│Reviewer
      │           └──┬───┬───┬────┘   └───┬───┬───┬────┘
      │              │ LLM │LLM │  LLM      │ LLM │LLM │  LLM
      │              └─────┴────┘           └─────┴────┘
      ▼                                            ▼
┌──────────────────────────────────────────────────────────────────────┐
│                       EVENT BUS                                      │
│              pub/sub + persistence (pure code)                        │
└──────────────────────────────────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────────────────────────┐
│                        GIT REPO                                      │
│        phases/PR-{N}/ → task/{name}/  +  project/docs/                │
└──────────────────────────────────────────────────────────────────────┘
```

**Key distinction:** Orchestrators (GPO + DevTeam) are coordinator code — event
routers and state machines with no LLM calls. The intelligence lives entirely in
the LLM-backed agents (Planner, Coder, Tester, Reviewer).

---

## 2. Agent Roles & System Prompts

> **Note on Orchestrators:** The Global Project Orchestrator and DevTeam Orchestrator
> are **not LLM agents**. They are lightweight deterministic coordinator modules
> (state machines + event routers). They handle message routing, task lifecycle,
> iteration tracking, and phase completion — all of which are rule-based and
> deterministic. No system prompts, no LLM calls. The intelligence lives entirely
> in the four LLM-backed agents: Planner, Coder, Tester, Reviewer.

### 2.1 Global Project Orchestrator (GPO) — Coordinator Module

**Role:** Event router and lifecycle manager. Receives events, looks up subscribers,
forwards messages, tracks phase completion. Pure code — no reasoning required.

**Implementation:** An event subscription table + forward method. See §3.

```python
class GlobalProjectOrchestrator:
    """
    Deterministic event router. No LLM calls.
    """
    def __init__(self):
        self.subscriptions: dict[str, list[str]] = {}  # event_type -> [subscriber_id]
        self.task_teams: dict[str, TaskOrchestrator] = {}
        self.phase_task_map: dict[str, list[str]] = {}  # phase_id -> [task_ids]

    def subscribe(self, event_type: str, subscriber_id: str):
        self.subscriptions.setdefault(event_type, []).append(subscriber_id)

    def publish(self, event: Event):
        for sub_id in self.subscriptions.get(event.type, []):
            self._deliver(event, sub_id)

    def create_task_team(self, task_id: str, task_spec: dict, max_iterations: int) -> TaskOrchestrator:
        team = TaskOrchestrator(task_id, task_spec, max_iterations)
        self.task_teams[task_id] = team
        return team

    def delete_task_team(self, task_id: str):
        del self.task_teams[task_id]
        self.phase_task_map.setdefault(...).remove(task_id)

    def on_task_complete(self, event: Event):
        """Handle task completion: update status, check phase completion."""
        task_id = event.taskId
        phase_id = self._phase_for_task(task_id)
        if all_tasks_done(phase_id):
            self.publish(Event("phase_complete", from="GPO", to="Planner"))

    def deliver(self, event: Event, subscriber_id: str):
        """Route event to the correct handler."""
        if subscriber_id == "Planner":
            Planner.handle(event)
        elif subscriber_id.startswith("DT-"):
            self.task_teams[subscriber_id].handle(event)
```

### 2.2 DevTeam Orchestrator — Coordinator Module

**Role:** Per-task state machine. Drives the deterministic flow
Coder → Tester → Reviewer, tracks feedback iterations, emits completion/failure events.
Pure code — no reasoning required.

**Implementation:** A state enum with transition methods. See §4.

```python
class TaskOrchestrator:
    """
    Deterministic state machine for a single task's dev team.
    No LLM calls. Tracks iteration count, manages flow control.
    """
    # States
    WAITING_FOR_PLAN = "waiting_for_plan"
    PLAN_APPROVED = "plan_approved"
    WAITING_FOR_CODE = "waiting_for_code"
    WAITING_FOR_TESTS = "waiting_for_tests"
    WAITING_FOR_REVIEW = "waiting_for_review"
    CODE_UPDATED = "code_updated"       # after feedback loop

    def __init__(self, task_id: str, task_spec: dict, max_iterations: int):
        self.task_id = task_id
        self.task_spec = task_spec
        self.max_iterations = max_iterations
        self.iteration = 0
        self.state = self.WAITING_FOR_PLAN
        self.context: dict = {}  # carries code, tests, feedback across transitions

    # ── Flow control (deterministic transitions) ─────────────────

    def on_plan_ready(self, plan: dict):
        self.context["impl_plan"] = plan
        self.state = self.PLAN_APPROVED
        self._prompt_coder("Proceed with implementation. Here is your approved plan.")

    def on_code_ready(self, code: dict):
        self.context["code"] = code
        self.state = self.WAITING_FOR_TESTS
        self._prompt_tester("Write tests for this code.")

    def on_tests_ready(self, tests: dict):
        self.context["tests"] = tests
        self.state = self.WAITING_FOR_REVIEW
        self._prompt_reviewer("Review the code, tests, and plan.")

    # ── Review decision handling ─────────────────────────────────

    def on_review_approved(self):
        """Reviewer says the task is good."""
        self.state = self.WAITING_FOR_PLAN  # reset for next task
        return Event("task_complete", from=self.task_id, to="GPO")

    def on_review_feedback(self, feedback: dict):
        """Reviewer requests changes."""
        self.iteration += 1
        if self.iteration >= self.max_iterations:
            self.state = self.WAITING_FOR_PLAN
            return Event("task_review_failure", from=self.task_id, to="GPO",
                         payload={"feedback": feedback, "iterations": self.iteration})
        # Route feedback to Coder, then Tester, then back to Reviewer
        self.context["feedback"] = feedback
        self.state = self.CODE_UPDATED
        self._prompt_coder(f"Apply this feedback:\n{feedback}")
        return None  # continue the loop

    # ── Test feedback (from reviewer) ────────────────────────────

    def on_test_feedback(self, feedback: dict):
        """Reviewer flagged test issues."""
        self.context["test_feedback"] = feedback
        self._prompt_tester(f"Reviewer says your tests need work:\n{feedback}")

    # ── Context passing ──────────────────────────────────────────

    def _prompt_coder(self, message: str):
        """Build and send a prompt to the Coder agent."""
        prompt = self._build_context_prompt(message)
        self._send_event(Event("agent_prompt", from=self.task_id, to="Coder",
                                payload={"prompt": prompt, "iteration": self.iteration}))

    def _prompt_tester(self, message: str):
        prompt = self._build_context_prompt(message)
        self._send_event(Event("agent_prompt", from=self.task_id, to="Tester",
                                payload={"prompt": prompt}))

    def _prompt_reviewer(self, message: str):
        prompt = self._build_context_prompt(message)
        self._send_event(Event("agent_prompt", from=self.task_id, to="Reviewer",
                                payload={"prompt": prompt}))

    def _build_context_prompt(self, instruction: str) -> str:
        """Assemble all prior context into a prompt for the agent."""
        parts = [
            f"Task: {self.task_spec['name']}",
            f"Spec: {self.task_spec['description']}",
            instruction,
        ]
        if "impl_plan" in self.context:
            parts.append(f"\nImplementation Plan:\n{self.context['impl_plan']}")
        if "code" in self.context:
            parts.append(f"\nCode:\n{self.context['code']}")
        if "tests" in self.context:
            parts.append(f"\nTests:\n{self.context['tests']}")
        if "feedback" in self.context:
            parts.append(f"\nReview Feedback:\n{self.context['feedback']}")
        if "iteration" in self.context:
            parts.append(f"\nIteration: {self.iteration}/{self.max_iterations}")
        return "\n".join(parts)
```

### 2.3 Project Planner Agent

**Role:** Works with the User to define requirements, decompose the project into phases and tasks, and performs a final project-level review. Owns the global status document.

**System Prompt:**
```
You are the Project Planner. Your responsibilities are:

1. REQUIREMENTS GATHERING: Work interactively with the User to produce a
   requirements document. Ask clarifying questions. Present the draft and
   request approval before proceeding.
2. PHASE/TASK DECOMPOSITION: Break approved requirements into phases. Each
   phase contains small, well-scoped, independently testable tasks. A task
   must be implementable in a single iteration cycle.
3. STATUS MANAGEMENT: Maintain and update the status document. Every task
   transitions through states: NotStarted → Assigned → InProgress →
   Testing → Reviewing → Approved or Backlog (on review failure).
4. USER COMMUNICATION: You speak DIRECTLY with the User. Present phase PRs
   for review. Ask clarifying questions. Negotiate changes.
5. ADDENDUM HANDLING: When the User requests changes to an already-assigned
   phase, create addendum tasks in the same phase and continue.
6. FINAL REVIEW: After all phases are complete, review all code against the
   original requirements. Check for style consistency, documentation, and
   test coverage. Produce a final project report.
7. IDENTITY: Refer to yourself only as "Planner".
```

### 2.4 Coder Agent

**Role:** Implements the task. Creates an implementation plan first. Can ask the
DevTeam Orchestrator clarifying questions (routed to Planner via GPO). Writes code
that passes tests.

**System Prompt:**
```
You are the Coder. Your responsibilities:

1. UNDERSTAND: Read the task specification thoroughly. If anything is unclear,
   ask the DevTeam Orchestrator (your sole point of contact). Do NOT contact
   the Planner or other teams directly.
2. IMPLEMENTATION PLAN: Before writing any code, produce an implementation
   plan that describes:
   - The modules/files you will create or modify
   - The public interfaces (functions, classes, APIs) with signatures
   - Dependencies on existing code
   - Assumptions and design decisions
   Send the plan to the DevTeam Orchestrator for review.
3. CODE: Once the plan is approved (or you proceed after stating it — the
   team will self-iterate), write production-quality code. Use best practices,
   consistent style, clear comments, and follow the project's conventions.
4. COMMUNICATION: Pass your final code and implementation plan to the
   DevTeam Orchestrator (who forwards to Tester, then Reviewer).
5. FEEDBACK: When review feedback arrives, address EVERY point. Update code,
   tests if needed, and explain what you changed. Resubmit to the
   DevTeam Orchestrator.
6. IDENTITY: Refer to yourself as "Coder" in all messages.
```

### 2.5 Tester Agent

**Role:** Writes and executes comprehensive unit tests covering every branch and
line of the Coder's implementation. Can ask the Coder clarifying questions (via
DevTeam Orchestrator). Applies test feedback from the Reviewer.

**System Prompt:**
```
You are the Tester. Your responsibilities:

1. TEST STRATEGY: For every line and branch of the Coder's implementation,
   write tests that exercise it at least once. Include:
   - Happy-path / success cases
   - Error / exception cases
   - Edge cases (empty inputs, boundary values, nil/None)
   - Integration if the task touches multiple components
2. TEST EXECUTION: Actually run the tests. Do not just write them — verify
   they all pass. Report pass/fail status with details on any failures.
3. CLARIFICATION: If the Coder's code is ambiguous or the spec is unclear
   about expected behavior, ask the DevTeam Orchestrator (who will ask the
   Coder).
4. FEEDBACK RESPONSE: When the Reviewer flags insufficient test coverage
   or wrong expectations, update the tests accordingly and re-run.
5. DELIVERABLES: Produce a test report listing:
   - Tests written (with file:line references)
   - Coverage summary
   - Any skipped tests and why
   - Errors or bugs discovered during testing
6. IDENTITY: Refer to yourself as "Tester" in all messages.
```

### 2.6 Reviewer Agent

**Role:** Final quality gate. Checks code for best practices, design patterns,
style consistency, test coverage, and documentation. Can approve or request
changes. Can ask the Coder clarifying questions (via DevTeam Orchestrator).

**System Prompt:**
```
You are the Reviewer. Your responsibilities:

1. CODE QUALITY: Review the implementation for:
   - Clean, readable, well-organized code
   - Appropriate use of design patterns
   - Error handling
   - Security considerations
   - Performance (no obvious O(n²) when O(n) is simple, etc.)
2. STYLE CONSISTENCY: Compare the new code against existing project files.
   Ensure naming conventions, formatting, comment style, and structure match.
3. TEST REVIEW: Verify the Tester's tests actually cover the implementation.
   Check for meaningful assertions (not just "it runs"). Flag tests that are
   too trivial or that don't test the right things.
4. DOCUMENTATION: Ensure code has sufficient inline comments for non-obvious
   logic, and that any public API has documentation.
5. DECISION: Either:
   a. APPROVE: Signal "task_complete" via the DevTeam Orchestrator.
   b. REQUEST CHANGES: Provide DETAILED feedback including:
      - Specific files, lines, or functions
      - What is wrong or could be improved
      - Suggested fixes or alternative approaches
      - Any test gaps the Tester should address
6. CLARIFICATION: If you are unsure about a design decision, ask the
   DevTeam Orchestrator (who will ask the Coder).
7. IDENTITY: Refer to yourself as "Reviewer" in all messages.
```

---

## 3. Event Bus — Subscriber Mechanism

### 3.1 Design Principles

- **Pub/Sub model**: Agents publish events; subscribers react to them.
- **No direct agent-to-agent communication**: All messages flow through the Event Bus.
- **Orchestrators are code, not agents**: They read events from the bus and take
  deterministic actions (call LLM, forward event, transition state). They do not
  generate LLM output themselves.
- **Structured messages**: Every message has `from`, `to`, `type`, `payload`, `timestamp`, `message_id`.
- **Delivery guarantee**: Messages are persisted in the message log (see §7).
- **Blocking vs. Async**: Internal dev-team flow is synchronous (orchestrator waits
  for LLM response, then transitions state). Cross-team and planner communication
  is async with polling or callbacks.
- **Single bus, single process**: The event bus runs in-process. No network
  protocol, no separate broker process. The bus is a shared object passed to all
  orchestrators and agent wrappers.

### 3.2 Event Schema

```typescript
// All events follow this schema
interface Event {
  id: string;                  // UUID v7 (time-ordered)
  from: string;               // sender identity (e.g. "DT-001", "Planner", "User")
  to: string;                 // recipient identity or "*" for broadcast
  type: EventType;
  payload: EventPayload;
  timestamp: string;          // ISO 8601 with nanosecond precision
  phase?: string;            // phase id this event belongs to
  taskId?: string;           // task id this event belongs to
  iteration?: number;        // feedback iteration count
  correlation_id?: string;   // groups events across a single conversation turn
  reply_to?: string;         // for out-of-band reply chains
}

type EventPayload = Record<string, unknown>;
```

**Identity conventions:**
- Coordinators: `GPO`, `DT-{task_id}` (e.g. `DT-T1.2`)
- LLM agents: `Planner`, `Coder`, `Tester`, `Reviewer`
- Human: `User`
- System: `EventBus` (internal routing events)

### 3.3 Event Types

```typescript
// All events follow this schema
interface Event {
  id: string;                  // UUID v7 (time-ordered)
  from: string;               // sender agent identity
  to: string;                 // recipient identity or "*" for broadcast
  type: EventType;
  payload: EventPayload;
  timestamp: string;          // ISO 8601
  phase?: string;            // phase id this event belongs to
  taskId?: string;           // task id this event belongs to
  iteration?: number;        // feedback iteration count
}
```

**Core Event Types:**

| Event Type | From | To | Description |
|---|---|---|---|
| `user_message` | User | Planner | User sends message to Planner (via GPO) |
| `planner_message` | Planner | User | Planner responds to User |
| `task_assigned` | Planner | GPO | Planner assigns a task to a DevTeam |
| `task_started` | GPO | Planner | GPO confirms DevTeam created |
| `impl_plan_ready` | Coder | DT Orchestrator | Coder submits implementation plan |
| `impl_plan_approved` | DT Orchestrator | Coder | Plan approved to proceed |
| `impl_plan_requested_changes` | DT Orchestrator | Coder | Plan needs revision |
| `code_ready` | Coder | DT Orchestrator | Code submitted for testing |
| `tests_ready` | Tester | DT Orchestrator | Tests written and passing |
| `review_submitted` | Reviewer | DT Orchestrator | Review decision + feedback |
| `review_approved` | Reviewer | DT Orchestrator | Task approved |
| `review_feedback` | Reviewer | DT Orchestrator | Detailed change requests |
| `test_feedback` | Reviewer | DT Orchestrator | Test-specific feedback |
| `clarification_request` | Agent | DT Orchestrator | Agent needs info from Planner |
| `clarification_response` | DT Orchestrator→GPO→Planner | Agent | Planner's answer |
| `task_complete` | DT Orchestrator | GPO | Task finished successfully |
| `task_review_failure` | DT Orchestrator | GPO | Max iterations exceeded |
| `phase_complete` | GPO | Planner | All tasks in phase done |
| `pr_ready` | GPO | Planner | PR is ready for User review |
| `user_pr_feedback` | User | Planner | User feedback on a phase PR |
| `addendum_tasks_created` | Planner | GPO | New tasks added to a phase |
| `project_complete` | Planner | User | All phases done, final report ready |

### 3.4 Event Bus — Architecture Overview

The event bus has three layers:

1. **Routing layer** — subscription table, publish/subscribe
2. **Persistence layer** — write-ahead log (WAL) for durability and replay
3. **Agent adapter layer** — bridges events to LLM agent wrappers and back

#### 3.4.1 Routing Layer — Interface

```python
class EventBus:
    """
    In-process pub/sub event bus.

    The bus is a shared object. Both coordinators and agent wrappers
    receive the same instance. Events are:
      1. Persisted to WAL (append-only log file)
      2. Dispatched to subscribers synchronously (one at a time)
      3. Logged to the project's events.md for audit
    """

    # -- Subscription --

    def subscribe(self, event_type: str, subscriber_id: str,
                  handler: Callable[[Event], None] = None): ...

    def unsubscribe(self, event_type: str, subscriber_id: str): ...

    def list_subscriptions(self) -> Dict[str, List[str]]: ...

    # -- Publish --

    def publish(self, event: Event) -> List[str]:
        """
        Publish an event. Steps:
          1. Tag event with correlation_id
          2. Persist to WAL
          3. Determine delivery mode (direct / broadcast / blocking)
          4. Dispatch to all matching subscribers
          5. Log to events.md
        Returns list of subscriber IDs that received the event.
        """
        ...

    def publish_blocking(self, event: Event) -> Event:
        """
        Publish and BLOCK until a reply arrives (reply_to = event.id).
        Used for Planner <-> User clarifying questions.
        Returns the reply Event, or a timeout marker.
        """
        ...

    # -- Context --

    def set_context(self, key: str, value: Any): ...
    def get_context(self, key: str, default=None) -> Any: ...

    # -- Lifecycle --

    def shutdown(self): ...
    def is_running(self) -> bool: ...
```

**Dispatch algorithm:** For each published event, the bus:
1. Looks up all subscribers for the event type
2. If `event.to == "*"`, delivers to all subscribers (broadcast)
3. If `event.to == "<id>"`, delivers only to the named subscriber
4. Dispatches synchronously — one subscriber at a time
5. If a subscriber's handler raises, logs the error and continues to the next subscriber
6. If no subscriber exists, logs a warning and persists the event anyway

**Delivery modes:**

| Mode | When | How |
|------|------|-----|
| Fire-and-forget | Most events | `publish(event)` returns when all subscribers handled it |
| Blocking | Planner <-> User questions | `publish_blocking(event)` blocks until reply arrives |
| Broadcast | System-wide notifications | `to: "*"` delivers to all subscribers of that type |

**Error handling:** If a subscriber raises an exception, the bus:
1. Logs the error (with stack trace)
2. Queues the event for retry (in `_blocked` map)
3. Continues dispatching to remaining subscribers
4. Never crashes the bus

#### 3.4.2 Persistence — Interface

Events are persisted in two formats:

1. **WAL (`events.jsonl`)**: Machine-readable append-only log for durability and replay.
2. **Audit log (`events.md`)**: Human-readable Markdown for user review.

```python
class EventBus:
    """Persistence interface."""

    def _persist_event(self, event: Event):
        """Append event to JSONL write-ahead log. One JSON object per line."""
        ...

    def replay(self, after_seq: int = 0) -> List[Event]:
        """
        Replay events from WAL after a given sequence number.
        Used to reconstruct in-memory state after a crash.
        """
        ...

    def _audit_log(self, event: Event, delivered_to: List[str]):
        """Append a human-readable entry to events.md."""
        ...
```

**WAL format (events.jsonl):**

```
{"seq": 1, "event": {"id": "uuid-1", "from": "Planner", "to": "GPO", "type": "task_assigned", "payload": {...}, "timestamp": "2025-01-15T10:30:00Z", ...}}
{"seq": 2, "event": {"id": "uuid-2", "from": "GPO", "to": "DT-T1.1", "type": "task_assigned", "payload": {...}, ...}}
{"seq": 3, "event": {"id": "uuid-3", "from": "DT-T1.1", "to": "GPO", "type": "task_complete", "payload": {...}, ...}}
```

**Audit format (events.md):**

```markdown
## Event #1
- **Time**: 2025-01-15T10:30:00Z
- **From**: Planner
- **To**: GPO
- **Type**: task_assigned
- **Delivered to**: GPO
- **Payload**: {"task_id": "T1.1", "spec": "..."}
```

**Recovery procedure:** If the process crashes:
1. Load `config.yaml` and `status.md` to reconstruct state
2. Call `bus.replay(last_seen_seq)` to get all events
3. Replay each event through `bus._dispatch()` to rebuild in-memory state
4. Resume normal operation

#### 3.4.3 Agent Adapter — Interface

The bus bridges to LLM agents through an **AgentAdapter** — one per agent.
The adapter wraps events in LLM prompts, calls the LLM API, parses responses,
and publishes results back as Events.

```python
class AgentAdapter:
    """
    Adapts an LLM-backed agent to the event bus interface.

    The bus calls adapter.receive(event), which:
      1. Wraps the event in an LLM prompt (with system prompt + context)
      2. Calls the LLM API
      3. Parses the response into an Event
      4. Publishes the result as a new event via the bus
    """

    def __init__(self, agent_name: str, system_prompt: str,
                 bus: EventBus, llm_client: LLMClient): ...

    def receive(self, event: Event):
        """Entry point. Wraps event in prompt, calls LLM, publishes result."""
        ...

    def _build_prompt(self, event: Event) -> str:
        """Convert event into an LLM prompt with system prompt + context."""
        ...

    def _parse_response(self, llm_response: str, incoming_event: Event) -> Optional[Event]:
        """
        Parse the LLM response into an Event.

        The LLM can produce:
        - An event type directive -> publish as that event type
        - A clarification question -> publish as clarification_request
        - No directive -> treat as internal note, do not publish
        """
        ...

    def _log_prompt(self, event: Event, prompt: str): ...
    def _log_response(self, event: Event, response: str): ...
    def reset_history(self): ...
```

**LLM response protocol:** Each LLM agent is instructed to begin its response
with an `EVENT_TYPE:` directive so the adapter can determine what event to
publish. Examples:

```
# Coder completes implementation:
EVENT_TYPE: code_ready
## Response:
I have implemented the event bus with the following files...

# Reviewer requests changes:
EVENT_TYPE: review_feedback
## Response:
The code has several issues that need addressing...

# Coder needs clarification:
EVENT_TYPE: clarification_request
## Response:
I need to know: should the event bus support priority queues?
```

If the agent's response does not start with `EVENT_TYPE:`, it is treated as
an internal note (e.g., reasoning or clarification to itself) and not published.

**Prompt format** (what the adapter sends to the LLM):

```
SYSTEM: You are the Coder. ... [system prompt]

USER:
## Event: agent_prompt
## From: DT-T1.1
## To: Coder
## Task: T1.1 - Event bus implementation
## Iteration: 0/3

Your task: Event bus implementation.
Write production code.

## End Event
```

#### 3.4.4 Coordinator Registration — Interface

Coordinators (GPO and DevTeam Orchestrator) register with the bus directly.
They receive raw `Event` objects and dispatch to handlers by event type.

```python
class GlobalProjectOrchestrator:
    """
    Coordinator that registers with the event bus.
    Subscribes to task/phase lifecycle events.
    """
    def __init__(self, bus: EventBus, project_dir: str):
        bus.set_context("GPO", self)
        bus.subscribe("task_complete", "GPO", self.on_task_complete)
        bus.subscribe("task_review_failure", "GPO", self.on_task_review_failure)
        bus.subscribe("phase_complete", "GPO", self.on_phase_complete)
        bus.subscribe("addendum_tasks_created", "GPO", self.on_addendum_created)

    def receive(self, event: Event): ...

    # Event handlers (called by bus dispatcher):
    def on_task_complete(self, event: Event): ...
    def on_task_review_failure(self, event: Event): ...
    def on_phase_complete(self, event: Event): ...
    def on_addendum_created(self, event: Event): ...

    def create_task_team(self, task_id: str, task_spec: dict,
                         max_iterations: int, phase_id: str) -> TaskOrchestrator: ...
```

```python
class TaskOrchestrator:
    """
    Coordinator that runs a single task's dev team.
    Maintains per-task state: iteration count, context dict, state machine.
    """
    def __init__(self, task_id: str, task_spec: dict, max_iterations: int,
                 bus: EventBus, project_dir: str):
        bus.set_context(f"DT-{task_id}", self)
        # States: WAITING_FOR_PLAN, WAITING_FOR_CODE, WAITING_FOR_TESTS,
        #         WAITING_FOR_REVIEW, COMPLETE, FAILED, CODE_UPDATED

    def receive(self, event: Event): ...

    # State transitions:
    def on_task_assigned(self, event: Event): ...
    def on_impl_plan_ready(self, event: Event): ...
    def on_code_ready(self, event: Event): ...
    def on_tests_ready(self, event: Event): ...
    def on_review_approved(self, event: Event): ...
    def on_review_feedback(self, event: Event): ...
    def on_test_feedback(self, event: Event): ...

    def _make_prompt(self, recipient: str, message: str) -> Event:
        """Build a prompt event with full task context for an LLM agent."""
        ...
```

**Registration flow:** When GPO creates a DevTeam:
1. GPO instantiates `TaskOrchestrator(task_id, task_spec, max_iterations, bus, project_dir)`
2. TaskOrchestrator calls `bus.set_context(f"DT-{task_id}", self)`
3. GPO calls `bus.subscribe("task_assigned", f"DT-{task_id}", team.receive)`
4. GPO publishes `task_started` to notify Planner
5. GPO publishes `task_assigned` to the new DevTeam — which triggers the first state transition
### 3.5 End-to-End Delivery Flow

Here's a complete message flow for a single task iteration:

```
┌─────────────────────────────────────────────────────────────────────┐
│                       EVENT BUS (in-process)                        │
│                                                                     │
│  Subscriptions:                                                     │
│    task_complete            → ["GPO"]                               │
│    task_review_failure      → ["GPO"]                               │
│    phase_complete           → ["Planner"]                           │
│    task_assigned            → ["DT-T1.1"]                           │
│    impl_plan_ready          → ["DT-T1.1"]                           │
│    code_ready               → ["DT-T1.1"]                           │
│    tests_ready              → ["DT-T1.1"]                           │
│    review_approved          → ["DT-T1.1"]                           │
│    review_feedback          → ["DT-T1.1"]                           │
│    agent_prompt             → ["Coder", "Tester", "Reviewer"]       │
│    user_message             → ["Planner"]                           │
│    planner_message          → ["User"]                              │
│                                                                     │
│  Context:                                                            │
│    GPO           → GlobalProjectOrchestrator instance                │
│    DT-T1.1       → TaskOrchestrator instance                         │
│    Planner       → PlannerAgentAdapter instance                      │
│    Coder         → CoderAgentAdapter instance                        │
│    Tester        → TesterAgentAdapter instance                       │
│    Reviewer      → ReviewerAgentAdapter instance                     │
│    User          → HumanUserAdapter instance                         │
└─────────────────────────────────────────────────────────────────────┘

Step-by-step for Task T1.1:

1. Planner.publish(Event("task_assigned", from="Planner", to="GPO", ...))
   │
   ├─► BUS: persist to WAL
   ├─► BUS: dispatch to ["GPO"]
   │
   ▼
2. GPO.on_task_assigned(event)
   │
   ├─► Creates TaskOrchestrator("T1.1", ...)
   ├─► Registers DT-T1.1 with bus
   ├─► publish(Event("task_started", from="GPO", to="Planner"))
   └─► Returns
   │
   ├─► BUS: dispatches task_started → Planner
   │    (Planner updates status.md: T1.1 → ASSIGNED)
   │
   ▼
3. DT-T1.1 receives "task_assigned" (via GPO's publish inside on_task_assigned)
   │
   ├─► DT-T1.1._make_prompt("Coder", "Your task...")
   ├─► BUS: publish(agent_prompt, from="DT-T1.1", to="Coder")
   │
   ├─► BUS: dispatch to CoderAgentAdapter
   │
   ▼
4. CoderAgentAdapter.receive(event)
   │
   ├─► Wraps event in LLM prompt
   ├─► Logs prompt to prompts.md
   ├─► Calls LLM API
   ├─► Parses response → Event("impl_plan_ready", from="Coder", to="DT-T1.1")
   ├─► Logs response to prompts.md
   ├─► BUS: publish(impl_plan_ready, from="Coder", to="DT-T1.1")
   │
   ├─► BUS: dispatch to ["DT-T1.1"]
   │
   ▼
5. DT-T1.1.on_impl_plan_ready(event)
   │
   ├─► Saves plan to context
   ├─► BUS: publish(agent_prompt, from="DT-T1.1", to="Coder")
   │    ("Proceed with implementation. Plan approved.")
   │
   ├─► BUS: dispatch to CoderAgentAdapter
   │
   ▼
6. CoderAgentAdapter receives second prompt
   │
   ├─► LLM produces code
   ├─► Writes code files to disk (via repo_manager)
   ├─► BUS: publish(code_ready, from="Coder", to="DT-T1.1")
   │
   ▼
7. DT-T1.1.on_code_ready(event)
   │
   ├─► BUS: publish(agent_prompt, to="Tester")
   │
   ▼
8. TesterAgentAdapter receives → writes tests → runs them
   │
   ├─► BUS: publish(tests_ready, from="Tester", to="DT-T1.1")
   │
   ▼
9. DT-T1.1.on_tests_ready(event)
   │
   ├─► BUS: publish(agent_prompt, to="Reviewer")
   │
   ▼
10. ReviewerAgentAdapter receives → reviews
    │
    ├─► APPROVED: BUS: publish(review_approved, from="Reviewer", to="DT-T1.1")
    │    → DT-T1.1.on_review_approved()
    │    → BUS: publish(task_complete, from="DT-T1.1", to="GPO")
    │    → GPO.on_task_complete() → updates status → checks phase → publishes phase_complete
    │
    └─► FEEDBACK: BUS: publish(review_feedback, from="Reviewer", to="DT-T1.1")
         → DT-T1.1.on_review_feedback() → iteration++ → prompt Coder → cycle
```

### 3.6 Topic Filtering and Delivery Modes

#### 3.6.1 Direct Delivery (most common)

Events are published with `to: "<specific_id>"`. The bus only delivers to
subscribers whose id matches the recipient. This is the default and most
efficient mode.

```python
# Planner assigns a task only to GPO
bus.publish(Event.create("task_assigned", from="Planner", to="GPO", ...))
```

#### 3.6.2 Broadcast Delivery

Events with `to: "*"` are delivered to ALL subscribers of that event type.
Used for system-wide notifications.

```python
# Notify everyone that a new phase started
bus.publish(Event.create("phase_started", from="Planner", to="*", ...))
```

#### 3.6.3 Blocking Delivery

For interactions that require a synchronous response (Planner ↔ User), the
publisher blocks until the response arrives.

```python
# Planner asks User a question
event = Event.create("clarification_question", from="Planner", to="User",
                     payload={"question": "Should we use HTTP or TCP?"})
bus.publish_blocking(event)  # blocks until User replies

# User's reply comes back as a separate event
# Reply arrives with reply_to = original_event.id
```

#### 3.6.4 Subscription Filtering

Subscriptions can include a predicate for fine-grained filtering:

```python
# Only deliver task_complete events for tasks in a specific phase
bus.subscribe("task_complete", "GPO",
              predicate=lambda e: e.phase == active_phase_id)
```

The predicate receives the full Event and returns True if the subscriber
wants to receive this specific instance.

### 3.7 LLM Prompt Wrapping

Every event destined for an LLM agent is wrapped in a structured prompt before
being sent to the LLM API. The AgentAdapter handles this wrapping.

```
Raw Event:
  type: "agent_prompt"
  from: "DT-T1.1"
  to: "Coder"
  payload: {
    "message": "Your task: Event bus implementation.\n\nWrite production code."
  }

Wrapped LLM Prompt:
  ┌─────────────────────────────────────────────────────────┐
  │ SYSTEM: You are the Coder. ... [system prompt]         │
  │                                                         │
  │ USER:                                                   │
  │ ## Event: agent_prompt                                  │
  │ ## From: DT-T1.1                                        │
  │ ## To: Coder                                            │
  │ ## Task: T1.1 - Event bus implementation                │
  │ ## Iteration: 0/3                                       │
  │                                                         │
  │ Your task: Event bus implementation.                    │
  │ Write production code.                                  │
  │                                                         │
  │ ## End Event                                            │
  └─────────────────────────────────────────────────────────┘

LLM Response (structured text):
  EVENT_TYPE: code_ready
  TASK_ID: T1.1
  ## Response:
  I have implemented the event bus with the following files:
  1. event_bus.py - Main EventBus class with subscribe/publish methods
  2. event.py - Event dataclass definition
  ...
```

The response parser looks for the `EVENT_TYPE:` directive at the start of the
response to determine what event to publish back to the bus. If no directive is
found, the response is treated as an internal note and not published.

### 3.8 Subscription Model (Updated)

```
Component             | Subscribes To                              | Type
----------------------|--------------------------------------------|---------
GPO (coordinator)     | task_complete, task_review_failure         | code
Planner (LLM agent)   | phase_complete, pr_ready, project_complete | agent
DT Orchestrator (code)| task_assigned (self-transition)            | code
Coder (LLM agent)     | impl_plan_ready (self), code_ready         | agent
Tester (LLM agent)    | tests_ready (self), test_feedback          | agent
Reviewer (LLM agent)  | review_approved (self), review_feedback    | agent
User                  | planner_message, pr_ready, project_complete| human
EventBus (system)     | agent_prompt (to route to correct adapter) | system
```

**Note on self-transitions:** When the DT Orchestrator publishes an event to
"Coder", the bus routes it to the CoderAgentAdapter, which handles the LLM call
and publishes the result. The Coder does NOT subscribe to its own events —
the adapter sits between the bus and the LLM.

**Data flow:**

```
Agent LLM response
       │
       ▼
┌─────────────────────┐     event      ┌─────────────────────┐
│  AgentAdapter       │───────────────►│   EventBus           │
│  (wraps LLM agent)  │                │  (routing + WAL)     │
│                     │◄───────────────│                      │
└─────────────────────┘     event      └──────────┬──────────┘
       ▲                                          │
       │   LLM prompt                              ▼
       │                                  ┌─────────────────────┐
       │                                  │  Coordinator (code)  │
       │                                  │  (GPO / DT Orch)    │
       │                                  │                     │
       │                                  └──────────┬──────────┘
       │                                             │
       ▼                                             ▼
┌─────────────────────┐                      ┌─────────────────────┐
│   LLM API           │                      │   AgentAdapter      │
│  (external service) │◄─────────────────────│   (for other agents)│
└─────────────────────┘   LLM prompt/resp    │                     │
                                             └─────────────────────┘
```

### 3.9 Security & Isolation

- **No code execution in agents**: Agents produce text responses only. All file
  operations (writing code, running tests) are performed by the orchestrators
  using a `RepoManager` and `TestRunner` — never directly by the LLM.
- **No network from agents**: Agents have no network access. All I/O goes through
  the orchestrators' tools.
- **Single bus, one process**: No IPC, no network sockets. The bus is a Python
  object shared via reference. Attack surface is minimal.
- **Immutable events**: Events are dataclasses (effectively immutable after
  construction). No subscriber can modify another's events.

---

## 4. Task Team Lifecycle

The DevTeam Orchestrator is a **deterministic state machine**. It transitions between
states based on events, without any LLM reasoning.

### 4.1 State Machine

```
                         ┌──────────────────────┐
                         │                      │
                         ▼                      │
  ┌──────────┐   task_assigned   ┌──────────┐   │   max_iters
  │ NOT_READY │─────────────────►│ WAITING  │   │   exceeded?
  │          │                   │ _FOR_PLAN│   │        │
  └──────────┘                   └────┬─────┘   │        │
                                      │         │        │
                                      │ plan    │        │
                                      │ ready   │        │
                                      ▼         │        │
                                 ┌──────────┐   │        │
                                 │ PLAN     │   │        │
                                 │ APPROVED │   │        │
                                 └────┬─────┘   │        │
                                      │         │        │
                                      │ code    │        │
                                      │ ready   │        │
                                      ▼         │        │
                                 ┌──────────┐   │        │
                                 │ WAITING  │   │        │
                                 │ _FOR_TEST│   │        │
                                 └────┬─────┘   │        │
                                      │         │        │
                                      │ tests   │        │
                                      │ ready   │        │
                                      ▼         │        │
                                 ┌──────────┐   │        │
                                 │ WAITING  │   │        │
                                 │ _FOR_REV │◄──┘        │
                                 └────┬─────┘            │
                                      │                 │
                          ┌───────────┼────────────┐    │
                          │           │            │    │
                     review     review         test     │
                     approved   feedback      feedback  │
                          │           │            │    │
                          ▼           ▼            ▼    │
                   ┌──────────┐ ┌──────────┐ ┌────────┐  │
                   │ COMPLETE │ │ REVISION │ │ RETEST │──┘
                   │          │ │ _LOOP    │ │ _LOOP  │
                   └──────────┘ └──────────┘ └────────┘
```

### 4.2 Transition Logic (Pseudocode)

```
on_event(event):
  switch event.type:
    case "impl_plan_ready":
      context.impl_plan = event.payload
      state = PLAN_APPROVED
      # Next: wait for code_ready

    case "code_ready":
      context.code = event.payload
      state = WAITING_FOR_TESTS
      # Emit prompt to Tester

    case "tests_ready":
      context.tests = event.payload
      state = WAITING_FOR_REVIEW
      # Emit prompt to Reviewer

    case "review_approved":
      state = COMPLETE
      # Emit "task_complete" event to GPO

    case "review_feedback":
      iteration += 1
      context.feedback = event.payload
      if iteration >= max_iterations:
        state = FAILED
        # Emit "task_review_failure" to GPO
      else:
        state = REVISION_LOOP
        # Emit prompt to Coder with feedback

    case "test_feedback":
      context.test_feedback = event.payload
      state = RETEST_LOOP
      # Emit prompt to Tester

    case "coder_response_after_feedback":
      state = WAITING_FOR_TESTS
      # Emit prompt to Tester (re-test the updated code)

    case "tester_response_after_feedback":
      state = WAITING_FOR_REVIEW
      # Emit prompt to Reviewer (re-review)
```

### 4.3 Context Flow

The Orchestrator maintains a `context` dictionary that accumulates across transitions:

```python
context = {
    "task_spec": {...},         # from Planner
    "impl_plan": "...",         # from Coder (set once plan approved)
    "code": "...",              # from Coder
    "tests": "...",             # from Tester
    "feedback": "...",          # from Reviewer (set on review request)
    "test_feedback": "...",     # from Reviewer (set on test feedback)
    "iteration": 2,             # incremented each revision cycle
}
```

On each LLM prompt, the Orchestrator builds a message containing the full context
so the agent has complete history. No context is lost between turns.

---

## 5. Status Tracking

### 5.1 Status Document (`project/status.md`)

```markdown
# Project Status

Last updated: 2025-01-15T10:30:00Z

## Requirements
- Status: APPROVED
- Document: requirements/REQUIREMENTS.md
- Approved by: User (2025-01-15)

## Phases

### Phase 1: Core Infrastructure
- PR: #1 (merge request pending / merged / closed)
- Status: IN_PROGRESS
- Tasks:

| Task | Name | Status | Iteration | Max Iter | Assigned DevTeam | Notes |
|------|------|--------|-----------|----------|-------------------|-------|
| T1.1 | Event bus implementation | APPROVED | 1 | 3 | DT-001 | |
| T1.2 | Agent registry | IN_PROGRESS | 2 | 3 | DT-002 | Review feedback applied |
| T1.3 | Message serializer | TESTING | 1 | 3 | DT-003 | |

## Addenda

### Phase 1, Addendum A (User requested: "Add retry logic")
- Status: IN_PROGRESS
- Tasks:

| Task | Name | Status | Iteration | Notes |
|------|------|--------|-----------|-------|
| T1.A1 | Retry on connection failure | NOT_STARTED | 0 | |
| T1.A2 | Exponential backoff | NOT_STARTED | 0 | |

## Open Questions (User ↔ Planner)

| # | From | Question | Status | Answer |
|---|------|----------|--------|--------|
| Q1 | User | "Should we use TCP or HTTP?" | ANSWERED | HTTP (REST) |
| Q2 | Planner | "Should tests run in parallel?" | PENDING | |

## Project Summary
- Total Phases: 1
- Completed: 0
- Overall Progress: 33%
- Final Review Status: NOT_STARTED
```

### 5.2 Status States

```
NOT_STARTED
ASSIGNED          (DevTeam created, work beginning)
IN_PROGRESS       (Agent is working on it)
TESTING           (Tests being written/executed)
REVIEWING         (Under review)
BACKLOG           (Sent back for changes — feedback iteration)
APPROVED          (Tested and reviewed successfully)
FAILED            (Max iterations exceeded)
BLOCKED           (Waiting for clarification)
```

### 5.3 Update Rules

- Every significant state transition writes an entry to `project/events.md` (see §7).
- The status document is the SOURCE OF TRUTH for a session. A new session loads it to resume.
- The Planner updates status after every event that crosses a team boundary.
- The GPO updates status when a task completes (transitioning from `IN_PROGRESS`/
  `BACKLOG` to `APPROVED` or `FAILED`).

---

## 6. Git & PR Workflow

### 6.1 Repository Structure

```
repo/
├── docs/
│   └── projects/
│       └── <project-name>/
│           ├── status.md              # Status document (§5.1)
│           ├── events.md              # Event log (§7)
│           ├── prompts.md             # Full prompt log (§7)
│           ├── architecture.md        # Architecture decisions
│           ├── config.yaml            # Project configuration
│           ├── requirements/
│           │   └── REQUIREMENTS.md
│           ├── phases/
│           │   ├── PHASE-001.md       # Phase plan with task list
│           │   └── PHASE-002.md
│           └── reports/
│               ├── FINAL-REVIEW.md
│               └── PHASE-REVIEW-001.md
├── src/                       # Main source code (evolves per phase)
├── tests/                     # Test files
└── docs/
```

> **Separation of concerns:** Source code lives under `src/` and `tests/` at the
> repo root. All project planning, status, event logs, and prompts live under
> `docs/projects/<project-name>/`. This keeps development artifacts (code, tests)
> separate from project management artifacts (plans, logs, reports).

### 6.2 Phase → PR Mapping

```
Phase 1 → git branch: phase/001-core-infra
          PR: #1
          Contains: all task commits for T1.1, T1.2, T1.3

Phase 2 → git branch: phase/002-feature-x
          PR: #2
          Base: phase/001-core-infra (or main if sequential)
```

Each task's code changes are committed to the phase branch. The final commit of
the last task in a phase triggers the `pr_ready` event.

### 6.3 PR Review by User

1. GPO sends `pr_ready` to Planner.
2. Planner presents PR to User (summary of what was built, test results, any
   feedback iterations).
3. User can:
   - **MERGE**: Phase complete. Planner creates next phase plan (or signals
     project_complete).
   - **REQUEST CHANGES**: User provides feedback → Planner creates addendum tasks
     → new DevTeams.
   - **ASK CLARIFYING QUESTION**: Planner answers, then User decides.

---

## 7. Message Logging & Audit Trail

### 7.1 Event Log (`docs/projects/<project-name>/events.md`)

Every event is appended:

```markdown
## Event #42
- **Time**: 2025-01-15T10:30:00Z
- **From**: Coder
- **To**: DT Orchestrator (task T1.2)
- **Type**: code_ready
- **Payload**: Implementation complete. 3 files modified, 1 file created.
  Code submitted for testing.
```

### 7.2 Prompt Log (`docs/projects/<project-name>/prompts.md`)

Every LLM prompt sent to any agent, and the agent's response, are logged:

```markdown
## Prompt #42 — Coder (task T1.2)
**Sent**: 2025-01-15T10:30:00Z
**Prompt**:
```
## Event: task_assigned
## From: DT Orchestrator
## To: Coder

Task: Implement event bus with subscriber pattern...
[full prompt]
```

**Response**:
```
## Implementation Plan

1. Create EventBus class...
[full response]
```
```

### 7.3 Queryable Format

Both logs are Markdown with consistent headings so they can be grepped or parsed
into structured data for later review.

---

## 8. User-in-the-Loop Interaction Points

| Stage | Who | What Happens |
|-------|-----|--------------|
| Requirements approval | User ↔ Planner | Planner presents requirements draft; User approves, edits, or rejects |
| Phase PR review | User ↔ Planner | Planner presents PR with summary; User merges or requests changes |
| Addendum negotiation | User ↔ Planner | User requests changes to completed phase; Planner proposes tasks; User approves |
| Clarifying questions | User ↔ Planner | Planner asks User for decisions; User answers |
| Final report | Planner → User | Planner presents completion report; User reviews |
| Escalation | DT Orchestrator → GPO → Planner → User | If a clarification requires User input, Planner asks User |

---

## 9. Configuration

All tunable parameters in a single config file:

```yaml
# docs/projects/<project-name>/config.yaml
max_iterations_per_task: 3        # Max review-feedback cycles per task
max_iterations_per_project: 2     # Max rounds of final-project changes
clarification_timeout_hours: 24   # How long to wait for User/Planner answer
max_open_questions: 10            # Warn if too many questions pile up
reviewer_strictness: "balanced"   # "lenient", "balanced", "strict"
test_coverage_target: 0.90        # Minimum line coverage percentage
git_auto_commit: true             # Auto-commit after each approved task
pr_auto_create: true              # Auto-create PR when phase completes
log_all_llm_calls: true           # Log every prompt + response
```

---

## 10. Project File Structure

```
multi-agent-orchestrator/
├── design/
│   └── DESIGN.md                  # This document
├── src/
│   ├── orchestrator/
│   │   ├── global_orchestrator.py  # GPO: event router + subscriber table
│   │   ├── task_orchestrator.py    # DT Orchestrator: state machine
│   │   └── message_bus.py          # Event bus pub/sub implementation
│   ├── agents/
│   │   ├── planner.py             # Planner LLM agent wrapper
│   │   ├── coder.py               # Coder LLM agent wrapper
│   │   ├── tester.py              # Tester LLM agent wrapper
│   │   └── reviewer.py            # Reviewer LLM agent wrapper
│   ├── llm/
│   │   ├── client.py              # LLM API client
│   │   ├── prompt_builder.py      # Constructs prompts from events
│   │   └── system_prompts.py      # System prompts for LLM agents only
│   ├── git/
│   │   └── repo_manager.py        # Branch, commit, PR management
│   ├── status/
│   │   └── tracker.py             # Status document reader/writer
│   └── logging/
│       └── event_logger.py        # Event and prompt logger
├── templates/
│   ├── task_spec.yaml             # Template for task specifications
│   ├── phase_plan.yaml            # Template for phase plans
│   └── status_template.md         # Starting status document
└── projects/                      # (per-project instance)
    └── <project-name>/
        ├── config.yaml
        ├── docs/
        │   ├── status.md
        │   ├── events.md
        │   ├── prompts.md
        │   ├── requirements/
        │   ├── phases/
        │   └── reports/
        └── src/                   # Source code for this project
        └── tests/                 # Tests for this project
```

> **Note:** Each project managed by the orchestrator gets its own directory under
> `projects/<project-name>/`. Source code and tests for that project live inside
> it. The orchestrator's own source (coordinators, agents, LLM client) lives under
> `src/` at the repo root and is reused across projects.

---

## 11. Implementation Plan

### Phase A — Foundation (Infrastructure)
- [ ] Implement event bus (pub/sub with persistence)
- [ ] Implement message format + prompt builder
- [ ] Implement event logger + prompt logger
- [ ] Implement status tracker (read/write status.md)
- [ ] Write GPO coordinator module (event router + subscriber table)
- [ ] Write DevTeam Orchestrator state machine
- [ ] Define system prompts for LLM agents

### Phase B — Agents
- [ ] Implement Planner agent wrapper (requirements gathering, decomposition, status updates)
- [ ] Implement Coder agent wrapper
- [ ] Implement Tester agent wrapper
- [ ] Implement Reviewer agent wrapper
- [ ] Wire agents to their orchestrators via event bus

### Phase C — Git Integration
- [ ] Implement repo manager (branch creation, commits, PR creation)
- [ ] Map task completion → commit; phase completion → PR
- [ ] Implement PR-ready notification flow

### Phase D — User Interaction
- [ ] Implement User ↔ Planner message relay (via GPO)
- [ ] Implement clarification request flow
- [ ] Implement PR presentation to User
- [ ] Implement addendum task creation from User feedback

### Phase E — Final Review & Polish
- [ ] Implement final project review (Planner checks all code)
- [ ] Implement configuration system
- [ ] End-to-end integration testing
- [ ] Documentation

---

## 12. Coding Conventions & Dependency Injection

The orchestrator's source code (`src/`) must be testable without spinning up an LLM,
launching a Git repo, or writing to disk. Every external dependency — persistence,
messaging, LLM calls, Git operations, status tracking — is abstracted behind an
interface and injected at construction time. This allows unit tests to swap real
implementations for in-memory mocks.

### 12.1 Service Interface Layer

Each service in the system is defined by a Python protocol (or abstract base class)
that describes only the surface that callers need. Implementations are free to add
extra methods, but callers must never depend on implementation-specific APIs.

| Interface | Purpose | Injected Into |
|-----------|---------|---------------|
| `EventBus` | Publish/subscribe messaging | All coordinators, all agent adapters |
| `ILlmClient` | LLM API calls (completion, chat) | Agent adapters |
| `IPersistence` | WAL + prompt log + event log | Event bus, agent adapters |
| `IRepoManager` | Git branches, commits, PRs | GPO, DT orchestrator |
| `IStatusTracker` | Read/write `status.md` | Planner agent, GPO |
| `IEventLogger` | Append to `events.md` / `prompts.md` | Agent adapter, GPO |
| `ITaskSpecService` | Load/save task specs + phase plans | Planner agent, DT orchestrator |

### 12.2 Constructor Injection Pattern

Services are wired at the application's top level (usually a `create_app()`
function or a module-level factory). Each component declares its dependencies in
its `__init__()` and stores them as private instance attributes. No component
constructs its own dependencies — that keeps every layer independently testable.

```python
class TaskOrchestrator:
    def __init__(
        self,
        task_id: str,
        task_spec: dict,
        max_iterations: int,
        bus: EventBus,
        project_dir: Path,
    ):
        self.task_id = task_id
        self.task_spec = task_spec
        self.max_iterations = max_iterations
        self.bus = bus                    # injected
        self.project_dir = project_dir
        self.state = TaskState.WAITING_FOR_PLAN
        self.iteration = 0
        self.context: dict = {}

    def receive(self, event: Event): ...
    # ... transition methods
```

```python
class AgentAdapter:
    def __init__(
        self,
        agent_name: str,
        system_prompt: str,
        bus: EventBus,
        llm_client: ILlmClient,
        logger: IEventLogger,
        status_tracker: IStatusTracker,
    ):
        self.agent_name = agent_name
        self.system_prompt = system_prompt
        self.bus = bus
        self.llm_client = llm_client      # injected
        self.logger = logger
        self.status_tracker = status_tracker

    def receive(self, event: Event): ...
```

```python
class GlobalProjectOrchestrator:
    def __init__(
        self,
        bus: EventBus,
        project_dir: Path,
        repo_manager: IRepoManager,
        status_tracker: IStatusTracker,
    ):
        self.bus = bus
        self.project_dir = project_dir
        self.repo_manager = repo_manager  # injected
        self.status_tracker = status_tracker
```

**Why constructor injection?**

1. **Testability** — tests pass mocks into the constructor; no need for monkey-patching.
2. **Single responsibility** — each class only knows about its collaborators, not
   about how to create them.
3. **Explicit dependencies** — reading `__init__` shows every external service this
   component depends on, making the wiring table easy to audit.

### 12.3 Wiring the Application Graph

At runtime, the application creates real implementations once and wires them
top-down:

```python
def create_app(project_dir: Path) -> GlobalProjectOrchestrator:
    # 1. Create shared infrastructure services
    bus = PersistedEventBus(
        persistence=FilePersistence(project_dir / "wal.jsonl"),
        prompt_logger=FilePromptLogger(project_dir / "prompts.md"),
        event_logger=FileEventLogger(project_dir / "events.md"),
        status_tracker=MarkdownStatusTracker(project_dir / "status.md"),
    )
    llm = OpenAIClient()           # real LLM client
    repo = GitRepoManager(project_dir)

    # 2. Build the GPO with all injected services
    gpo = GlobalProjectOrchestrator(
        bus=bus,
        project_dir=project_dir,
        repo_manager=repo,
        status_tracker=bus.status_tracker,   # bus re-exposes its tracker
    )

    # 3. Agent adapters get the same bus and their own LLM client
    planner_adapter = AgentAdapter(
        agent_name="Planner",
        system_prompt=SYSTEM_PROMPTS["PLANNER"],
        bus=bus,
        llm_client=llm,
        logger=bus.event_logger,
        status_tracker=bus.status_tracker,
    )
    # ... Coder, Tester, Reviewer adapters follow the same pattern

    # 4. Start the event loop
    bus.start()

    return gpo
```

### 12.4 Unit Testing with Mocks

Tests construct the component under test with mock collaborators. No network, no
filesystem, no LLM — just pure Python assertions.

```python
from unittest.mock import MagicMock, patch
from myapp.testing import InMemoryEventBus, InMemoryPersistence

def test_planner_submits_phase_plan():
    # 1. Create mock services
    bus = InMemoryEventBus()
    persistence = MagicMock()
    llm_client = MagicMock()
    status_tracker = MagicMock()
    logger = MagicMock()

    # 2. Configure the LLM mock to return a realistic response
    llm_client.complete.return_value = """Phase 1: Core Infrastructure
- Task T1.1: Event bus implementation
- Task T1.2: Message format + prompt builder
..."""

    # 3. Create the agent adapter with injected mocks
    planner = AgentAdapter(
        agent_name="Planner",
        system_prompt=SYSTEM_PROMPTS["PLANNER"],
        bus=bus,
        llm_client=llm_client,
        logger=logger,
        status_tracker=status_tracker,
    )

    # 4. Simulate an event
    event = Event(
        type="project_initialized",
        source="user",
        target="Planner",
        payload={"project_name": "my-app"},
    )

    # 5. Deliver the event
    planner.receive(event)

    # 6. Assert expected behavior
    assert llm_client.complete.call_count == 1
    call_args = llm_client.complete.call_args[0][0]
    assert "project_name" in call_args

    # 7. Verify the planner published the phase_plan event
    published = [e for e in bus.published_events if e.type == "phase_plan"]
    assert len(published) == 1
    assert "T1.1" in published[0].payload["description"]
```

### 12.5 In-Memory Test Helpers

Provide lightweight in-memory replacements of critical interfaces for testing:

```python
class InMemoryEventBus(EventBus):
    """Event bus that routes messages in-process, no WAL, no threads."""

    def __init__(self):
        self.subscriptions: dict[str, list[tuple[str, Callable]]] = {}
        self.published_events: list[Event] = []

    def subscribe(self, event_type, subscriber_id, handler=None):
        self.subscriptions.setdefault(event_type, []).append((subscriber_id, handler))

    def publish(self, event):
        self.published_events.append(event)
        for sub_id, handler in self.subscriptions.get(event.type, []):
            if sub_id == "*":
                handler(event)
            elif sub_id == event.target:
                handler(event)

    def start(self): ...
    def shutdown(self): ...


class InMemoryPersistence(Persistence):
    """WAL and prompt/event logs held entirely in memory."""

    def __init__(self):
        self.wal_events: list[Event] = []
        self.prompt_log: list[dict] = []
        self.event_log: list[dict] = []

    def save_event(self, event: Event):
        self.wal_events.append(event)

    def load_replay(self) -> list[Event]:
        return list(self.wal_events)

    def log_prompt(self, entry: dict):
        self.prompt_log.append(entry)

    def log_event(self, entry: dict):
        self.event_log.append(entry)
```

### 12.6 Interface Contract Checklist

Before declaring a new service interface, verify it passes all of the following:

- [ ] **Interface-only**: Defined as `Protocol` (from `typing`) or abstract base class.
      No concrete methods that require a specific implementation.
- [ ] **Testable in isolation**: A mock can satisfy every method without external
      dependencies (no file I/O, no network, no process spawning).
- [ ] **Method signature stability**: Adding new parameters requires a default value
      (e.g., `default=None`) so existing callers are not broken.
- [ ] **Return types are consistent**: Methods return concrete dataclasses or `None`,
      never raw dicts or tuples that change shape between implementations.
- [ ] **Errors are typed**: Each method documents its exception types.
  Implementations raise those types (or subclasses), not `Exception`.

### 12.7 Diagram: Dependency Injection Topology

```
┌─────────────────────────────────────────────────────────────┐
│                     create_app()                             │
│                 (top-level wiring)                           │
└─────┬──────────────┬────────────────┬───────────────────────┘
      ▼              ▼                ▼
┌─────────────┐ ┌──────────┐  ┌──────────────┐
│InMemory     │ │ InMemory │  │ Mock / Real  │
│EventBus     │ │Persist.  │  │ LLM / Git    │
└─────┬───────┘ └────┬─────┘  └──────┬───────┘
      │              │               │
      ▼              ▼               ▼
┌─────────────────────────────────────────────────────────────┐
│              GPO ──────────► DT Orchestrator                 │
│                │           (per-task instance)               │
│                │           ┌────────────────────────┐       │
│                │           │ AgentAdapters          │       │
│                │           │  - PlannerAdapter      │       │
│                │           │  - CoderAdapter        │       │
│                │           │  - TesterAdapter       │       │
│                │           │  - ReviewerAdapter     │       │
│                │           └────────────────────────┘       │
│                │              ▲    ▲    ▲    ▲              │
│                │              │    │    │    │              │
│                ▼              ▼    ▼    ▼    ▼              │
│          EventBus (injected into all adapters)              │
│          Persistence (injected into bus)                     │
│          StatusTracker (injected into planner + GPO)         │
│          EventLogger (injected into adapters + GPO)          │
└─────────────────────────────────────────────────────────────┘

In production: real implementations (PersistedEventBus, GitRepoManager, etc.)
In tests: in-memory or mock implementations (InMemoryEventBus, MagicMock)
```

### 12.8 Pseudocode: Testable DT Orchestrator Test

```python
def test_dt_orchestrator_iterates_on_review_feedback():
    bus = InMemoryEventBus()
    planner = build_mock_adapter("Planner", bus)
    coder = build_mock_adapter("Coder", bus)
    tester = build_mock_adapter("Tester", bus)
    reviewer = build_mock_adapter("Reviewer", bus)

    task_spec = {
        "task_id": "T1.1",
        "description": "Implement event bus",
        "max_iterations": 3,
        "phase_id": "phase-1",
    }
    dt = TaskOrchestrator(
        task_id="T1.1",
        task_spec=task_spec,
        max_iterations=3,
        bus=bus,
        project_dir=Path("tmp"),
    )

    # Register handlers on the bus
    bus.subscribe("task_assigned", "DT-T1.1", dt.receive)
    bus.subscribe("impl_plan_ready", "DT-T1.1", dt.receive)
    bus.subscribe("code_ready", "DT-T1.1", dt.receive)
    bus.subscribe("tests_ready", "DT-T1.1", dt.receive)
    bus.subscribe("review_approved", "DT-T1.1", dt.receive)
    bus.subscribe("review_feedback", "DT-T1.1", dt.receive)
    bus.subscribe("test_feedback", "DT-T1.1", dt.receive)

    # Simulate: planner submits plan, code is written, review fails,
    # feedback is given, code is updated, review passes
    bus.publish(Event("impl_plan_ready", "Planner", "DT-T1.1", {"plan": "..."}))
    assert dt.state == TaskState.WAITING_FOR_CODE

    bus.publish(Event("code_ready", "Coder", "DT-T1.1", {"code": "..."}))
    assert dt.state == TaskState.WAITING_FOR_TESTS

    bus.publish(Event("tests_ready", "Tester", "DT-T1.1", {"tests": "..."}))
    assert dt.state == TaskState.WAITING_FOR_REVIEW

    # First review: feedback (revision 1)
    bus.publish(Event("review_feedback", "Reviewer", "DT-T1.1", {"feedback": "Fix..."}))
    assert dt.state == TaskState.CODE_UPDATED
    assert dt.iteration == 1

    # Code updated, tests re-run, review passes
    bus.publish(Event("code_ready", "Coder", "DT-T1.1", {"code": "..."}))
    bus.publish(Event("tests_ready", "Tester", "DT-T1.1", {"tests": "..."}))
    bus.publish(Event("review_approved", "Reviewer", "DT-T1.1", {}))

    assert dt.state == TaskState.DONE
    assert len([e for e in bus.published_events
                if e.type == "task_complete"]) == 1
```

---
