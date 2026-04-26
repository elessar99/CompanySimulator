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
- replace simple direct movement with navmesh-backed movement
- complete office worker test slice before starting agent work

### Iteration 5
- add detected agent NPC flow
- connect detection state to spawn and dismissal

## Test Checkpoint Rule
Before starting any new NPC phase, stop and test the latest completed slice in-game.

Current checkpoint order:
1. test core NPC + interview session flow
2. fix interview issues if found
3. only after interview is stable, continue with office worker implementation
4. finish office worker sit/wander behavior
5. finish point-of-interest integration
6. finish navmesh movement integration
7. test the full office worker slice in-game
8. fix office worker issues if found
9. only after office worker is stable, continue with agent NPC implementation

If a phase is partially implemented and not yet tested, do not begin the next phase.

## Current Resume Point
Interview checkpoint passed.
Office worker core loop checkpoint passed as temporary baseline.

Resume from here:
1. implement detected agent NPC flow
2. spawn visible agent actors only for detected player-targeted agents
3. add simple player interact -> dismiss flow
4. test detected spawn and dismissal loop
5. fix agent baseline issues found during testing

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
- Interview flow tested and accepted as current baseline
- Office worker groundwork started: `OfficeDeskCapacityService`, `OfficeWorkerNpcRuntimeData`, `OfficeWorkerManager`
- Multi-seat office desk discovery now resolves every `EmployeeNpc` seat under `OfficeDeskController`
- Current desk setup preference: one `OfficeDeskController` per chair slot
- Next office worker milestone must include `Point of Interest` support and `NavMesh` movement before the next test checkpoint
- Office worker sit/wander loop implemented as current baseline
- Office point-of-interest groundwork added: `OfficePointOfInterest`, `OfficePointOfInterestService`, `OfficePointOfInterestType`
- `NpcActor` movement now prefers `NavMeshAgent` when present and falls back to direct movement otherwise
- Office worker core loop accepted for now; polish will be deferred until model/animation integration
- Agent groundwork started: `AgentNpcRuntimeData`, `DetectedAgentManager`, `DetectedAgentInteractable`

## Next Start Point
Current next implementation pass should begin with:
1. detected agent runtime data
2. detected agent manager
3. player interaction dismissal flow
4. then stop for agent testing and fixes

## Planned Office Worker Test Setup
After `Point of Interest` and `NavMesh` work is implemented, test with this setup:

1. `OfficeDeskController`
- one controller per chair slot
- each controller must have one `SeatController`
- that seat must use `SeatPoint.AllowedOccupantType = EmployeeNpc`

2. worker roles
- use at least one `EmployeeRoleDefinition` with `RequiresOffice = true`
- use at least one role with `RequiresOffice = false` to confirm no office NPC is spawned for that role

3. office worker manager scene objects
- `EmployeeManager`
- `OfficeWorkerManager`
- `OfficeDeskCapacityService`
- worker NPC prefab if you want model testing; otherwise fallback capsule is acceptable for logic testing

4. navmesh setup
- bake a walkable navmesh for the office floor
- ensure desks and blockers affect navigation as intended
- ensure worker start seat and optional visit targets are on reachable walkable areas

5. point-of-interest setup
- add at least two visit targets such as coffee machine or vending area
- verify each target has the future point-of-interest component/config required by the implementation
- place targets far enough from desks that walking behavior is visible

6. test checklist
- hire an office-required worker and confirm NPC spawns
- confirm worker initially sits in its own desk slot
- confirm worker leaves seat after a delay
- confirm worker walks through navmesh instead of straight-line clipping
- confirm worker can choose a point-of-interest target when available
- confirm worker returns to a valid seat afterward
- confirm firing despawns the worker NPC
- confirm non-office roles never spawn office NPCs

7. bug triage order
- wrong seat selection
- unreachable navmesh path
- point-of-interest target selection
- return-to-seat failures
- fire/despawn cleanup
