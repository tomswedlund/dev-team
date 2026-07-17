# Implementation Task Plan

Detailed breakdown of all tasks needed to build the multi-agent orchestrator.
Each task has an estimated effort, dependencies, and a clear completion criteria.

> **Effort estimates** assume a single experienced Python developer using an
> LLM-assisted coding workflow. Adjust for your own speed.

---

## Phase A — Project Setup & Core Services

**Goal:** Scaffold the project, define the event schema and service interfaces,
and build the standalone persistence, logging, and status services. These are
the building blocks the EventBus will wire together.

**Estimate:** 2–3 days

| # | Task | Est. | Dependencies | Deliverable |
|---|------|------|--------------|-------------|
| A-01 | Set up Python project: virtualenv, `pyproject.toml`, `src/`, `tests/`, CI skeleton | 0.5 d | — | Project compiles, tests run, CI passes on green |
| A-02 | Define `Event` dataclass + `EventType` enum (all event types from §3.3) | 0.5 d | A-01 | Schema file importable, type-checked |
| A-03 | Define all service interfaces (§12.1): `EventBus`, `IPersistence`, `IEventLogger`, `IStatusTracker`, `ILlmClient`, `IRepoManager`, `ITaskSpecService` | 0.5 d | A-02 | Protocol/ABC files, all methods documented with signatures |
| A-04 | Implement `FilePersistence` (WAL append, load/replay) | 1 d | A-03 | Events persist across process restart; replay produces identical event stream |
| A-05 | Implement `FileEventLogger` (append to `events.md` in markdown table) | 0.5 d | A-03 | Events appended to `events.md`; format matches §7.1 |
| A-06 | Implement `FilePromptLogger` (append to `prompts.md`) | 0.5 d | A-03 | Prompts appended to `prompts.md`; format matches §7.2 |
| A-07 | Implement `MarkdownStatusTracker` (read/write `status.md`) | 0.5 d | A-03 | Can read current phase/task statuses; can update a task's `State` field |
| A-08 | End-to-end smoke test: publish an event, verify it's persisted, replayed, logged, and visible in status | 0.5 d | A-04, A-05, A-07 | Single integration test covering the full chain |

**Phase A Gates:**
- [ ] Project compiles and CI is green
- [ ] `Event` schema + all interfaces are defined with type signatures
- [ ] Persistence, logging, and status tracker work as standalone units

---

## Phase B — EventBus

**Goal:** Build the event-driven communication backbone — the in-memory bus,
WAL-backed persistence, the agent adapter, and the coordinator registration
system. Nothing else in the system can work without this phase.

**Estimate:** 3–4 days

| # | Task | Est. | Dependencies | Deliverable |
|---|------|------|--------------|-------------|
| B-01 | Implement `EventBus` in-memory (subscribe, publish, direct + broadcast delivery, topic filtering) | 1.5 d | A-03 | In-memory bus routes events correctly; 4+ unit tests pass |
| B-02 | Implement `PersistedEventBus` — wraps `EventBus` + `IPersistence` for WAL (crash recovery, replay) | 1 d | B-01, A-04 | Bus replays all events on startup; crash-safe write-ahead log |
| B-03 | Implement `AgentAdapter` (receive → prompt build → LLM call → response parse → publish → log) | 1.5 d | A-03, A-05 | Adapter routes event → LLM → response; response events published to bus |
| B-04 | Implement GPO + DT Orchestrator coordinator registration on the bus (subscriber table, routing) | 0.5 d | B-01 | Coordinators can register/deregister; events routed to correct handler |
| B-05 | Implement `InMemoryEventBus` + `InMemoryPersistence` test helpers (§12.5) | 0.5 d | B-01, B-02 | Helpers importable from `myapp.testing`; used in all Phase B tests |
| B-06 | End-to-end EventBus smoke test: publish → persist → replay → agent receives → agent publishes response → response persisted | 0.5 d | B-01, B-02, B-03, B-04 | Single test covering full message lifecycle with no real LLM |

