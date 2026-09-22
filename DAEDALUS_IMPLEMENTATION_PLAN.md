
# DAEDALUS — Production Implementation Plan

> **Authoritative implementation plan.**
>
> This document replaces the previous architecture-first approach with a production plan centered on a finished vertical slice.
>
> The current repository already contains working foundations for campaign state, party state, expedition state, visibility, exploration, combat, extraction, progression systems, and CI. The remaining problem is not primarily missing architecture. It is the gap between those systems and the visual / experiential game represented by the Daedalus concept board.


> **Design-driven refactor overlay:** See [`DAEDALUS_GAME_DESIGN_REFACTOR_PLAN.md`](DAEDALUS_GAME_DESIGN_REFACTOR_PLAN.md) for the gameplay-analysis-derived refactor sequence. Use that plan to decide when architectural changes are justified by player decisions, risk, mastery, feedback, and iteration needs. Do not treat it as a separate production gate.

---

## 1. Product Target

Daedalus is a dark exploration RPG built around repeated expeditions into ancient ruins.

The target experience is:

**Campaign → prepare party → enter ruins → explore → discover → interact → fight → loot → extract → return → improve → descend again**

The visual target supplied for this project establishes the intended presentation:

- dramatic ruined architecture
- strong vertical depth
- bridges over abyssal spaces
- layered stone construction
- warm firelight against cool darkness
- supernatural teal energy
- readable character silhouettes
- environmental storytelling
- tactical combat presentation
- authored UI rather than debug panels
- cohesive biome identity

The implementation plan must therefore optimize for **visible game quality**, not code volume.

---

# 2. Current Baseline

## 2.1 What Already Works

The current develop branch has a viable foundation:

- campaign state and persistent roster
- expedition state
- four-person party runtime
- fifth roster member
- formation support
- exploration movement
- fog / discovery and line-of-sight visibility
- minimap
- interactable props
- exploration events
- extraction flow
- campaign return / expedition restart
- multi-enemy battle state
- party turn handling
- target selection
- attack / skill / item / defend / run commands
- poison and guarded status behavior
- battle victory / defeat / escape states
- extraction rewards
- progression / morale / synthesis systems
- authored ruin feature records
- exploration feedback effects
- CI build/test validation

The last verified commit at the time this plan was rewritten is:

f94f9a2c922fe04dafb6d47a1082dfa312211014

The associated GitHub Actions run completed successfully.

## 2.2 What Is Not Yet Production Quality

The current screenshot exposes the primary gap:

### Environment
The renderer currently communicates a tiled dungeon grid more strongly than an authored ancient ruin.

### Characters
The party is technically distinct but visually too small and procedural to carry character identity.

### Depth
Elevation and foreground behavior exist, but the scene still lacks the layered vertical composition of the target.

### Atmosphere
The current effects provide glow and feedback, but lighting does not yet substantially sculpt the environment.

### UI
The current HUD communicates useful state but still reads partially as a development interface. Some lower-screen instructions can clip at the current window size.

### Combat
The battle state is functional, but the presentation still needs to become a finished tactical scene.

### Assets
The project currently relies heavily on System.Drawing primitives. A production visual target requires an authored asset layer.

---

# 3. Non-Negotiable Production Rules

## Rule 1 — Vertical slice before expansion

Do not build four polished biomes before one biome works.

Do not build every menu before one expedition is excellent.

Do not add systems merely because they may be needed later.

---

## Rule 2 — Architecture must not outrun the game

A refactor is justified only when it:

1. removes a current blocker,
2. prevents near-term duplication,
3. improves testability of something we are actively shipping,
4. or directly improves a visible result.

---

## Rule 3 — Preserve the working domain model

Do not rewrite campaign, party, expedition, combat, or progression foundations simply to make them look architecturally cleaner.

Refactor them incrementally when the vertical slice exposes a real limitation.

---

## Rule 4 — Gameplay grid and presentation geometry remain separate

