# NPC System Plan

## Goal
Create a modular NPC system for:
1. interview NPC flow
2. office worker NPC flow
3. detected agent NPC flow

This document is the working source of truth and should be updated as implementation progresses.

## Phase Order
1. Interview NPC
2. Office Worker NPC
3. Agent NPC

## Core Architecture Rules
- Keep runtime data separate from scene actors.
- Keep animation integration behind a dedicated bridge layer.
- Reuse a shared NPC actor/state pipeline for employees and agents.
- Keep future morale, strike, and office-need systems as extension points, not hardcoded into initial actor logic.
- Keep office seat capacity independent from hiring UI so furniture capacity can change later.

## Target Folder Layout
- `Runtime/Models` - shared NPC runtime data
- `Runtime/Actors` - scene actor components and presentation sync
- `Runtime/Services` - orchestration and lookup services
- `Runtime/Animation` - animator bridge and animation contracts
- `Runtime/Interview` - interview session flow
- `Runtime/Office` - office capacity and worker behavior
- `Runtime/Agents` - detected agent behavior
- `Runtime/Definitions` - future NPC-specific authoring definitions

## Planned Runtime Layers
### 1. Core NPC Layer
Shared concepts to add first:
- `NpcKind`
- `NpcLifecycleState`
- `NpcRuntimeData`
- `NpcActor`
- `NpcAnimationBridge`

### 2. Interview Layer
Goal:
- selected applicant appears as an interview NPC at CEO desk
- candidate sits in interview seat
- salary negotiation session runs through a dedicated runtime session
- result leads to hire or reject

Planned pieces:
- `InterviewSessionRuntimeData`
- `InterviewSessionManager`
- `InterviewNpcRuntimeData`
- `InterviewSeatReservationService`
- UI hook from `EmployeePanelUI` applicant selection

### 3. Office Worker Layer
Goal:
- only office-required roles spawn as office NPCs
- active office workers can wander, sit, and visit future points of interest
- firing removes the office NPC from the scene

Planned pieces:
- `OfficeWorkerNpcRuntimeData`
- `OfficeWorkerManager`
- `OfficeDeskCapacityService`
- `OfficeSeatGroup` or equivalent seat-capacity contract
- worker state flow: idle, walking, seating, seated, visiting-point

### 4. Office Capacity Layer
Goal:
- hiring limit comes from placed office desk seat capacity
- support 1, 3, 4, 6 seat desk variations

Planned pieces:
- seat group contract on placed office desks
- office seat discovery service
- total office worker capacity calculation
- later overflow handling when desks are removed

### 5. Point of Interest Layer
Goal:
- future coffee machine, food vending, fun area, etc. can become worker visit targets
- later morale system can consume the same data

Planned pieces:
- shared point-of-interest contract
- worker visit target selector
- extensible scoring hooks for future morale system

### 6. Agent Layer
Goal:
- player-targeted agent appears only after detection
- player removes the agent through direct interaction/combat outcome

Planned pieces:
- `AgentNpcRuntimeData`
- `DetectedAgentManager`
- scene actor spawn tied to `PlayerTargetedAgentRuntimeData.IsDetected`
- dismiss flow tied to existing agent runtime

## Animation Integration Strategy
Animation should stay easy to swap.

Planned rule:
- gameplay state never talks to `Animator` directly
- `NpcAnimationBridge` maps generic state into animator parameters

Suggested generic parameters:
- `MoveSpeed`
- `IsMoving`
- `IsSeated`
- `IsTalking`
- `IsInterviewing`
- `IsAlert`
- `IsAgent`
- `StateIndex`

This allows replacing animator controllers without rewriting NPC logic.

## Implementation Sequence
### Iteration 1
- create core NPC folders and scaffolding
- define shared runtime data and actor contracts
- define animation bridge contract

### Iteration 2
- build interview session flow
- connect applicant selection to interview spawning
- seat candidate at CEO desk opposite seat
- complete hire/reject result handling

### Iteration 3
- add office desk capacity calculation
- add office worker spawn/despawn for office-required roles
- add basic worker sit/wander loop

### Iteration 4
- add point-of-interest abstraction for future office props
- connect worker wandering to optional office targets

### Iteration 5
- add detected agent NPC flow
- connect detection state to spawn and dismissal

## Test Checkpoint Rule
Before starting any new NPC phase, stop and test the latest completed slice in-game.

Current checkpoint order:
1. test core NPC + interview session flow
2. fix interview issues if found
3. only after interview is stable, continue with office worker implementation
4. after office worker is stable, continue with agent NPC implementation

If a phase is partially implemented and not yet tested, do not begin the next phase.

## Current Resume Point
Do not continue with office worker NPCs yet.

Resume from here after current testing is complete:
1. verify applicant -> interview spawn flow
2. verify CEO desk interview seat usage
3. verify interview dialogue open/close flow
4. verify `Kabul Et` result
5. verify `Reddet` result
6. fix any interview bugs found during testing
7. then start office worker capacity and spawn/despawn phase

## Deferred Systems
These are intentionally deferred but must stay compatible:
- morale score
- office amenity points
- strikes
- non-office employees with no visible NPC representation
- desk reduction overflow resolution
- richer combat/agent eviction presentation
- save/load for NPC scene state

## Current Status
- [x] Plan persisted in repo
- [x] NPC folder skeleton created
- [x] Core NPC scaffolding implemented
- [ ] Interview NPC implemented
- [ ] Office worker NPC implemented
- [ ] Agent NPC implemented

### Active Progress Notes
- Core NPC runtime scaffolding added: `NpcKind`, `NpcLifecycleState`, `NpcRuntimeData`, `NpcActor`, `NpcAnimationBridge`
- Interview foundation added: `InterviewSessionManager`, `InterviewSessionRuntimeData`, `InterviewNpcRuntimeData`
- Applicant selection now starts an interview session instead of hiring instantly
- Simple interview dialogue panel added with requested salary text and `Kabul Et` / `Reddet` actions
- Next active development phase is paused until interview flow is tested and stabilized

## Next Start Point
After testing and bug fixes, next implementation pass should begin with:
1. office worker seat capacity service
2. office worker runtime data and manager
3. office worker spawn/despawn flow for office-required roles