**Phase B Gates:**
- [ ] `EventBus` is the single source of truth for all inter-component communication
- [ ] WAL-backed bus survives process restart (replay restores exact state)
- [ ] Agent adapter can receive an event, call a mock LLM, publish the response, and log the prompt — with zero real LLM involved
- [ ] Coordinators (GPO, DT Orchestrator) register on the bus and receive their events

---

## Phase C — Agent Infrastructure

**Goal:** Build the agent adapter, state machine, and system prompts. Agents
don't yet do anything useful — they just receive events, build prompts, call the
LLM, and publish responses.

**Estimate:** 3–4 days

| # | Task | Est. | Dependencies | Deliverable |
|---|------|------|--------------|-------------|
| C-01 | Implement LLM response parser (§3.4.3): extract `EVENT_TYPE:`, body, and metadata | 0.5 d | A-02 | Parser handles valid/invalid responses; 3 tests pass |
| C-02 | Write system prompt templates for all 4 agents (§2.3–§2.6) | 0.5 d | A-03 | Prompt files in `src/llm/system_prompts.py` |
| C-03 | Define `TaskState` enum and `TaskOrchestrator` state machine (§4.1) | 0.5 d | A-03 | State enum importable; transition rules documented |
| C-04 | Implement `TaskOrchestrator` with all 6 transition methods | 1.5 d | C-03, B-01 | State changes correctly on each event type; iteration counting works |
| C-05 | Write unit tests for `TaskOrchestrator` — happy path (plan → code → test → review → done) | 1 d | C-04 | 5+ state transition tests; all pass |
| C-06 | Write unit tests for `TaskOrchestrator` — failure paths (review feedback, test feedback, max iterations) | 1 d | C-05 | 3+ failure path tests; all pass |
| C-07 | Implement `GlobalProjectOrchestrator` bare skeleton (§2.1) | 0.5 d | B-01 | GPO can subscribe to events; stub methods exist |

**Phase C Gates:**
- [ ] `TaskOrchestrator` passes all unit tests (happy + failure paths)
- [ ] `AgentAdapter` can receive an event, call a mock LLM, publish the response,
      and log the prompt — with no real LLM involved
- [ ] System prompts are present for all 4 agent roles

---

## Phase D — Global Project Orchestrator & Planning

**Goal:** Make the GPO functional. It must be able to receive user requests,
delegate to the Planner, create task teams, and track phases.

**Estimate:** 3–4 days

| # | Task | Est. | Dependencies | Deliverable |
|---|------|------|--------------|-------------|
| D-01 | Implement GPO `receive()` — route events to correct handlers (§2.1.1) | 1 d | C-07, B-01 | GPO delivers events to Planner, DT orchestrators, and User correctly |
| D-02 | Implement GPO `create_task_team()` — register subscriber, spawn DT orchestrator | 0.5 d | D-01 | New DT orchestrator is created and registered on the bus |
| D-03 | Implement GPO `on_phase_complete()` — check if all tasks in a phase are done, emit `phase_complete` | 0.5 d | D-01, C-04 | Phase completion triggers correctly when last task finishes |
| D-04 | Implement GPO `on_addendum_created()` — create new task team for addendum | 0.5 d | D-01 | Addendum task gets its own DT team |
| D-05 | Implement Planner agent: receive `project_initialized`, call LLM with requirements, publish `phase_plan` | 1 d | B-03, C-02 | Planner produces a valid phase plan event |
| D-06 | Implement Planner agent: receive `task_complete`, update status, check if all phases done | 0.5 d | D-05, A-07 | Status tracker updated; final review event emitted if all tasks complete |
| D-07 | Wire Planner + GPO together: end-to-end test (user initializes → planner decomposes → GPO creates teams) | 1 d | D-05, D-01 | Single test simulates full planning flow without real LLM (use mock) |
| D-08 | Implement `ITaskSpecService` (load YAML task specs, save phase plans) | 0.5 d | A-03 | Task specs can be loaded from YAML; phase plans saved |

**Phase D Gates:**
- [ ] GPO correctly routes events to all registered subscribers
- [ ] Planning flow works end-to-end with a mocked LLM
- [ ] Task teams are created and destroyed correctly

---