Collision, movement, encounters, and visibility use the logical grid.

Visual presentation may use:

- elevation
- irregular geometry
- sprite offsets
- large props
- bridges
- foreground occlusion
- decorative geometry
- backdrop layers

Do not force the visual scene to become a literal 1:1 tile map.

---

## Rule 5 — Every major commit must produce a visible improvement

Good:

> Replace the reference-room floor renderer with authored stone modules and demonstrate a readable bridge/abyss composition.

Bad:

> Add another generic renderer abstraction with no change to the game.

---

## Rule 6 — The reference room is the visual benchmark

Before mass content creation, the project must have one room that can serve as the standard for:

- environment art
- character scale
- lighting
- VFX
- UI density
- camera framing
- depth / occlusion
- interaction language

New content is judged against that room.

---

# 4. Implementation Sequence

The project is now organized into these production gates:

1. **Gate A — Visual Benchmark Room**
2. **Gate B — Character Presentation**
3. **Gate C — Atmosphere & Effects**
4. **Gate D — Complete Exploration Slice**
5. **Gate E — Complete Combat Slice**
6. **Gate F — Campaign Loop**
7. **Gate G — Production Biome**
8. **Gate H — Progression Depth**
9. **Gate I — Content Expansion**
10. **Gate J — UX / Save / Accessibility**
11. **Gate K — Alpha / Beta / Release Candidate**

Each gate has a hard exit condition.

---

# 5. Gate A — Visual Benchmark Room

## Goal

Transform the current reference chamber into a scene that visually communicates **Daedalus** immediately.

This is the most important stage of the project.

## A1. Lock the exploration composition

Define a stable presentation target:

- playfield dimensions
- HUD region
- camera framing
- tile/world scale
- party screen footprint
- safe margins
- minimap position
- message / objective areas
- interaction prompt position

The current gameplay viewport and HUD should stop shifting between ad hoc sizes.

### Acceptance

The same room renders consistently across the intended window size without clipping the HUD.

---

## A2. Replace the flat tile appearance

Keep the logical tiles.

Change the presentation.

Implement:

- authored floor modules
- irregular stone seams
- edge variation
- wall caps
- broken edges
- rubble clusters
- damaged masonry
- large stone slabs
- shadowed wall faces
- floor transitions

The floor should stop reading as a repeating chessboard.

### Acceptance

At normal gameplay scale, the room reads as stone architecture first and grid second.

---

## A3. Build true layered depth

Implement a generalized visual depth model:

RenderPass → Elevation → WorldY → StableOrder

The logical grid remains unchanged.

Visual items gain enough information to determine:

- vertical offset
- foreground/background relationship
- bridge level
- occlusion
- draw order

The current render pass structure should be retained, but fixed depth values inside ExplorationRenderer should evolve into real item depth.

### Acceptance

A character can walk behind a wall edge, across a raised bridge, and in front of foreground debris with predictable ordering.

---

## A4. Build the authored abyss

The abyss is a major visual signature.

It should contain:

- near-black void
- lower-level stone silhouettes
- distant architecture
- teal atmospheric glow
- vertical haze
- drifting particles
- depth variation
- broken masonry descending into darkness

The abyss cannot remain a single black fill.

### Acceptance

The viewer can visually understand that the party is above a deep drop.

---

## A5. Build architectural landmarks

Create a small set of large authored scene anchors:

- collapsed arch
- monumental pillar
- broken bridge
- ancient terminal / mechanism
- central resonance landmark
- doorway
- ruined altar / machine
- rubble bank

These should be placed deliberately rather than generated solely from random decoration.

### Acceptance

The room has a recognizable focal point and a reason to move through it.

---

## A6. Establish the visual language

The base Ruined Depths presentation should use:

- cold stone
- near-black negative space
- restrained teal supernatural energy
- warm amber firelight
- muted metal / bone / parchment accents
- danger red as a selective signal

Avoid flooding the scene with saturated effects.

### Acceptance

