# Interview Negotiation System Plan

## Purpose
Build a modular interview negotiation system on top of the current applicant interview flow.

This document is the source of truth for the negotiation implementation phase and should be updated as progress continues.

## Design Goals
- Support multi-step salary negotiation.
- Support varied NPC dialogue phrasing.
- Keep negotiation logic separate from text generation.
- Keep all dialogue content ready for future multi-language support.
- Keep runtime logic deterministic enough to test, while still allowing probabilistic NPC responses.
- Reuse the existing interview session foundation already implemented in the project.

## Current Baseline
Already implemented:
- applicant -> interview NPC spawn flow
- CEO desk interview seating
- simple accept / reject dialogue UI
- applicant hire / reject end results
- negotiation core models and orchestrator skeleton
- state-aware temporary negotiation panel with offer input

This plan expands that baseline into a full negotiation loop.

## Core Rule Summary
- Each applicant already has a base salary expectation from `ExpectedDailySalary`.
- That value is the negotiation reference point.
- NPC can either:
  - open with its own offer
  - or ask the player to make the first offer
- Player can respond with accept / reject / offer depending on the current negotiation state.
- NPC may accept, reject and leave, or reject and counter.
- A counter-offer phase is terminal from the player's side: after the NPC counter-offers, the player can only accept or reject.

## Salary Reference Definitions
- `BaseExpectation`: the applicant's current `ExpectedDailySalary`
- `NpcOpeningOffer`: NPC's first explicit number, if NPC opens with a number
- `PlayerOffer`: the current player offer
- `HighestPlayerOffer`: highest offer player has made during this interview
- `NpcLastOffer`: last number spoken by NPC
- `NormalizedOfferRatio = PlayerOffer / BaseExpectation`

## Required Negotiation States
Create an explicit negotiation state machine.

### High-Level States
1. `NotStarted`
2. `NpcOpeningStatement`
3. `WaitingForPlayerDecisionOnNpcOffer`
4. `WaitingForPlayerOpeningOffer`
5. `EvaluatingPlayerOffer`
6. `NpcCounterOffer`
7. `WaitingForPlayerDecisionOnCounterOffer`
8. `Accepted`
9. `RejectedByNpc`
10. `RejectedByPlayer`
11. `InterviewClosed`

### Recommended Runtime Turn Marker
Use a separate turn enum too:
- `Npc`
- `Player`
- `System`

This keeps UI enable/disable logic simpler.

## Note1 Flow Interpretation
### Scenario
NPC begins by making its own offer.

### NPC opening offer
If NPC opens with a number, that number must be generated in this range:
- minimum = `BaseExpectation * 0.90`
- maximum = `BaseExpectation * 1.50`

### Player options after NPC opening offer
Player can:
1. accept
2. end interview / reject
3. make a counter-offer

### If player counter-offers
NPC evaluates the player's offer.
Then NPC can do one of these:
1. accept
2. reject and end interview
3. reject and make one counter-offer

### If NPC makes that counter-offer
The player may only:
1. accept
2. reject

Player may not submit another custom number after that counter-offer.

### Counter-offer generation rule
NPC counter-offer should be generated between:
- NPC's most recent spoken offer
- player's highest offer so far

Then clamp the final result to:
- minimum = `BaseExpectation * 0.70`
- maximum = `BaseExpectation * 1.20`

## Note2 Flow Interpretation
### Scenario
NPC does not open with a number.
Instead NPC asks the player what salary they are offering.

### Player submits first offer
NPC evaluates the offer and can:
1. accept it
2. reject and end interview
3. reject and present its own offer

### Branch A: rejected player offer is below 70% of expectation
If the rejected player offer is below `BaseExpectation * 0.70` and NPC chooses to answer with its own number,
then continue from the full NPC-offer branch like Note1's beginning.

Meaning:
- NPC has now made an offer
- player may accept / reject / make a counter-offer
- if player counters, NPC may accept / end / make one final counter-offer

