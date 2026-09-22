# DAEDALUS — Game Design Analysis → Refactor Plan

> **Purpose**
>
> This is the design-driven refactor overlay for Daedalus.
>
> It translates the current game's mechanics into a set of refactors whose purpose is to make the game's **fun, decisions, tension, mastery, and replayability** easier to design and test.
>
> It does **not** replace `DAEDALUS_IMPLEMENTATION_PLAN.md`. The implementation plan remains the production roadmap; this document determines which architectural changes are justified when the production roadmap exposes a gameplay problem.

---

## 1. Design Thesis

Daedalus should not primarily be a dungeon generator, combat simulator, or progression spreadsheet.

Its identity is the repeated expedition decision:

**Prepare → descend → read the ruin → make choices under uncertainty → spend resources → gain value → decide whether to push or extract → return changed → prepare again**

The most important player question is:

> **“How much farther do I dare go with what I have?”**

That question should exist at multiple scales.

### Moment

**What should I do right now?**

### Encounter

**What is the safest / most valuable way through this problem?**

### Expedition

**Do I spend resources now, avoid the threat, or push deeper?**

### Campaign

**How should I prepare the party for the next descent?**

### Mastery

**What have I learned that lets me make better decisions than I made last time?**

The refactor therefore prioritizes systems that make those questions explicit.

---

# 2. Current-State Design Audit

This audit is based on the current `develop` branch implementation.

## 2.1 Exploration: functional, but weak as a decision system

The current exploration loop is structurally sound:

- movement
- visibility
- discovered cells
- interactables
- combat nodes
- chest
- terminal
- extraction
- periodic events

But most of the important choices are still predetermined.

### Evidence

`GameWorld.CreateNodes()` currently creates a small fixed set of node types around the generated floor:

- start
- chest
- terminal
- combat
- extraction

`CreateEnemies()` places a fixed number of enemies based mainly on floor.

`EventSystem` contains only three event definitions and `Roll()` deterministically selects one from seed/floor/turn.

`DungeonGenerator` creates rooms and connects them sequentially, then enlarges the starting area into a reference chamber.

### Design consequence

The player can explore, but the world currently offers relatively few situations in which **knowledge, route choice, risk assessment, or resource planning changes the outcome**.

The refactor target is not “more procedural generation.”

It is:

**more meaningful situations generated from controllable content rules.**

---

# 3. Combat: strong skeleton, shallow decision surface

The current battle system already has useful foundations:

- interleaved initiative
- target selection
- attack / skill / item / defend / run
- status effects
- enemy behavior
- gear influence
- morale influence
- signature skills
- defeat handling
- battle animation events

That means combat should be **refactored**, not rewritten.

## Current design limitations

### 3.1 Skills are hard-coded by character identity

`BattleSystem.GetSkillDefinition()` selects skills through `SpriteId`.

This makes character abilities difficult to:

- balance independently
- expand
- inspect
- combine
- test
- author as content

### 3.2 Enemy behavior is inferred from names

`InferEnemyBehavior()` maps strings such as “guard”, “stalker”, and “brute” to behavior.

This is a prototype shortcut, not a production content model.

A creature's behavior should be an explicit authored property.

### 3.3 Status effects are string-based

Examples:

`"Guarded"`

`"Poisoned"`

`"Exposed"`

Strings make future interactions harder to reason about and easy to misspell.

### 3.4 BattleSystem owns too much

The current `ui/BattleSystem.cs` contains:

- battle state transitions
- turn sequencing
- target selection
- damage formulas
- skill definitions
- enemy behavior
- status handling
- inventory consumption
- morale changes
- animation event creation
- combat messaging

This makes it difficult to answer a design question such as:

> “What happens if we change the value of defending?”

without touching a very large class that also owns unrelated concerns.

### Design consequence

Combat works, but **combat design iteration is expensive**.

The goal is not abstraction for its own sake. The goal is to isolate the pieces that determine tactical depth.

---

# 4. Expedition Risk: currently too flat

The extraction loop exists, and carried rewards are separated from campaign rewards.

That is the correct foundation.

The problem is that expedition pressure is currently represented by relatively few variables:

- HP
- inventory/resource accumulation
- upkeep
- morale
- floor/depth

Upkeep currently increases per movement turn, while extraction is tied to a fixed extraction node.

There is limited interaction between:

**what the player has gained**

and

**what remaining danger costs them.**

## Target design

An expedition should develop a readable pressure curve:

**Value rises**

while

**resources / safety fall**

and eventually:

**the value of continuing becomes difficult to distinguish from the risk of losing it.**