A screenshot of the room is recognizable as one cohesive art direction.

---

## Gate A Exit Condition

The room screenshot should no longer look like a generic procedural dungeon.

It must visibly demonstrate:

- layered stone
- bridge
- abyss
- elevation
- authored landmark
- foreground occlusion
- four-member party
- chest
- terminal
- enemy
- minimap
- objective / message UI
- readable interaction cue

---

# 6. Gate B — Character Presentation

## Goal

Make Arden, Lyra, Marek, and Sera immediately identifiable during normal play.

## B1. Create a production character visual definition

Replace hardcoded character drawing rules with data-driven presentation.

Each character should define:

- sprite / atlas source
- portrait source
- idle frames
- walk frames
- direction frames
- combat idle
- attack
- hit
- defeat
- skill
- accent / secondary material
- silhouette rules

The gameplay systems continue to use the existing character IDs.

---

## B2. Increase exploration readability

Characters need to occupy enough screen area to read as people rather than markers.

Priorities:

1. silhouette
2. equipment
3. motion
4. role
5. face / detail

Do not solve this by simply enlarging the current primitive shapes indefinitely.

---

## B3. Build a minimal sprite asset pipeline

Add an asset structure suitable for WinForms / System.Drawing, for example:

assets/characters/arden/
assets/characters/lyra/
assets/characters/marek/
assets/characters/sera/

and equivalent folders for environment and UI art.

Update the project file so these assets are copied or loaded reliably in development and published builds.

---

## B4. Character animation

Implement:

- idle breathing / sway
- directional walk
- movement bob
- simple action transitions
- hit reaction
- defeat state

Animation can remain sprite-based and intentionally restrained.

---

## Gate B Exit Condition

A player can identify all four core characters from gameplay distance without reading the HUD.

---

# 7. Gate C — Atmosphere & Effects

## Goal

Make the environment feel alive, ancient, dangerous, and supernatural.

## C1. Lighting model

Within the existing System.Drawing approach, approximate localized lighting through layered compositing rather than introducing a new graphics engine.

Support:

- base darkness
- torch pools
- teal resonance pools
- danger tint
- soft ambient haze
- localized character light
- distance falloff

---

## C2. Environmental effects

Implement reusable effects for:

- dust
- mist
- sparks
- embers
- floating motes
- falling debris
- energy wisps
- abyss particles

Effects should be spatially anchored to world locations.

---

## C3. Gameplay feedback

Every important action needs a readable response:

- discover
- interact
- loot
- heal
- damage
- poison
- defend
- enemy defeat
- victory
- extraction
- danger

Current FeedbackEffect infrastructure should be extended rather than replaced.

---

## C4. Landmark presentation

The existing authored landmark discovery message is a good foundation.

Expand it into a short presentation event:

1. visual pulse
2. audio hook placeholder
3. brief message
4. optional camera emphasis
5. persistent discovered state

Do not turn it into a long cutscene.

---

## Gate C Exit Condition

Walking through the room feels substantially different from moving through a plain tilemap.

---

# 8. Gate D — Complete Exploration Slice

## Goal

Deliver the first genuine 10–20 minute playable experience.

The player loop must be:

**enter → orient → explore → discover → interact → encounter → fight → loot → choose whether to continue → extract**

## D1. Opening sequence

The first minute should communicate:

- where the player is
- who the party is
- the objective
- what extraction means
- what can be interacted with

No developer-only knowledge should be required.

---

## D2. Exploration flow

Add or refine:

- authored room connections
- encounter locations
- chest locations
- terminal interactions
- landmark discovery
- short environmental story beats
- extraction node

---

## D3. Risk / reward

The expedition should ask the player to choose between:

- exploring further
- collecting more resources
- fighting
- extracting early

The exact economy can remain simple at this stage.

---

## D4. Extraction

Extraction should:

- clearly signal the node
- confirm party presence
- summarize rewards
- transition cleanly
- return to campaign state

The current extraction framework should be retained and polished.

---