## Phase E — Git Integration

**Goal:** Connect the orchestrator to a real Git repository. Each task commits
to a branch; each phase creates a PR.

**Estimate:** 2–3 days

| # | Task | Est. | Dependencies | Deliverable |
|---|------|------|--------------|-------------|
| E-01 | Define `IRepoManager` interface (§12.1) | 0.25 d | A-03 | Interface with `create_branch`, `commit`, `create_pr`, `list_branches` |
| E-02 | Implement `GitRepoManager` — branch creation with naming convention `phase-N/task-TN.M` | 0.5 d | E-01 | Branches created with correct naming; all branches listable |
| E-03 | Implement `GitRepoManager` — commit with structured message | 0.25 d | E-02 | Commits include task ID, description, iteration number |
| E-04 | Implement `GitRepoManager` — PR creation with description template | 0.5 d | E-02 | PRs created with body summarizing all commits in the phase |
| E-05 | Wire task completion → commit on task branch (GPO triggers repo commit) | 0.5 d | E-03, D-03 | When a task is approved, its code is committed to its branch |
| E-06 | Wire phase completion → PR creation (GPO triggers PR) | 0.5 d | E-04, D-03 | When all tasks in a phase complete, a PR is opened |
| E-07 | Implement `RepoManager` test helper (uses a temp Git repo via `git` CLI) | 0.5 d | E-02 | Helper sets up a fresh repo, runs a test, tears it down |
| E-08 | Integration test: full task lifecycle ends with a committed branch + PR | 1 d | E-05, E-06 | One end-to-end test with real Git; branch exists, PR exists, files committed |

**Phase E Gates:**
- [ ] `GitRepoManager` wraps a real Git repo (branches, commits, PRs)
- [ ] Integration test passes with real Git (no mocks for Git operations)

---

## Phase F — User Interaction

**Goal:** Connect the User to the system. User provides requirements, reviews PRs,
gives feedback, and approves addenda.

**Estimate:** 2–3 days

| # | Task | Est. | Dependencies | Deliverable |
|---|------|------|--------------|-------------|
| F-01 | Implement User ↔ Planner message relay via GPO (§8.1) | 0.5 d | D-01 | User message reaches Planner; Planner response reaches User |
| F-02 | Implement clarification request flow (§8.2): Planner asks User, User answers | 0.5 d | F-01 | `clarification_request` event → User reply → `clarification_reply` → Planner |
| F-03 | Implement PR presentation to User (§8.3): summary + diff + approve/reject | 0.5 d | E-06 | User receives PR summary; can approve or reject |
| F-04 | Implement addendum task creation from User feedback (§8.4) | 0.5 d | F-03, D-04 | User feedback generates new task spec; new DT team spawned |
| F-05 | Implement User approval gate — system waits for User before merging (§7.3) | 0.5 d | F-03 | System blocks on User input; doesn't auto-merge |
| F-06 | End-to-end user interaction test: requirements → clarification → plan → review → feedback → addendum | 1 d | F-01, F-02, F-04 | Single test with mocked User that exercises the full interactive loop |

**Phase F Gates:**
- [ ] All 4 user interaction patterns tested
- [ ] User is the gate before any merge — no auto-merge path exists

---

## Phase G — Integration, Polish & Documentation

**Goal:** Tie everything together, write integration tests, polish the UX, and
document for users and contributors.

**Estimate:** 2–3 days

| # | Task | Est. | Dependencies | Deliverable |
|---|------|------|--------------|-------------|
| G-01 | Implement final project review (Planner checks all code before submitting to User) | 0.5 d | D-06, E-04 | Planner produces a final review event summarizing all phases |
| G-02 | Implement configuration system (§9): load `config.yaml`, validate, inject into create_app() | 0.5 d | A-03 | Config loaded; LLM provider, model, API key, repo path all configurable |
| G-03 | Implement `create_app()` top-level factory (§12.3) — wires all real implementations | 0.5 d | All phases | One function that produces a working GPO with all services wired |
| G-04 | End-to-end integration test: user initializes → planner decomposes → task completes → commit → PR → user approves | 1.5 d | All phases | One comprehensive test with real Git + mocked LLM |
| G-05 | Write README: architecture overview, quick start, configuration reference | 0.5 d | G-03 | README.md covers install, config, run |
| G-06 | Write developer guide: testing strategy, adding new agents, extending the event bus | 0.5 d | G-03 | Guide in `docs/developers/` |

