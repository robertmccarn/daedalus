# DAEDALUS — Unified Implementation Plan

> **Single source of truth for production, game design, refactoring, visual development, and validation.**
>
> This document consolidates the useful material from the former implementation, visual-production, fun-analysis, design-refactor, roadmap, and earlier Systemic architecture plans.
>
> The previous documents are intentionally retired after this consolidation. New work should be planned here rather than in a second roadmap.

---

# 1. Product Direction

Daedalus is a dark expedition RPG about repeatedly descending into ancient ruins with a persistent party.

The intended core experience is:

**Prepare → descend → explore → discover → assess risk → fight / avoid / interact → gain value → decide whether to push or extract → return → improve → prepare again**

The game's identity should come from the interaction of four things:

1. **Discovery** — the ruin contains information, landmarks, routes, and surprises.
2. **Tactical mastery** — combat rewards learning enemy behavior and party interactions.
3. **Expedition tension** — the more value the player carries, the more meaningful the decision to continue becomes.
4. **Persistent ownership** — the party, equipment, knowledge, and campaign state make each expedition matter.

The target emotional question is:

> **“I have gained something valuable. How much farther am I willing to go with what I have?”**

---

# 2. Design Thesis

The project should optimize for **decision quality, not system count**.

A feature is valuable when it creates a meaningful choice, consequence, lesson, or reason to replay.

For every major feature, ask:

> **What does the player decide because this exists?**

Then:

> **What can change because of that decision?**

Then:

> **What does the player learn?**

Then:

> **Why would they want to try again?**

A feature that cannot answer those questions does not automatically deserve additional implementation depth.

---

# 3. Current Baseline

The current build already provides a viable foundation:

### Campaign / persistence
- persistent party roster
- campaign resources, gear, materials, cores, recipes
- campaign flags and depth
- extraction return flow
- synthesis foundation

### Expedition
- active expedition state
- four-person party runtime
- leader and follower movement
- formations
- discovery / fog of war
- minimap
- carried inventory and rewards
- upkeep
- extraction
- floor progression
- morale

### Exploration
- generated dungeon topology
- authored Ruined Depths benchmark room
- chest
- terminal
- enemies
- combat nodes
- extraction node
- landmark
- visual features including bridge, abyss, pillars, rubble, doorway

### Combat
- party-versus-group combat
- interleaved initiative
- target selection
- Attack / Skill / Item / Interact / Defend / Run
- enemy behaviors
- Poisoned / Exposed / Guarded states
- character signature skills
- gear and morale modifiers
- victory / defeat / escape
- battle animation events

### Presentation
- structured exploration render passes
- viewport layout
- shared visual depth ordering
- animated party presentation
- authored environment primitives
- layered abyss
- local lighting
- atmosphere
- HUD
- interaction feedback
- extraction / campaign / game-over presentations

### Developer validation
- F1 dev menu
- deterministic floor jump setup
- level / gear normalization for test scenarios
- automated test suite and CI

The renderer and game-state foundations are no longer the primary blockers.

---

# 4. Current Design Diagnosis

The project currently has **more systems than meaningful decisions**.

## Exploration

The game can currently:

**move → reveal → interact → fight → continue**

But movement is still often transit.

The generated topology is primarily a connected route rather than a set of competing opportunities.

The authored room is visually meaningful, but its gameplay structure remains comparatively predictable.

### Design problem

The player needs reasons to choose:

- a safer route
- a riskier route
- a valuable route
- an information-rich route
- an optional encounter
- an optional reward

rather than simply choosing the shortest path toward the required endpoint.

---

## Combat

Combat is the most mature decision system and has a good skeleton.

However:

- skills are still hard-coded around character identity
- enemy behavior is inferred from names
- status effects are string-based
- battle rules, content, inventory behavior, morale, animation events, and messaging are concentrated in `BattleSystem`
- the current command set is broader than the actual tactical decision space

The intended direction is not “add lots of skills.”

It is:

> **Create more useful relationships between timing, targets, enemy behavior, statuses, party roles, and resources.**

---

## Expedition

This is the most important design gap.

The current game has:

- carried rewards
- HP loss
- healing
- morale
- upkeep
- floor progression
- extraction
- defeat

But extraction is primarily an endpoint.