We should make that pressure model explicit before adding more loot or more enemies.

---

# 5. Progression: mostly numerical, not decision-driven

The current progression foundation includes:

- XP
- level
- HP / MP growth
- stat growth
- gear power
- cores
- materials
- recipes
- morale

This is enough for a prototype.

The current problem is that many outcomes are still additive:

**more level → more stats**

**more gear → more power**

**more morale → small multiplier**

That produces progression, but not necessarily build identity.

The long-term target is not “more stats.”

It is:

> **More ways to solve the same expedition problems.**

A progression choice should eventually change:

- what the party can risk
- what encounters favor them
- how resources are conserved
- which enemy behaviors they exploit
- when they choose to retreat

---

# 6. Campaign: currently a state destination, not yet a preparation game

The campaign state already stores the right broad categories:

- roster
- stash
- cores
- gear
- materials
- recipes
- alignment
- flags
- depth
- run history

The missing piece is decision density.

The campaign should answer:

> **“What am I changing before this next run?”**

That means preparation needs to become an explicit player-facing decision layer rather than merely a place the expedition returns to.

---

# 7. Architecture Findings

## 7.1 GameSession is now the primary refactor hotspot

`game/GameSession.cs` has become the orchestration center for:

- movement
- interaction
- event triggering
- visibility
- battles
- victory/defeat
- extraction
- campaign return
- dev tools
- gear synchronization
- compatibility synchronization
- state transitions

This is acceptable for a prototype, but it is now the biggest obstacle to gameplay iteration.

### Refactor target

Turn `GameSession` into a **thin game-flow coordinator**.

It should coordinate subsystems rather than contain their design rules.

---

## 7.2 GameWorld mixes simulation, content assembly, and compatibility concerns

`GameWorld` currently owns:

- map storage
- floor generation
- enemy creation
- props
- nodes
- visual features
- player compatibility state
- defeated enemy tracking

The render-only visual feature system is appropriately separated conceptually, but gameplay content is still assembled directly inside the world object.

### Refactor target

Separate:

**world topology**

from

**floor content**

from

**presentation metadata**

without creating a generic world-editor framework.

---

## 7.3 State contains compatibility fields

`ExpeditionState` contains both newer and older concepts, including:

- `Health`
- `MaxHealth`
- `PlayerGridPosition`
- `CarriedInventory`
- legacy `Inventory`

`CampaignState` likewise retains explicit legacy fields such as:

- `EnergyCores`
- `RustedCatalysts`
- `UnlockedBlueprintIds`
- `LevelCapModifier`

These are not the first thing to delete.

They are signals that the model is carrying multiple generations of the prototype.

### Refactor target

First make one canonical model authoritative.

Then remove compatibility surfaces after tests and save migration protect the new model.

---

## 7.4 Content is embedded in system code

Examples include:

- starter gear in `ContentCatalog`
- enemy scaling in `GameWorld`
- skills in `BattleSystem`
- event definitions in `EventSystem`
- item behavior in `BattleSystem`

The project does not yet need a full external data pipeline.

It does need **small, explicit content definitions**.

---

# 8. Refactor Principles

### Principle A — Refactor toward design experiments

Every refactor must make a specific design question easier to answer.

Bad:

> “Split a 25 KB class because it is large.”

Good:

> “Separate battle rules from battle presentation so defend values and skill costs can be tuned without touching animation handling.”

### Principle B — Preserve behavior until the new seam exists

Do not combine architectural change and balancing change unless the current behavior is itself the tested reference.

### Principle C — One authoritative state

Avoid synchronization between duplicate values wherever practical.

### Principle D — Content should be explicit

A skill, enemy behavior, event, reward, or encounter should have a discoverable definition.

### Principle E — Keep System.Drawing architecture intact

The visual renderer is not the current reason the game is difficult to design.

Do not replace the rendering approach merely because it is not production-grade.

### Principle F — Every refactor phase ends in a playable or testable experiment

No architecture-only milestone.

---

# 9. Refactor Sequence

# R0 — Design Instrumentation + Scenario Harness

## Goal

Make gameplay behavior measurable before changing the rules.

## Add

`game/Design/GameplayEvent.cs`

A small immutable event record for events such as:

- ExpeditionStarted
- RoomEntered
- DiscoveryMade
- Interaction
- BattleStarted
- CommandSelected
- AbilityUsed
- DamageTaken
- EnemyDefeated
- RewardCollected
- ExtractionPresented
- ExtractionChosen
- ExpeditionEnded
- Defeat

`game/Design/GameplayRecorder.cs`