## Gate D Exit Condition

A new player can complete one short expedition from a fresh launch without debug knowledge.

---

# 9. Gate E — Complete Combat Slice

## Goal

Make tactical combat feel like the same game as exploration.

## E1. Battlefield presentation

Implement:

- distinct battle arena
- party formation placement
- readable enemy silhouettes
- clear turn order
- target indication
- movement / attack range cues when applicable

---

## E2. Character and enemy states

Show:

- HP
- status
- active turn
- attack
- skill
- defense
- hit
- defeat

Do not rely solely on textual log output.

---

## E3. Effects

At minimum:

- melee impact
- ranged / magic impact
- poison
- guard
- damage numbers
- defeat animation
- victory transition

---

## E4. Combat UI

Replace the feel of a debug command list with a deliberate tactical interface:

- command selection
- target selection
- party portraits
- active turn
- status
- concise action feedback

Keyboard controls remain supported.

---

## Gate E Exit Condition

A complete battle can be captured in a screenshot and visually belongs to Daedalus.

---

# 10. Gate F — Campaign Loop

## Goal

Make the expedition matter after the player returns.

The loop becomes:

**prepare → descend → extract → improve → descend again**

## F1. Campaign presentation

Polish:

- roster
- expedition entry
- stash
- materials
- cores
- gear
- run history
- depth

---

## F2. Party management

Add:

- member selection
- leader selection
- formation selection
- basic equipment visibility

Do not overbuild character management yet.

---

## F3. Progression payoff

The player should have a concrete reason to care about:

- gear
- resources
- experience
- morale
- deeper floors

---

## Gate F Exit Condition

The player completes one expedition, returns to campaign, changes something meaningful, and starts another expedition.

---

# 11. Gate G — First Production Biome

## Goal

Build **The Ruined Depths** as the first complete biome.

Do not build all four biomes simultaneously.

## Environment target

Create a reusable production kit containing approximately:

- 10–20 environment modules
- 10+ props
- multiple wall styles
- bridge variations
- floor variations
- landmark set
- doorway / transition pieces
- background structures
- foreground blockers

## Content target

Create approximately:

- 3–5 enemy types
- 3–5 event types
- multiple encounter compositions
- controlled loot tables
- several room archetypes
- at least one special landmark
- one extraction space

## Story target

Use visual and short textual clues to communicate:

- who built the ruins
- what changed
- what remains active
- why the player should care

---

## Gate G Exit Condition

A player can spend meaningful time in The Ruined Depths without feeling that every room is the same template.

---

# 12. Gate H — Progression Depth

Only after the first production biome works should the systems receive additional depth.

## H1. Roster

Expand toward 5–8 production characters.

Voss becomes the next fully authored character after the initial four.

---

## H2. Gear

Add:

- weapons
- armor
- accessories
- rarity
- conditional effects

---

## H3. Cores

Add:

- core families
- tiers
- synthesis choices
- visible impact on builds

---

## H4. Morale

Morale must eventually do something players can feel.

Possible directions can be evaluated during playtesting:

- temporary stat impact
- event outcomes
- retreat risk
- dialogue / campaign consequences

The exact mechanic should be tuned through play, not locked prematurely.

---

## H5. Campaign consequence layer

Eventually support:

- deeper destinations
- campaign state changes
- alignment consequences
- meaningful preparation choices

---

## Gate H Exit Condition

Repeated expeditions create real build and preparation decisions.

---

# 13. Gate I — Content Expansion

Expand the production pattern from The Ruined Depths.

Biomes:

1. The Ruined Depths
2. The Ashen Halls
3. The Verdant Below
4. The Crystal Wastes

Each biome receives:

- architecture
- palette
- lighting profile
- backdrop language
- hazards
- enemies
- props
- landmarks
- loot
- events
- narrative fragments
- unique combat compositions

The new biome is not allowed to be a recolor-only implementation.

---

## Gate I Exit Condition

All planned biomes are recognizably different while still belonging to the same game.