### Branch B: rejected player offer is at least 70% of expectation
If the rejected player offer is greater than or equal to `BaseExpectation * 0.70` and NPC chooses to answer with its own number,
then skip the full opening-offer branch and treat that NPC response as the final counter-offer stage.

Meaning:
- player may only accept or reject
- player may not submit another custom number
- interview ends after that decision

## Offer Acceptance Logic
### Hard boundaries
- If `NormalizedOfferRatio <= 0.50`:
  - acceptance probability = `0`
- If `NormalizedOfferRatio >= 1.00`:
  - acceptance probability = `1`

### Between 50% and 100%
For `0.50 < ratio < 1.00`:
- acceptance probability must be parabolic
- probability should rise slowly near 50%
- probability should rise quickly near 100%

Recommended normalized parameter:
- `t = (ratio - 0.50) / 0.50`
- then use a convex-up curve such as `t * t`

This yields:
- near 50% -> very low chance
- near 100% -> quickly rising chance

This should remain configurable in case curve tuning is needed later.

## Configurable Settings Requirement
All major behavior values must be adjustable through a manager-owned config model, not hardcoded.

### Required configurable values
1. NPC opens with own offer probability
2. NPC opening offer min multiplier
3. NPC opening offer max multiplier
4. low-offer hard rejection floor multiplier
5. guaranteed acceptance multiplier
6. NPC rejection -> end interview probability
7. NPC rejection -> counter-offer probability
8. counter-offer minimum multiplier
9. counter-offer maximum multiplier
10. acceptance curve mode / coefficient inputs
11. dialogue variation randomization settings
12. repeated line cooldown / anti-repeat settings

## Suggested Config Types
Recommended structure:
- `InterviewNegotiationSettings`
- `InterviewNegotiationProbabilitySettings`
- `InterviewNegotiationDialogueSettings`

Potential serialization options later:
- serialized class on manager
- or ScriptableObject definition if authoring grows larger

## Dialogue System Requirements
Dialogue must not be hardcoded inline with the logic.

### Rule
Separate:
- negotiation decision logic
- dialogue text lookup / generation

### Suggested dialogue intent categories
- `NpcOpeningOffer`
- `NpcRequestsPlayerOffer`
- `NpcAcceptsOffer`
- `NpcSoftRejectsOffer`
- `NpcHardRejectsOffer`
- `NpcCounterOffers`
- `NpcEndsInterview`
- `PlayerAcceptedNpcOffer`
- `PlayerRejectedNpcOffer`

### Variation support
Each intent must support multiple line variants.

Example approach:
- intent key -> list of templates
- randomly choose one template
- avoid repeating the most recent line when alternatives exist

### Number insertion
Templates should support placeholders like:
- applicant name
- role name
- salary amount
- previous offer amount

## Localization Requirement
Future language support must be assumed now.

### Required architectural rule
Do not design the negotiation text system around raw hardcoded Turkish strings inside logic classes.

### Instead
Separate:
- `logic keys / intents`
- `localized text content`
- `runtime numeric formatting`

### Plan for future language support
The system should later be able to map:
- `DialogueIntent + Tone + Context`
into
- localized text entries for Turkish / English / others

### Suggested future-ready model
- `InterviewDialogueLineKey`
- `InterviewDialogueIntent`
- `InterviewDialogueVariantCollection`
- `InterviewDialogueLineProvider`

The negotiation manager should ask for:
- a dialogue key or generated dialogue payload
not a final hardcoded string.

### Important localization rule
Store numeric negotiation logic separately from sentence text.
For example:
- logic computes amount `125`
- localization layer formats line such as
  - Turkish: `Günlük 125 teklif ediyorum.`
  - English: `I am offering 125 per day.`

### Result
When more languages are added, only content/provider layers change, not negotiation rules.

## Suggested Runtime Data Model
Recommended runtime model additions:
- `BaseExpectation`
- `NpcOpeningOffer`
- `NpcLastOffer`
- `HighestPlayerOffer`
- `LastPlayerOffer`
- `NegotiationState`
- `CurrentTurn`
- `WasNpcOpeningOfferBranch`
- `IsFinalDecisionStage`
- `NegotiationOutcomeReason`
- `NegotiationHistoryEntries`