A lightweight recorder with an in-memory implementation for tests and an optional JSON-lines implementation for local playtests.

`tests/Daedalus.Tests/DesignAnalysisTests.cs`

Test deterministic scenario playback.

## Add scenario helpers

Create a deterministic benchmark scenario that can:

1. start an expedition
2. move through the benchmark room
3. trigger combat
4. collect a reward
5. reach extraction
6. extract

The point is repeatable observation.

## Exit condition

We can compare two gameplay implementations using the same scenario and inspect:

- actions
- resource state
- combat choices
- rewards
- extraction decision
- duration / turns

without manually reconstructing the run from memory.

---

# R1 — Separate Game Flow from Domain Actions

## Goal

Make `GameSession` an orchestrator rather than the place where gameplay rules live.

## Introduce

`game/Application/GameFlow.cs`

Own:

- current game state
- routing between exploration / battle / campaign / extraction
- high-level commands

`game/Application/ExplorationActions.cs`

Own:

- movement requests
- interactions
- exploration event requests
- extraction requests

`game/Application/BattleActions.cs`

Own:

- player battle commands
- target selection requests
- completion of battle actions

## Rule

These application objects may call existing systems.

Do not rewrite systems yet.

## GameSession after R1

`GameSession` should primarily:

- hold current flow state
- expose commands to UI
- route commands to application services
- publish resulting state / messages

## Exit condition

A change to exploration rules no longer requires editing the battle code path inside `GameSession`.

---

# R2 — Explicit Exploration Content Model

## Goal

Turn exploration into a system capable of creating decisions rather than only locations.

## Introduce

`game/World/FloorDefinition.cs`

Contains:

- biome
- floor number / tier
- room archetypes
- encounter rules
- reward rules
- event pool
- extraction rules
- landmark rules

`game/World/EncounterDefinition.cs`

Contains:

- encounter ID
- enemy composition
- reward tier
- optional special rule
- danger rating

`game/World/ExplorationEventDefinition.cs`

Contains:

- event ID
- choices
- requirements
- outcomes
- presentation text

## Change

`DungeonGenerator` generates topology.

`GameWorld` materializes topology.

A separate content/materialization layer determines:

- which encounters exist
- where optional rewards appear
- which events can occur
- what kind of room the player is entering

## Preserve

The benchmark room remains authored and deterministic.

Do not replace the benchmark with pure procedural generation.

## Exit condition

We can create two materially different floor experiences by changing content definitions without changing the dungeon algorithm.

---

# R3 — Refactor Combat Around Tactical Concepts

## Goal

Expose the actual tactical design variables.

## Introduce

`game/Combat/SkillDefinition.cs`

Properties should cover the minimum required for current design:

- ID
- name
- power
- resource cost
- targeting rule
- damage type
- status effects
- tags
- presentation key

`game/Combat/EnemyDefinition.cs`

Properties:

- ID
- family
- behavior
- base stats
- abilities
- target policy
- reward tier

`game/Combat/StatusEffectType.cs`

Replace string identifiers with an enum or immutable ID type.

`game/Combat/CombatActionResult.cs`

A deterministic result describing:

- actor
- target(s)
- resource changes
- damage
- statuses
- defeat
- follow-up actions

## Move out of BattleSystem

- character skill definitions
- enemy behavior inference
- hard-coded status strings
- reward semantics
- inventory-specific item lookup

## BattleSystem retains

- turn sequencing
- battle-state mutation
- legality checks
- deterministic combat resolution

## Presentation receives

`CombatActionResult`

and turns it into:

- animation events
- message
- visual emphasis

instead of the combat rules creating presentation events directly.

## Exit condition

Changing a skill's power/cost/status behavior requires editing a content definition and tests, not `BattleSystem` internals.

---

# R4 — Build the Expedition Pressure Model

## Goal

Make “push deeper or extract” a real gameplay system.

## Introduce

`game/Expedition/ExpeditionRiskState.cs`

Possible tracked concepts:

- carried value
- current health pressure
- available healing
- morale pressure
- upkeep / time pressure
- current depth
- known danger
- extraction availability

Do not implement every possible variable immediately.

The first version should answer:

> **How much have I gained, what can I still safely spend, and what am I risking by continuing?**

## Introduce

`game/Expedition/ExtractionDecision.cs`

An explicit result describing:

- carried value at risk
- banked value
- cost paid
- remaining party condition

## Change

Extraction should become a meaningful decision point, not merely an endpoint.

Possible first production rule:

- rewards remain carried until extraction
- deeper areas increase reward quality
- party condition deteriorates
- meaningful recovery is limited
- extraction can occur when the game rules say it can

The exact numerical tuning comes after the refactor.

## Exit condition

A player can explain why they extracted in one run and pushed deeper in another.

---

# R5 — Refactor Progression Toward Build Decisions

## Goal

Make progression alter future choices rather than merely increase numbers.

## Introduce

`game/Progression/CharacterDefinition.cs`

Explicitly define:

- role
- base stats
- skill IDs
- growth profile
- tags

`game/Progression/GearDefinition.cs`

Separate:

- identity
- slot
- base power
- effect rules

from runtime ownership/equipment state.

## Change

`ProgressionSystem` becomes the executor of progression.

Definitions determine what a character / gear item actually is.

## First design target

Each core character should present at least one meaningful tactical tradeoff.

Examples of the desired shape:

**Arden**
- better at direct physical pressure
- gives up something when optimizing for high burst

**Lyra**
- strong magical/status utility
- has resource or setup pressure

**Marek**
- high durability / impact
- slower or more position-dependent

**Sera**
- target setup / exposure / utility
- lower direct output

These are design directions, not final balance values.

## Exit condition

Changing party composition changes the player's preferred answers to expedition problems.

---

# R6 — Make Morale a Decision System

## Goal

Prevent morale from being only a hidden multiplier.

Current morale is mostly:

**event → numerical change → small combat multiplier**

The refactor should make it a source of decisions or consequences.

## Introduce

`game/Progression/MoraleEffect.cs`

Explicit thresholds / effects.

Potential bands:

- confident
- steady
- shaken
- broken

The first implementation should expose only a small number of effects.

Examples:

- altered retreat consequences
- event availability
- temporary combat modifier
- recovery cost

Do not implement campaign drama until playtesting demonstrates that morale is interesting.

## Exit condition

A player can identify a reason to protect morale beyond “keep the number high.”

---

# R7 — Campaign Preparation Layer

## Goal

Make the campaign loop answer:

> “What am I changing before the next expedition?”

## Introduce

`game/Campaign/ExpeditionPreparation.cs`

Explicit preparation decisions:

- party selection
- leader
- formation
- gear
- limited consumables
- optional core / specialization choice

## UI implication

`CampaignRenderer` becomes a view of preparation state rather than a passive dashboard.

Do not build a huge inventory screen yet.

## Exit condition

After extracting, the player has at least one meaningful preparation decision before the next run.

---

# R8 — Remove Compatibility Architecture

Only after R0–R7 have stabilized.

## Remove / consolidate

### From GameWorld

- compatibility `Player` as an authoritative gameplay object

### From ExpeditionState

- legacy `Inventory` compatibility property
- duplicate health authority once party state is canonical
- duplicate position authority once PartyController + expedition state are canonical

### From CampaignState

Remove legacy fields only after migration coverage exists.

### From UI

Retire:

- `ui/TopDownWorldRenderer.cs`
- `game/TileRenderer.cs`
- `ui/BattleMenuRenderer.cs`

only when no active path references them.

## Exit condition

There is one clear owner for:

- player position
- party health
- equipment
- carried resources
- campaign resources
- combat status
- floor content

---

# 10. Refactor Dependency Graph

The order matters.

```text
R0 Instrumentation
       ↓
R1 Game Flow Seam
       ↓
R2 Exploration Content
       ↓
R3 Combat Content + Results
       ↓
R4 Expedition Pressure
       ↓
R5 Progression Decisions
       ↓
R6 Morale
       ↓
R7 Campaign Preparation
       ↓
R8 Compatibility Removal
```

Do not start R8 early.

Do not build R5/R6/R7 deeply before R4 proves that the expedition loop is producing useful pressure.

---

# 11. What We Are Explicitly NOT Refactoring Yet

These are deliberately deferred.

## Renderer rewrite

The current rendering architecture is sufficient for the active visual benchmark.

## Generic ECS

No evidence currently requires it.

## Generic room editor

The benchmark needs authored data, not an editor framework.

## External content files

A small C# content-definition layer is sufficient initially.

## Multiplayer architecture

No gameplay need exists.

## Audio engine abstraction

Use placeholders until the gameplay loops are validated.

## Save architecture rewrite

Harden the existing save path only where refactors require it.

---

# 12. Design Experiments the Refactor Must Enable

These are the questions the new architecture should make cheap to test.

## Exploration

- Does an optional dangerous room create more tension than a mandatory room?
- Does incomplete information make exploration more interesting?
- Are route choices meaningful when rewards differ?
- Do events create decisions or merely text?