---

# 14. Gate J — UX, Save, Accessibility

Only after the core game is proven.

## J1. UI

Complete:

- campaign menu
- party management
- inventory
- equipment
- synthesis
- expedition briefing
- extraction results
- settings

---

## J2. Save reliability

Implement:

- autosave
- manual save
- save slots
- validation
- migration strategy
- failure recovery

---

## J3. Accessibility

Support, as practical for the game:

- text scaling
- readable contrast
- reduced screen effects
- reduced flashes
- animation options
- color-independent state communication
- input remapping

---

## Gate J Exit Condition

The complete core loop can be played comfortably without relying on prototype-only UI.

---

# 15. Gate K — Alpha / Beta / Release Candidate

## Alpha

Everything important exists.

Known problems may include:

- placeholder art
- balance issues
- rough transitions
- duplicate assets
- ugly edge cases

No architectural rewrite.

---

## Beta

Focus entirely on:

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

---

## Release Candidate

Feature freeze.

Only:

- bugs
- optimization
- accessibility
- balance
- asset consistency
- UX polish
- save validation
- release packaging

---

# 16. Refactor Queue

These are legitimate engineering improvements, but they should be scheduled around production needs.

## R1. Generalized exploration render depth

Current state:

- ExplorationRenderer has explicit render passes
- individual renderers perform some Y ordering
- depth values are mostly fixed at renderer level

Target:

RenderPass + Elevation + WorldY + StableOrder

Trigger:

**Do this during Gate A** because layered environment presentation needs it.

---

## R2. Data-driven visual assets

Current state:

- character visuals are hardcoded in EntityRenderer
- world visuals rely heavily on primitives

Target:

- character visual definitions
- sprite sheets / atlases
- reusable world assets
- prop definitions

Trigger:

**Do this during Gates A–B.**

---

## R3. Room definition data

Current state:

- authored features are represented as records
- reference-room construction is still embedded in world generation

Target:

A data-driven room definition capable of specifying:

- visual features
- props
- elevation
- landmarks
- encounter nodes
- extraction node
- backdrop composition

Trigger:

**Do this when Gate A stops being maintainable with the current hardcoded reference room.**

---

## R4. Compatibility cleanup

Current state includes compatibility paths such as the older player bridge and mixed state/data responsibilities.

Do not remove them simply for cleanliness.

Remove or consolidate them when:

- the new party/exploration state is authoritative,
- existing tests no longer require the compatibility layer,
- and the removal reduces active complexity.

---

## R5. Legacy renderer retirement

Known legacy files include:

- ui/TopDownWorldRenderer.cs
- game/TileRenderer.cs
- ui/BattleMenuRenderer.cs

Retire them only after repository-wide reference checks confirm they are unused.

Do not delete them speculatively.

---

## R6. Expedition determinism

The current expedition restart path can use a random seed.

Add explicit seed injection so tests and reproducible content can say:

- seed
- floor
- room layout
- encounter placement

This becomes important once the authored / procedural boundary is formalized.

---

## R7. Reward quantity correctness

Review extraction summary calculations so stacked resources report quantities rather than only collection counts.

Do this before economy balancing.

---

# 17. Code / Asset Organization Target

The project should converge toward:

game/
- state
- systems
- party
- world
- content
- presentation-neutral models

ui/
- exploration
- battle
- campaign
- rendering
- input

assets/
- characters
- environments
- props
- effects
- portraits
- UI

The domain should never depend on the UI renderer to define gameplay truth.

---

# 18. Test Strategy

Tests remain important, but they change emphasis.

## Keep unit coverage for:

- state transitions
- combat rules
- visibility
- movement
- formation
- rewards
- extraction
- progression
- synthesis
- save/load

## Add integration-style checks for:

- complete expedition loop
- campaign return
- battle victory
- discovery flow
- landmark interaction
- extraction flow

## Add visual acceptance checks manually for:

- reference room
- character readability
- combat screen
- campaign screen
- biome identity