The player needs a real opportunity to think:

> **“I can leave now with this.”**

versus:

> **“I can risk this haul for something better.”**

That push-your-luck decision should become the game's primary macro-level fun engine.

---

## Progression

The current progression foundation mostly produces larger numbers:

- level
- stats
- HP / MP
- gear power
- morale modifiers

The target is progression that changes **how the player solves problems**, not merely how large the numbers are.

---

## Morale

Morale has unusually strong thematic potential because it responds to:

- victory
- loot
- rare loot
- ally defeat
- retreat
- positive events
- negative events

It currently acts mostly as a numerical modifier.

It should become meaningful only after the expedition-pressure experiment demonstrates where it helps.

---

## Events

Events currently behave mostly as:

**roll → outcome → message**

They should eventually become:

**situation → choice → consequence → information**

But this should not become a giant narrative framework before the expedition loop is proven.

---

# 5. The Fun Model

Daedalus should create four nested loops.

## Moment loop

**Input → action → feedback → state change**

Examples:
- move
- interact
- attack
- defend
- select target

## Encounter loop

**Observe → decide → act → consequence → adapt**

Examples:
- choose whether to engage
- choose target
- sequence abilities
- spend healing
- respond to enemy behavior

## Expedition loop

**Enter → discover → gain value → accumulate risk → reassess → extract / push**

This is the signature loop.

## Campaign loop

**Return → evaluate haul → improve → prepare → descend again**

This is where persistence converts one good expedition into the reason for another.

---

# 6. Primary Design Hypotheses

These are hypotheses, not established player findings.

### H1 — Push-your-luck creates expedition tension

Tension should rise when:

- carried value rises
- resources fall
- danger rises
- future rewards improve

### H2 — Enemy behavior creates mastery

Combat becomes more satisfying when players learn patterns they can exploit.

### H3 — Party identity creates ownership

Characters should solve problems differently rather than merely have different numbers.

### H4 — Meaningful choices create replayability

Different decisions should produce materially different situations and outcomes.

### H5 — Knowledge is progression

An experienced player should be able to make better choices even when given the same party and equipment as a new player.

---

# 7. Production Rules

## Rule 1 — Vertical slice before expansion

One excellent biome and expedition loop comes before broad content.

## Rule 2 — Architecture must not outrun the game

Refactor only when it:

- removes a current blocker
- prevents near-term duplication
- improves active gameplay testability
- or directly enables a visible result

## Rule 3 — Preserve working foundations

Do not rewrite campaign, party, expedition, combat, or renderer foundations just to make them prettier architecturally.

## Rule 4 — Logical grid stays authoritative

Movement, collision, encounters, and LOS remain grid-based.

Presentation can use:

- elevation
- irregular geometry
- offsets
- large structures
- bridges
- foreground
- backdrop layers

## Rule 5 — Content must become explicit before it becomes enormous

Use small C# definitions first.

Do not build a generic content editor or external asset/data platform prematurely.

## Rule 6 — Every meaningful implementation phase needs an exit condition

The question is always:

> **What will be better for the player when this phase is done?**

---

# 8. Unified Production Gates

## Gate 0 — Measurement and Controlled Scenarios

### Purpose

Make the current game measurable before changing major rules.

### Deliver

- gameplay event model
- lightweight recorder
- deterministic scenario runner
- fixed benchmark expedition scenarios
- compact end-state snapshots

### Events to capture

- expedition start / end
- room entry
- discovery
- interaction
- battle start
- command selection
- ability use
- damage
- item use
- enemy defeat
- reward gain
- extraction availability
- extraction choice
- death
- floor transition

### Exit condition

Two builds can be compared through the same scripted scenario without reconstructing the run manually.

---

# 9. Gate A — Visual Benchmark Room

The Ruined Depths benchmark remains the visual reference scene.

It must establish the standard for:

- character scale
- environment material
- elevation
- occlusion
- atmosphere
- lighting
- interaction language
- HUD density
- camera framing

## A1 — Presentation frame

Maintain:

- stable exploration viewport
- HUD rectangle
- minimap region
- objective region
- message region
- safe margins
- stable camera/world scale

### Exit

No intended-size HUD clipping and repeatable composition.

## A2 — Environment language

The scene should read as architecture first and grid second.

Required:

- irregular stone modules
- damaged masonry
- wall caps / faces
- slabs
- rubble
- architectural edges
- substantial props

### Exit

The floor no longer visually reads as a procedural checkerboard.

## A3 — Visual depth

Use:

**Render Pass + Elevation + World Y + Stable Order**

Apply to:

- terrain
- bridge
- pillars
- props
- characters
- enemies
- foreground

### Exit

At least three convincing depth planes with predictable occlusion.

## A4 — Abyss

Use:

1. rim
2. near darkness
3. mid-depth architecture
4. distant ruins
5. teal haze
6. particles
7. falling debris

### Exit

The bridge visually reads as crossing a real drop.

## A5 — Authored landmarks

Required benchmark anchors:

- bridge
- monumental pillar
- doorway / broken arch
- terminal
- chest
- resonance landmark
- rubble banks
- encounter space

### Exit

The room has a clear focal hierarchy and visual route.

## A6 — Lighting

Hierarchy:

1. base darkness
2. warm practical light
3. cool supernatural light
4. character readability
5. danger accents

Use localized lights rather than uniform scene tint.

### Exit

The landmark is important without washing the whole room in teal.

## A7 — Atmosphere

Use restrained:

- dust
- mist
- motes
- embers
- resonance wisps
- abyss particles
- subtle environmental drift

### Exit

Effects enhance strong geometry rather than compensate for weak geometry.

---

# 10. Gate B — Character Presentation

The first four characters are the benchmark set:

- Arden
- Lyra
- Marek
- Sera

## B1 — Character definitions

Each character presentation definition should eventually contain:

- character ID
- asset source
- portrait
- idle frames
- walk frames
- direction
- action states
- scale / draw offset
- accent language

## B2 — Readability

Priority:

1. silhouette
2. role / weapon cue
3. accent
4. motion
5. directional readability

## B3 — Animation

Required:

- idle
- walk
- attack
- hit
- skill
- defeat

Animation should remain restrained and readable.

### Exit condition

A player can identify the four core characters at gameplay distance without consulting the HUD.

---

# 11. Gate C — Exploration Interaction and Atmosphere

The benchmark HUD and interaction language should make the environment readable without feeling like a diagnostic interface.

## Interaction states

Every important interactable should communicate:

- available
- nearby
- activated
- completed
- unavailable

Targets:

- chest
- terminal
- landmark
- extraction

Use combinations of:

- rings
- icons
- prompt
- glow
- animation
- state changes

Avoid making every interaction identical.

## Feedback signatures

Maintain distinct responses for:

- discovery
- loot
- heal
- damage
- poison
- defend
- defeat
- victory
- extraction
- danger

### Exit condition

Players understand what they can interact with and why an important event just happened.

---

# 12. Gate D — Exploration Decision Layer

This is the first major game-design expansion.

## D1 — Move beyond transit

Introduce controlled situations where movement creates a choice:

- safe vs dangerous
- known vs unknown
- low-value vs high-value
- direct vs optional
- recover vs continue

## D2 — Room archetypes

Introduce a small set of authored room purposes:

- traversal
- reward
- ambush
- recovery
- information
- high-risk/high-value
- landmark

Do not produce dozens of room types.

## D3 — Explicit encounters

Replace name-driven placement with explicit encounter definitions.

An encounter should specify:

- enemies
- danger
- reward tier
- special rule
- optionality

### Exit condition

A short exploration route contains multiple plausible choices rather than one required path.

---

# 13. Gate E — Tactical Combat Depth

Refactor combat because the current system is already the strongest basis for mastery.

## E1 — Combat content definitions

Introduce small explicit definitions for:

### Skills

- ID
- name
- resource cost
- power
- targeting rule
- damage type
- status effects
- tags
- presentation key

### Enemies

- ID
- family
- behavior
- base stats
- abilities
- target policy
- reward tier

### Status effects

Replace free-form strings with explicit identifiers.

## E2 — Combat result contract

Combat resolution should produce a deterministic action result describing:

- actor
- target(s)
- damage
- resources
- statuses
- defeat
- follow-up effects

Presentation should consume this result.

## E3 — Remove dead decisions

The command list should only contain commands that create meaningful gameplay choices.

Especially review:

- Interact
- Defend
- Item
- Run