## Combat

- Is defend worth choosing when health is high?
- Does target selection matter?
- Can enemy behaviors be learned?
- Do character abilities create distinct tactical roles?
- Are statuses strategic or merely damage bonuses?

## Expedition

- Does carried value create attachment?
- Does limited healing make extraction tense or annoying?
- Does deeper reward justify increased risk?
- Does retreat feel like a decision rather than a failure?

## Progression

- Does a new piece of gear change what players do?
- Does party composition change tactics?
- Does leveling create mastery or only bigger numbers?

## Morale

- Does morale influence decisions?
- Is morale memorable?
- Does the player understand why it changed?

## Campaign

- Does returning from an expedition create anticipation for the next run?
- Does preparation meaningfully alter the next expedition?

---

# 13. Acceptance Metrics

This project should not judge the refactor by code cleanliness alone.

## Exploration

A short run should contain several moments where the player chooses between at least two plausible actions.

## Combat

A battle should contain at least one meaningful tactical decision beyond selecting the highest-damage command.

## Expedition

The player should periodically reconsider:

**continue / spend / avoid / extract**

## Progression

A meaningful upgrade should alter behavior, not only numbers.

## Campaign

An extracted run should create a concrete preparation decision.

## Mastery

A returning player should be able to make a better decision because they learned something rather than merely because the character leveled.

---

# 14. Testing Strategy

## Unit tests

Protect deterministic rules for:

- skill definitions
- enemy behaviors
- status application
- reward composition
- risk calculations
- progression
- morale thresholds
- extraction outcomes

## Scenario tests

Use fixed seeds and scripted commands to verify complete loops.

Recommended scenarios:

1. safe extraction
2. greedy deep push
3. failed combat
4. resource-starved expedition
5. morale deterioration
6. gear-driven build change
7. alternate party composition

## Golden-state snapshots

For deterministic scenarios, store compact expected state rather than screenshots:

- depth
- HP
- MP
- morale
- carried value
- defeated enemies
- inventory
- campaign value
- extraction result

Do not snapshot every internal implementation detail.

---

# 15. Commit Plan

## Commit R0
**Add gameplay event instrumentation and deterministic scenario harness**

Visible improvement:
The project can measure and reproduce a full expedition.

## Commit R1
**Extract game-flow application services**

Visible improvement:
No immediate visual change; gameplay changes become isolated and testable.

## Commit R2
**Introduce authored exploration encounter/event definitions**

Visible improvement:
The same floor infrastructure can produce materially different decisions.

## Commit R3
**Separate combat rules from authored combat content**

Visible improvement:
Skills and enemies become tunable content rather than hard-coded behavior.

## Commit R4
**Introduce explicit expedition risk and extraction decisions**

Visible improvement:
The player begins making meaningful push/extract choices.

## Commit R5
**Refactor progression around character and gear definitions**

Visible improvement:
Build choices begin changing play behavior.

## Commit R6
**Give morale explicit gameplay consequences**

Visible improvement:
Morale becomes legible and consequential.

## Commit R7
**Add explicit campaign preparation decisions**

Visible improvement:
Extraction leads naturally into preparation for the next run.

## Commit R8
**Remove obsolete compatibility and renderer paths**

Visible improvement:
No user-facing feature; this is cleanup after the new model proves itself.

---

# 16. Relationship to the Production Plan

The existing production gates remain valid.

This design refactor should run **inside the vertical-slice strategy**, not ahead of it.

### During Gate A–C

Prioritize:

- R0
- R1
- R2
- R3 where required for combat presentation

Do not stop visual production to build every future system.

### During Gate D–E

Prioritize:

- R4
- R5

This is where the actual expedition and combat experience must become repeatable enough to evaluate.

### During Gate F–H

Prioritize:

- R6
- R7
- R8

Only after the loop demonstrates that these systems matter.

---

# 17. The New Refactor Rule

Every proposed refactor must answer three questions:

### 1. What player decision does this enable us to design?

### 2. What current design problem does the existing architecture make difficult to test?

### 3. What observable gameplay evidence will tell us the refactor was worthwhile?

If those questions cannot be answered, the refactor is deferred.

---

# 18. Immediate Next Step

The next engineering task is **R0: gameplay instrumentation + deterministic scenario harness**.

Not a renderer rewrite.

Not a generic architecture pass.

Not more content.

We first need the ability to observe the current game as a system:

**what the player did → what decision was available → what changed → what was gained → what risk remained → why the run ended.**

That evidence becomes the input to every subsequent design change.