Automated tests protect rules.

Human visual review protects the actual game.

---

# 19. Commit Strategy

Development should proceed in small, green commits.

Preferred pattern:

1. one visible feature
2. tests updated
3. local compile/test
4. commit
5. GitHub Actions green
6. proceed

Avoid giant commits that combine:

- new systems
- visual rewrites
- asset loading
- unrelated cleanup
- speculative architecture

A commit should answer:

> **What changed for the player?**

---

# 20. Immediate Implementation Backlog

The next implementation sequence is intentionally narrow.

### Sprint 1 — Reference Room Reconstruction

1. Fix exploration viewport / HUD clipping.
2. Lock room camera and playfield composition.
3. Generalize render depth ordering.
4. Replace checkerboard-like floor presentation.
5. Add authored wall / floor / rubble variation.
6. Build a visually deep abyss.
7. Add a major architectural focal point.
8. Add real foreground occlusion.
9. Tune the teal resonance landmark.

### Sprint 2 — Character Readability

10. Establish asset loading infrastructure.
11. Replace primitive character bodies with authored sprite presentation.
12. Enlarge exploration character footprint appropriately.
13. Add directional idle / walk animation.
14. Add four production portraits.
15. Add readable leader / selection states.

### Sprint 3 — Atmosphere

16. Add localized torch lighting.
17. Add localized teal environmental lighting.
18. Add mist / dust / particles.
19. Add discovery and landmark presentation.
20. Add combat impact effects.

### Sprint 4 — Exploration Slice

21. Author the first 10–20 minute route.
22. Connect chest, terminal, landmark, enemy, and extraction.
23. Add concise introductory messaging.
24. Validate the full loop with no developer controls.

### Sprint 5 — Combat Slice

25. Redesign battlefield composition.
26. Add party/enemy presentation.
27. Add turn and target readability.
28. Add action effects.
29. Add victory / defeat transitions.

Only after Sprints 1–5 pass their visual and gameplay gates should production-biome expansion begin.

---

# 21. Definition of Done

The project is not done because:

- all classes compile,
- every system has a test,
- every file in the original architecture exists,
- or every planned mechanic has a stub.

A milestone is done when a player can **see and use the intended experience**.

The finished Daedalus standard is:

> **A player enters an ancient ruin with four recognizable characters, sees a dramatic layered environment, understands where they are and what they are doing, explores through meaningful spaces, discovers secrets, fights tactically, collects resources, decides when to extract, returns to a persistent campaign, improves the expedition party, and wants to descend again.**

Everything else in the implementation plan exists to make that loop real, readable, and repeatable.

---

# 22. Current Priority

**Do not start another broad systems phase.**

The immediate target is:

> **Build the first spectacular Ruined Depths room.**

That room becomes the production benchmark for:

- environment art
- character art
- camera
- depth
- lighting
- effects
- interaction
- UI
- combat presentation

Once that benchmark is convincing, the rest of the project becomes an exercise in extending a proven visual and gameplay language rather than inventing the game piecemeal.


---

# 23. Sprint 1 Status — Reference Room Reconstruction

## Completed in the first implementation batch

The current branch now includes the first visual reconstruction pass:

- removed the strongest alternating checkerboard floor treatment
- added deterministic stone-slab and fracture variation
- increased masonry and wall-surface variation
- added deeper abyss silhouettes and internal atmospheric depth cues
- made ruin features sort by elevation before world Y
- made static units and interactive props sort by elevation before world Y
- made party members and enemies render using their logical tile elevation
- tightened the exploration HUD footer so controls remain inside the intended panel

This is an incremental visual pass, not the completion of the benchmark room.

## Still required for Gate A

- authored large environment assets
- stronger vertical architecture
- a more substantial landmark focal point
- true foreground obstruction/occlusion
- localized environmental lighting
- stronger abyss depth and parallax
- production character art
- final camera composition

The next work should continue from this state rather than restarting the renderer.