### History entry suggestion
Track each spoken/decision event as:
- speaker
- intent
- value if numeric
- state before
- state after

This helps later debugging and UI transcript display.

## Suggested Services
Keep the system modular using service classes.

### Recommended service breakdown
1. `InterviewOfferAcceptanceService`
- computes acceptance probability
- resolves accept / reject

2. `InterviewOpeningOfferService`
- decides if NPC opens with number or requests player offer
- generates opening offer when needed

3. `InterviewCounterOfferService`
- generates valid counter-offers
- clamps to allowed range

4. `InterviewDialogueGenerator`
- chooses line variants by intent
- returns localization-ready payload

5. `InterviewNegotiationOrchestrator`
- applies Note1 / Note2 state transitions
- updates session runtime data

## UI Plan
The existing simple panel must be expanded.

### Required UI elements
- NPC dialogue text block
- optional negotiation transcript area
- player numeric offer input field
- `Accept` button
- `Reject` button
- `Submit Offer` button

### UI rule
Button visibility / interactivity must depend on state.

#### When NPC made an opening offer
Player sees:
- accept
- reject
- submit offer

#### When NPC asked for player offer
Player sees:
- submit offer
- reject

#### When NPC gave final counter-offer
Player sees:
- accept
- reject
Only these two.

## Integration Targets in Current Codebase
The main current integration point will be:
- `InterviewSessionManager`

Likely affected runtime/UI classes later:
- `InterviewSessionRuntimeData`
- `InterviewDialoguePanelUI`
- `EmployeePanelUI` only indirectly

## Implementation Order
### Phase 1
- add negotiation settings model
- add negotiation states and runtime data extensions
- add acceptance evaluator service

### Phase 2
- add NPC opening decision service
- add counter-offer service
- encode Note1 / Note2 flow in orchestrator

### Phase 3
- add dialogue intent model
- add variation-ready dialogue provider
- keep localization-ready key separation

### Phase 4
- expand interview UI to multi-step negotiation controls
- bind buttons and offer input to state transitions

### Phase 5
- test matrix validation
- edge case handling
- finalize integration with hire/reject outcomes

## Test Matrix Requirements
Test at least these cases:
1. NPC opens with offer, player accepts
2. NPC opens with offer, player rejects
3. NPC opens with offer, player counters, NPC accepts
4. NPC opens with offer, player counters, NPC ends interview
5. NPC opens with offer, player counters, NPC final-counter-offers, player accepts
6. NPC opens with offer, player counters, NPC final-counter-offers, player rejects
7. NPC requests player offer, player gives <= 50%, NPC never accepts
8. NPC requests player offer, player gives 51% to 99%, probabilistic decision works
9. NPC requests player offer, player gives >= 100%, NPC always accepts
10. rejected player offer below 70% enters full Note1-style branch
11. rejected player offer >= 70% enters final-counter-only branch
12. counter-offer clamping respects 70% and 120% limits
13. repeated dialogue lines vary when alternatives exist
14. logic still works even if only one dialogue variant exists
15. localization-ready payload can be generated without coupling to one language

## Immediate Next Implementation Start
When implementation begins, start with:
1. negotiation settings data model
2. negotiation state enum
3. interview session runtime expansion
4. offer acceptance service

Only after these exist should dialogue generation and UI be expanded.

## Progress Notes
- Phase 1 completed: settings, state, turn, outcome, dialogue intent, runtime history
- Phase 2 started: opening offer, acceptance and counter-offer services integrated
- Phase 3 started: intent-based payload generation and temporary panel text routing integrated
- Phase 4 started: offer input and state-based button visibility added to the current panel
- Phase 4 expanded: final-counter-only branch and player offer restrictions are now enforced in runtime
- Next checkpoint: run full interview negotiation playtest, capture issues, then refine UI polish and dialogue content