Do not delete options blindly; strengthen or remove them based on the experiment results.

## E4 — Enemy behavior

Enemy behaviors must become authored and learnable.

The goal is for players to develop:

> “I know what this enemy is going to try to do.”

### Exit condition

A player who understands the enemy composition can make better tactical decisions than a player seeing it for the first time.

---

# 14. Gate F — Expedition Risk and Push-Your-Luck

This is the highest-priority design experiment after measurement.

## F1 — Explicit expedition risk

Introduce a dedicated risk model containing, at minimum:

- current floor
- carried value
- party condition
- available recovery
- morale pressure
- upkeep / time pressure
- known danger
- extraction availability

## F2 — Real extraction choice

The player must be able to recognize:

**what is currently safe to bank**

versus

**what is still available by continuing**

The implementation should allow optional continuation rather than making extraction only an endpoint.

## F3 — Reward scaling

Deeper or riskier opportunities should provide qualitatively better reasons to continue.

Avoid pure inflation.

## F4 — Recovery

Recovery must create tradeoffs.

Healing should cost something meaningful in the expedition context:

- time
- resources
- opportunity
- another action
- location access

## F5 — Risk readability

The player should understand enough of the current situation to make an informed gamble.

### Exit condition

Players can explain why they extracted in one run and pushed deeper in another.

---

# 15. Gate G — Progression and Build Identity

Do not deepen progression until the expedition and combat loops are working.

## G1 — Character definitions

Separate authored character identity from runtime state.

Definitions should cover:

- role
- growth
- base stats
- skills
- tags

## G2 — Gear definitions

Separate:

- gear identity
- slot
- base power
- effects

from ownership and equipped state.

## G3 — Meaningful build differences

A good upgrade changes:

**what the player does**

not merely:

**how large a number appears**

Examples of the intended shape:

- stronger burst vs resource efficiency
- durability vs output
- setup vs immediate damage
- safety vs speed

These are design directions, not final numbers.

### Exit condition

Changing party composition or equipment changes the player's preferred solutions to expedition problems.

---

# 16. Gate H — Morale and Campaign Preparation

## H1 — Morale

Move beyond:

**event → number changes → hidden modifier**

toward:

**state → visible consequence → decision**

Potential state bands:

- confident
- steady
- shaken
- broken

Potential consequences can include:

- retreat consequences
- event access
- combat effects
- recovery cost

Do not implement campaign drama until playtesting shows that morale is interesting.

## H2 — Preparation

The campaign should answer:

> **“What am I changing before the next run?”**

Preparation should eventually cover:

- party
- leader
- formation
- equipment
- consumables
- core / specialization choices

### Exit condition

An extracted run naturally creates at least one meaningful preparation decision for the next expedition.

---

# 17. Gate I — Production Biome

Only after the benchmark and decision loops work.

## Ruined Depths production kit

Approximately:

- 10–20 reusable environment modules
- 10+ props
- several wall styles
- bridge variations
- floor variations
- landmark set
- doorway / transition pieces
- backdrop structures
- foreground blockers

## Content

Approximately:

- 3–5 enemy types
- 3–5 event types
- multiple encounter compositions
- several room archetypes
- controlled loot tables
- special landmark
- extraction space

The biome should communicate:

- who built the ruins
- what changed
- what remains active
- why deeper exploration matters

### Exit condition

A player can spend meaningful time in the Ruined Depths without every room feeling interchangeable.

---

# 18. Gate J — Content Expansion

Expand only after the first biome establishes a proven production pattern.

Planned biome families:

1. Ruined Depths
2. Ashen Halls
3. Verdant Below
4. Crystal Wastes

Each biome must have its own:

- architecture
- palette
- lighting profile
- backdrop
- hazards
- enemies
- props
- landmarks
- loot
- events
- narrative fragments
- combat compositions

A new biome cannot be a recolor of the previous one.

---

# 19. Gate K — UX, Save, Accessibility

Complete:

- campaign UI
- party management
- inventory
- equipment
- synthesis
- expedition briefing
- extraction results
- settings

Save work:

- autosave
- manual save
- save slots
- validation
- migration
- recovery

Accessibility:

- text scaling
- contrast
- reduced effects
- reduced flashes
- animation options
- color-independent communication
- input remapping