**Phase G Gates:**
- [ ] Full end-to-end integration test passes
- [ ] `create_app()` is the only entry point
- [ ] README and developer guide written

---

## Task Dependency Graph

```
Phase A (Project Setup & Core Services)
|-> A-01 -> A-02 -> A-03 -> A-04 -> A-05 -> A-06 -> A-07 -> A-08 (smoke test)

Phase B (EventBus)
|-> A-03 -> B-01 -> B-02 -> B-03
|-> A-03 -> B-04
|-> B-01, B-02 -> B-05 (test helpers)
|-> B-01, B-02, B-03, B-04 -> B-06 (E2E smoke test)

Phase C (Agent Infrastructure)
|-> A-02 -> C-01 (response parser)
|-> A-03 -> C-02 (system prompts)
|-> A-03 -> C-03 -> C-04 -> C-05 -> C-06 (state machine + tests)
|-> B-01 -> C-07 (GPO skeleton)

Phase D (GPO & Planning)
|-> C-07, B-01 -> D-01 -> D-02 -> D-03
|                     |                    |
|                     v                    v
|                 D-04              D-05 -> D-06
|                  |                    |
|                  v                    v
|             D-08              D-07 (planning flow test)
|
|-> B-03, C-02 -> D-05 (Planner agent)
|-> D-05, D-01 -> D-07
|-> A-03 -> D-08

Phase E (Git Integration)
|-> A-03 -> E-01 -> E-02 -> E-03 -> E-04
|                              |
|-> E-05 -> E-06
|       ^       ^
|    E-03    E-04
|
|-> D-03 -> E-05, E-06
|-> E-02 -> E-07 -> E-08 (E2E test)

Phase F (User Interaction)
|-> D-01 -> F-01 -> F-02
|                |
|                v
|            F-03 -> F-04 -> F-05
|            E-06 -> F-03
|
|-> F-01, F-02, F-04 -> F-06

Phase G (Integration & Polish)
|-> D-06, E-04 -> G-01
|-> A-03 -> G-02
|-> All phases -> G-03
|-> All phases -> G-04
|-> G-03 -> G-05, G-06
|-> G-04 -> G-05, G-06
```

---

## Effort Summary

| Phase | Tasks | Est. (days) | Key Milestone |
|-------|-------|-------------|---------------|
| A — Project Setup & Core Services | 8 | 2–3 | Event schema, interfaces, persistence, logging, status tracker working |
| B — EventBus | 6 | 3–4 | In-memory + WAL-backed bus, agent adapter, coordinator routing |
| C — Agent Infrastructure | 7 | 3–4 | State machine + adapter + prompts tested |
| D — GPO & Planning | 8 | 3–4 | Planning flow works end-to-end |
| E — Git Integration | 8 | 2–3 | Real branches, commits, PRs |
| F — User Interaction | 6 | 2–3 | User can guide the system through feedback loops |
| G — Integration & Polish | 6 | 2–3 | Full E2E test + docs |
| **Total** | **49** | **~17–24** | **Working multi-agent orchestrator** |

---

## Notes

- **Parallelism:** Phases A and B can partially overlap — once interfaces (§12.1)
  are defined, C-03 (state machine) can start before B-01 (event bus) finishes.
- **Mock-first:** Every phase should have unit tests before integration tests.
  Build with mocks, then swap in real implementations in Phase G.
- **Git is optional early on:** You can develop and test the full orchestrator
  without real Git. Use `InMemoryEventBus` and mock `IRepoManager` until Phase E.
- **LLM calls:** Never call a real LLM in CI or unit tests. Always use
  `MagicMock` or `InMemoryEventBus`-based mocks.
- **Dependency stability:** When adding new methods to interfaces (§12.1), always
  use default parameters so existing implementations don't break.