This gate follows the proven core loop rather than preceding it.

---

# 20. Gate L — Alpha / Beta / Release Candidate

## Alpha

Everything important exists.

Acceptable roughness:

- placeholder assets
- balance issues
- rough transitions
- asset duplication
- edge-case bugs

Do not use Alpha as an excuse for another architectural rewrite.

## Beta

Focus on:

- balance
- pacing
- economy
- difficulty
- usability
- performance
- save reliability
- content consistency
- bug fixing

No new major systems.

## Release Candidate

Feature freeze.

Only:

- bugs
- optimization
- accessibility
- balance
- UX polish
- asset consistency
- save validation
- packaging

---

# 21. Refactor Program

The production gates above determine **when** to refactor.

These are the actual refactor seams.

## R0 — Instrumentation

Add:

- gameplay events
- recorder
- scenario harness
- deterministic snapshots

Purpose:

Make design experiments measurable.

## R1 — Game flow seam

Reduce `GameSession` toward orchestration.

Potential boundaries:

- `GameFlow`
- `ExplorationActions`
- `BattleActions`

Purpose:

Make gameplay changes independently testable.

## R2 — Exploration content seam

Separate:

**topology**

from

**content**

from

**presentation metadata**

Keep the benchmark authored.

Purpose:

Create meaningful exploration variation without rewriting the generator.

## R3 — Combat content seam

Separate:

- skill definitions
- enemy definitions
- status identifiers
- combat action results

from combat state resolution and presentation.

Purpose:

Make tactical experiments cheap.

## R4 — Expedition risk seam

Make risk and extraction explicit.

Purpose:

Make the game's signature push-your-luck loop tunable.

## R5 — Progression seam

Separate authored character / gear definitions from runtime state.

Purpose:

Make build experiments cheap.

## R6 — Morale seam

Give morale explicit effects only after the risk loop establishes a reason for it.

## R7 — Campaign preparation seam

Turn campaign state from a destination into a preparation decision layer.

## R8 — Compatibility retirement

Only after the canonical model survives gameplay iteration.

Remove:

- obsolete player bridge
- duplicated health authority
- duplicated position authority
- legacy inventory compatibility
- obsolete campaign fields

Then retire unused renderers:

- `ui/TopDownWorldRenderer.cs`
- `game/TileRenderer.cs`
- `ui/BattleMenuRenderer.cs`

Only after repository-wide reference checks.

---

# 22. Data Ownership

The architecture should converge toward:

| Layer | Owns |
|---|---|
| Campaign | persistent roster, stash, gear, materials, cores, recipes, flags, depth |
| Expedition | run-specific party, floor, carried value, discoveries, upkeep, risk, extraction state |
| World | transient floor topology, encounters, props, nodes, spatial simulation |
| Combat | current battle state and deterministic action resolution |
| Presentation | render state, animation, effects, HUD, screen composition |
| Application / Flow | high-level commands and state transitions |

The UI never owns gameplay rules.

The renderer never mutates authoritative gameplay state.

---

# 23. Controlled Design Experiments

These should be run against the actual game rather than answered by intuition.

## Experiment 1 — Extraction pressure

Create:

- known reward
- optional dangerous reward
- meaningful loss risk

Measure:

- extract vs push
- decision time
- explanation of choice
- resources remaining
- value carried

## Experiment 2 — Enemy mastery

Create two visibly distinct behavior patterns.

Measure whether players:

- notice
- predict
- exploit
- change their strategy after learning

## Experiment 3 — Party composition

Give players the same encounter with different party compositions.

Measure whether their tactics change.

## Experiment 4 — Build choice

Offer two upgrades with different strategic effects.

Measure whether players can explain the tradeoff.

## Experiment 5 — Knowledge progression

Repeat a scenario with the same mechanical difficulty.

Compare first exposure with later attempts.

The desired result is better decisions from learned understanding, not only greater character power.

---

# 24. Measurement Strategy

## Automated telemetry

Eventually record:

- expedition duration
- turns
- rooms entered
- optional content entered
- battle count
- commands
- targets
- abilities
- items
- damage
- retreats
- extraction timing
- depth
- carried value
- remaining resources
- death
- party configuration
- equipment configuration

## Qualitative observation

After a run, ask:

> **“What was the hardest decision you had to make?”**

Also ask:

> **“What did you learn that you would use next time?”**

These answers should be treated as evidence alongside telemetry.

---

# 25. Scenario Testing

Use fixed seeds and scripted sequences for repeatability.

Required scenarios:

1. safe extraction
2. greedy push
3. failed combat
4. resource-starved run
5. morale deterioration
6. gear-driven build change
7. alternate party composition
8. optional high-value encounter
9. deeper-floor continuation

Each scenario should be able to compare:

- starting state
- actions
- resulting state
- rewards
- risk
- extraction outcome

Do not snapshot implementation details that do not affect player-visible behavior.

---

# 26. Visual Validation

CI proves:

- compilation
- tests
- deterministic logic

It does **not** prove appearance.

For visual work, use the F1 dev menu to establish repeatable inspection points:

1. benchmark entry
2. bridge + abyss
3. landmark
4. interaction
5. combat transition

Check:

- depth
- occlusion
- lighting hierarchy
- character scale
- HUD clipping
- interaction readability
- visual focus

Manual visual validation is a required part of visual acceptance.

---

# 27. Performance Constraints

The current renderer remains immediate-mode `System.Drawing`.

Maintain:

- bounded particle counts
- localized translucent compositing
- cached fonts/resources where practical
- limited gradients
- deterministic low-cost animation
- no unnecessary off-screen redraw

Do not migrate to a GPU renderer until validated gameplay and content requirements justify it.

---

# 28. Asset Strategy

## Stage 1 — Authored procedural presentation

Continue using `System.Drawing` to establish:

- geometry
- material language
- depth
- lighting
- animation
- composition

## Stage 2 — Authored raster replacement

Once the benchmark is compositionally locked, prioritize assets in this order:

1. four core characters
2. landmark
3. bridge
4. major wall / arch pieces
5. terminal
6. chest
7. enemies
8. secondary props

The purpose is to avoid creating large quantities of art before the required composition is understood.

---

# 29. Legacy / Superseded Material

The following concepts from older documents are considered **historical guidance**, not active plans:

- the former Phase 1–9 architecture-first roadmap
- generic asset registries
- premature GPU migration
- broad room-editor work
- large external content pipelines
- speculative ECS architecture
- simultaneous production of all four biomes
- static-unit depth as an immediate requirement

Useful principles from the old architecture document are retained here:

- preserve the square logical grid
- separate persistent campaign state from transient expedition state
- keep renderers read-only
- make rewards change decisions
- use vertical slices
- prevent architecture from outrunning gameplay
- use software compositing until a real GPU requirement exists

---

# 30. Immediate Development Order

The project should now move in this order:

### Now

**Gate A visual benchmark completion + R0 measurement**

Continue the visual benchmark because it is the presentation reference, while adding the minimum instrumentation required to evaluate the game loop.

### Next

**R1 GameFlow seam**

Do not fully rewrite `GameSession`; extract only the boundaries required for experiments.

### Then

**R3 combat content seam**

Because combat is already the strongest tactical system.

### Then

**R4 expedition pressure experiment**

Make the push/extract choice real and measurable.

### Then

**R2 exploration content**

Use the risk model to create optional exploration decisions.

### Then

**R5 progression/build identity**

Make characters and gear change player behavior.

### Then

**R6/R7 morale + campaign preparation**

Give the meta loop a concrete reason to repeat.

### Only after the above

**Gate I production biome and broad content expansion.**

---

# 31. Definition of “Fun Enough to Expand”

Do not move into broad content expansion merely because the game contains:

- more rooms
- more enemies
- more gear
- more menus
- more animations

Expansion is justified when a short expedition reliably contains:

- discovery
- a meaningful tactical encounter
- resource pressure
- at least one real risk/reward decision
- a reason to continue
- a reason to extract
- a meaningful post-run preparation choice
- at least one lesson that can improve the next attempt

That is the minimum loop the rest of the game should multiply.

---

# 32. Guiding Question for Every Commit

Before implementing a change:

> **What is visibly or experientially better when the player launches Daedalus?**

For a refactor, the answer must additionally be:

> **What design experiment or gameplay decision does this refactor make easier to build or evaluate?**

If neither answer is clear, the work is not the next priority.
