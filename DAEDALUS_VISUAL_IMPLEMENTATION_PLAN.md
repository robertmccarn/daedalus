# DAEDALUS — Visual Production Technical Implementation Plan

> Execution plan for the next production phase.
>
> **Priority:** visual quality over new gameplay systems.
>
> **Current baseline:** `develop` at `f7bc0e882bcd7adb363683ae0161017ac368b12c`.
>
> This document turns the existing Gate A–C goals into concrete implementation work. It is intentionally narrower than the overall production roadmap: the immediate objective is to make one Ruined Depths benchmark room look and feel like a finished Daedalus scene before expanding content.

---

## 1. Immediate Objective

Produce one stable **Ruined Depths Benchmark Room** that demonstrates the final visual language of Daedalus.

The benchmark must visibly communicate:

- ancient ruined architecture
- layered elevation
- a real bridge over a deep abyss
- a readable four-person party
- a major supernatural landmark
- chest / terminal / enemy points of interest
- foreground occlusion
- warm vs. cool localized lighting
- ambient motion
- authored HUD composition
- readable interaction state

The result should read as an **authored game scene**, not a procedural tilemap with effects layered on top.

---

# 2. Scope of This Phase

## In scope

1. Exploration viewport/layout lock
2. Generalized visual depth ordering
3. Benchmark-room environment art pass
4. Authored abyss and vertical composition
5. Lighting / atmosphere pass
6. Character sprite/presentation pass
7. Exploration HUD polish
8. Local visual feedback polish
9. Local screenshot/manual validation workflow

## Explicitly out of scope

Do not expand:

- additional biomes
- large new gameplay systems
- large roster expansion
- deep progression mechanics
- save architecture
- content authoring frameworks beyond what the benchmark room requires
- speculative renderer rewrites
- broad refactors with no visible result

The benchmark room is the deliverable.

---

# 3. Technical Constraints

The current implementation is a C# WinForms game using `System.Drawing`.

Preserve these constraints during this phase:

- Logical grid remains authoritative for movement, collision, encounters, and LOS.
- Visual geometry may be non-grid-aligned.
- Existing render-pass architecture remains the foundation.
- Existing animation clock remains the common timing source.
- Existing party, world, feedback, visibility, and battle state remain authoritative.
- New rendering code must be deterministic where possible so benchmark scenes can be reproduced.
- Avoid adding a new graphics engine or dependency solely to improve presentation.
- Any abstraction introduced must support an immediate visible feature.

---

# 4. Current Visual Problems to Solve

The current renderer already contains useful foundations:

- backdrop layer
- terrain rendering
- structure rendering
- entity rendering
- foreground layer
- effects / atmosphere
- animated abyss
- animated landmark
- animated water / stairs / props
- procedural party silhouettes

The remaining visual problem is primarily **fidelity and composition**, not missing renderer classes.

The benchmark still needs:

- stronger architectural shapes
- larger visual landmarks
- less obvious tile repetition
- stronger vertical separation
- more convincing abyss depth
- more readable character silhouettes
- stronger lighting hierarchy
- more deliberate foreground occlusion
- more authored HUD composition

---

# 5. Work Package A0 — Lock the Presentation Frame

## Goal

Stop individual renderers from making independent assumptions about viewport dimensions and HUD placement.

### Current issue

Several renderers contain hard-coded drawing regions such as the exploration backdrop dimensions and full-screen overlay dimensions. This makes composition fragile as the scene becomes denser.

### Implementation

Introduce a small presentation-layout value object, for example:

`ui/rendering/ViewportLayout.cs`

Responsibilities:

- total client size
- exploration viewport rectangle
- HUD rectangle
- minimap rectangle
- objective/message rectangles
- bottom safe area
- camera/world origin
- intended tile size

Do not make this a generalized UI framework.

### Update

Pass the layout through the existing exploration render context.

Replace renderer-local constants with layout values.

### Acceptance

- No exploration HUD clipping at the intended development window size.
- World composition remains stable when the same benchmark room is redrawn.
- Minimap/objective/message areas have fixed safe margins.

---

# 6. Work Package A1 — Generalized Visual Depth

## Goal

Make elevation and occlusion visually dependable.

### Target ordering

Every visible world item should conceptually resolve to:

`RenderPass + Elevation + WorldY + StableOrder`

### Implementation

Add a lightweight render-depth model to the presentation layer.

Suggested structure:

`ui/rendering/RenderDepth.cs`

Properties:

- `Pass`
- `Elevation`
- `WorldY`
- `StableOrder`

Add a helper that produces a sortable depth key.

Do **not** turn every renderer into an entity-component hierarchy.

### Apply to

- floor features
- bridge
- pillars
- rubble
- doorway
- terminal
- chest
- characters
- enemies
- foreground blockers

### Special behavior

Characters should be able to appear:

- behind a raised wall edge
- on top of a bridge
- below a foreground blocker
- in front of lower-level abyss architecture

### Acceptance

The benchmark scene has at least three visually distinct depth planes and no obvious draw-order errors.

---

# 7. Work Package A2 — Benchmark Room Composition

## Goal

Treat the reference room as a deliberately composed scene rather than a generated room with decorative extras.

### Scene layout

Create one canonical benchmark layout containing:

- entry position
- main stone floor
- elevated bridge route
- abyss void
- far architecture
- landmark / focal point
- terminal
- chest
- encounter position
- foreground frame
- exit / continuation path

The player should be guided visually from:

**entry → bridge → landmark → interaction → threat → deeper route**

### Implementation

Keep the logical dungeon grid.

Move benchmark-specific presentation data into a clearly isolated authored definition instead of scattering coordinates through the generator.

Preferred minimum structure:

`game/World/BenchmarkRoomDefinition.cs`

containing authored:

- features
- props
- encounter marker
- extraction marker
- backdrop anchors
- elevation overrides where required

Do not build a generic room editor yet.

### Acceptance

The same benchmark room can be reproduced exactly from the same floor/seed and authored definition.

---

# 8. Work Package A3 — Environment Art Pass

## Goal

Replace the strongest remaining prototype signals.

### Floor

Implement several visually distinct stone modules:

- large slab
- fractured slab
- worn slab
- edge slab
- rubble-covered slab

Use deterministic selection.

Important: avoid a checkerboard-like alternation.

### Walls

Create distinct visual pieces for:

- intact wall
- broken wall
- wall cap
- recessed wall
- pillar base
- pillar body
- collapsed section

Wall pieces should have:

- top plane
- front face
- darker lower plane
- masonry seams
- selective cracks
- edge wear

### Bridge

Upgrade the current bridge from a flat rectangle to:

- side faces
- supports
- damaged edge
- broken section
- cast shadow into abyss
- subtle height offset

### Large props

Build visually prominent authored versions of:

- collapsed arch
- monumental pillar
- ancient doorway
- rubble bank
- terminal
- chest
- resonance landmark

### Acceptance

At normal gameplay scale, the floor/walls no longer dominate the scene as a visible regular grid.

---

# 9. Work Package A4 — Abyss Depth Pass

## Goal

The abyss must communicate actual vertical space.

### Layer model

Draw the abyss as multiple depth layers:

1. rim / near edge
2. near darkness
3. mid-depth architectural silhouettes
4. distant ruins
5. teal atmospheric haze
6. drifting particles
7. occasional falling debris

### Technical approach

Remain inside `System.Drawing`.

Use layered translucent shapes rather than one large black fill.

Each layer receives:

- deterministic position
- different opacity
- different movement rate
- different scale

Movement must use `AnimationClock`.

### Depth cues

Add:

- rim lighting
- downward fading silhouettes
- subtle haze
- reduced contrast with depth
- occasional falling mote/debris motion

### Acceptance

A screenshot of the room clearly communicates that the bridge crosses a significant drop.

---

# 10. Work Package A5 — Foreground Occlusion

## Goal

Use foreground geometry to create cinematic depth.

### Add

- hanging masonry
- broken wall edge
- large foreground rubble
- dark architectural framing
- occasional dangling debris

### Rules

Foreground objects may overlap the party but must not hide interaction prompts or critical UI.

Foreground should be:

- sparse
- asymmetrical
- large enough to establish scale
- darker than midground

### Animation

Use subtle sway only on appropriate hanging objects.

Avoid constant large movement.

### Acceptance

At least one benchmark composition has a clear foreground layer that partially overlaps the world without obstructing navigation.

---

# 11. Work Package A6 — Lighting Hierarchy

## Goal

Move from “colored effects” to deliberate scene lighting.

### Lighting hierarchy

1. Base environment darkness
2. Warm localized practical light
3. Cool teal supernatural light
4. Character readability light
5. Hazard accents

### Sources

Support spatial light sources for:

- player / party
- terminal
- landmark
- warm torch/fire source
- abyss resonance
- enemy danger

### Technical approach

Keep the current compositing strategy.

Create a reusable local-light helper rather than scattering ellipse drawing logic across renderers.

Suggested:

`ui/rendering/LocalLightRenderer.cs`

Inputs:

- center
- radius
- color
- intensity
- pulse
- falloff

Do not implement a full dynamic lighting engine.

### Acceptance

The focal landmark is visually important without making the entire room uniformly teal.

---

# 12. Work Package A7 — Atmospheric Pass

## Goal

Make the scene feel inhabited by air, dust, and supernatural energy.

### Effects

Use existing effect infrastructure for:

- dust
- mist
- motes
- embers
- resonance wisps
- abyss particles
- subtle environmental drift

### Rules

Effects should be anchored to world locations when they communicate location.

Global effects may exist only at low intensity.

Do not make every object emit particles.

### Layering

Atmosphere must respect world depth:

- abyss effects behind foreground
- landmark effects around landmark
- dust in midground
- foreground mist in front of selected geometry

### Acceptance

The scene still reads clearly when all effects are temporarily disabled, and effects add atmosphere rather than masking weak geometry.

---

# 13. Work Package B0 — Character Asset Pipeline

## Goal

Move the party from procedural placeholder silhouettes toward authored visual assets.

### Folder structure

Use a simple asset organization:

`assets/characters/arden/`
`assets/characters/lyra/`
`assets/characters/marek/`
`assets/characters/sera/`

Within each:

- idle
- walk
- action
- portrait

Start with only what the benchmark requires.

### Data definition

Create a minimal presentation definition containing:

- character ID
- asset path(s)
- source rectangle(s)
- idle frame count
- walk frame count
- frame duration
- direction support
- draw offset
- scale

Do not introduce a full animation framework.

---

# 14. Work Package B1 — Exploration Character Presentation

## Goal

Make the party readable at normal exploration scale.

### Required visual hierarchy

1. silhouette
2. weapon / role cue
3. color/accent
4. motion
5. directional readability

### Animation

Implement:

- idle breathing/sway
- directional walk
- movement bob
- leader emphasis
- minor equipment motion

The existing movement interpolation should remain.

### Acceptance

All four party members can be identified without reading the party HUD.

---

# 15. Work Package B2 — Character Action Readability

## Goal

Carry the same visual language into combat.

### Minimal action set

Do not build every animation yet.

Required:

- attack
- hit
- skill
- defeat

Use timed presentation events already established in the battle renderer.

### Acceptance

A battle screenshot contains readable character silhouettes and an obvious active-action state.

---

# 16. Work Package C0 — HUD Visual Pass

## Goal

Make the HUD feel authored rather than diagnostic.

### Exploration HUD

Refine:

- objective panel
- party status
- interaction cue
- message feed
- minimap
- extraction indicator

### Rules

- reduce unnecessary borders
- establish consistent type hierarchy
- use the established Ruined Depths palette
- reserve bright colors for actionable states
- keep lower-screen controls inside the safe area

### Acceptance

HUD supports the scene instead of competing with it.

---

# 17. Work Package C1 — Interaction Language

## Goal

Create a consistent visual language for interactable objects.

### States

Every key interactable should communicate:

- available
- nearby
- activated
- completed
- unavailable

### Targets

- chest
- terminal
- landmark
- extraction node

### Visual cues

Use combinations of:

- ring
- glow
- small icon
- prompt
- animation state

Avoid relying on a single glow for every object.

### Acceptance

A player can distinguish a chest, terminal, and landmark at a glance.

---

# 18. Work Package C2 — Exploration Feedback

Extend existing feedback effects so important events have short, distinct signatures.

### Required signatures

| Event | Presentation |
|---|---|
| Discovery | expanding pulse + rays |
| Loot | upward sparks + chest action |
| Heal | warm radial burst |
| Damage | impact flash + localized shake |
| Danger | short warning pulse |
| Landmark | teal resonance pulse |
| Extraction | expanding confirmation ring |
| Victory | restrained burst |

Do not stack multiple large effects for one event.

---

# 19. Work Package C3 — Visual Benchmark Camera States

Create a few fixed benchmark positions for validation:

1. Entry composition
2. Bridge + abyss composition
3. Landmark composition
4. Interaction composition
5. Combat transition composition

The room does not need a free camera.

These positions provide repeatable visual checks while iterating.

---

# 20. Work Package D0 — Manual Visual Validation

CI can prove that rendering code compiles and tests pass.

CI cannot prove that the room looks correct.

Create a repeatable local validation procedure:

1. Launch application.
2. Use F1 dev menu.
3. Jump to benchmark floor.
4. Walk to each benchmark position.
5. Capture a screenshot.
6. Compare against the previous iteration.
7. Check:
   - depth
   - occlusion
   - lighting
   - character scale
   - HUD clipping
   - interaction readability
   - visual hierarchy

Do not treat unit-test success as visual acceptance.

---

# 21. Suggested Implementation Order

The implementation order should follow visible dependency rather than architectural purity.

### Commit 1 — Presentation frame

- `ViewportLayout`
- HUD safe area
- central viewport geometry

**Exit:** composition stops shifting.

### Commit 2 — Depth ordering

- `RenderDepth`
- elevation/Y/stable ordering
- benchmark overlap fixes

**Exit:** layered world reads correctly.

### Commit 3 — Environment modules

- authored stone variation
- walls
- bridge
- large props

**Exit:** room stops reading as a flat tilemap.

### Commit 4 — Abyss + foreground

- multi-layer abyss
- architectural depth
- foreground blockers
- shadows

**Exit:** bridge/abyss composition becomes the visual focal point.

### Commit 5 — Lighting + atmosphere

- local lights
- landmark lighting
- warm practical light
- dust/mist/particles

**Exit:** scene has a deliberate mood.

### Commit 6 — Character assets

- four core character definitions
- idle/walk presentation
- authored silhouettes/sprites

**Exit:** party is readable at gameplay scale.

### Commit 7 — HUD + interaction

- objective/message polish
- prompt states
- minimap polish
- interaction language

**Exit:** UI belongs to the same visual language.

### Commit 8 — Benchmark polish

- composition tuning
- animation timing
- effect restraint
- final benchmark pass

**Exit:** Gate A/B/C visual acceptance.

---

# 22. Testing Strategy

## Automated

Maintain existing test coverage.

Add tests only where the new visual infrastructure has deterministic behavior worth protecting.

Useful tests:

- depth ordering is stable
- benchmark definition produces expected feature coordinates
- asset definitions reject missing required fields
- animation frame selection is deterministic
- layout rectangles do not overlap required HUD regions

Do not write tests for pixel-perfect appearance.

## Manual

Required for each visual commit:

- fresh application launch
- F1 benchmark jump
- benchmark room walk-through
- interaction with terminal/chest
- enemy encounter
- battle transition
- extraction transition

---

# 23. Performance Constraints

The current renderer is immediate-mode `System.Drawing`.

Avoid introducing per-frame allocation hotspots.

### Rules

- Reuse cached fonts where possible.
- Keep particle counts bounded.
- Avoid large numbers of expensive gradients.
- Keep translucent compositing localized.
- Do not redraw off-screen world geometry unnecessarily.
- Keep animation calculations deterministic and cheap.
- Prefer a small number of reusable primitives over thousands of independent shapes.

The target is smooth presentation at the existing game window size, not a general-purpose renderer.

---

# 24. Asset Strategy

The visual pipeline should evolve in two stages.

## Stage 1 — Authored procedural primitives

Use the existing `System.Drawing` system to improve:

- geometry
- composition
- depth
- lighting
- animation

This establishes the final visual language.

## Stage 2 — Sprite replacement

Once the benchmark composition is locked:

Replace the most visually important primitives with authored raster assets.

Priority:

1. four core characters
2. landmark
3. bridge
4. major wall/arch pieces
5. terminal
6. chest
7. enemies
8. secondary props

This prevents asset creation from becoming the bottleneck before we know what the final composition requires.

---

# 25. Acceptance Checklist — Visual Benchmark

The benchmark is not complete until all of these are true.

## Environment

- [ ] Floor reads as irregular stone construction.
- [ ] Walls have depth and material variation.
- [ ] Bridge has visible structure and elevation.
- [ ] Abyss contains multiple depth cues.
- [ ] Large architecture creates scale.
- [ ] Foreground geometry creates depth.
- [ ] Landmark is visually dominant.

## Characters

- [ ] Arden is readable.
- [ ] Lyra is readable.
- [ ] Marek is readable.
- [ ] Sera is readable.
- [ ] Party movement has readable motion.
- [ ] Leader state is visually obvious.

## Lighting

- [ ] Environment has a dark baseline.
- [ ] Warm light is localized.
- [ ] Teal energy is localized.
- [ ] Landmark light has hierarchy.
- [ ] Hazard light is selective.

## Atmosphere

- [ ] Dust/mist are subtle.
- [ ] Abyss particles have depth.
- [ ] Landmark wisps move.
- [ ] Foreground motion is restrained.

## UI

- [ ] No HUD clipping.
- [ ] Objective is readable.
- [ ] Interaction cue is readable.
- [ ] Minimap is readable.
- [ ] Message presentation is deliberate.
- [ ] UI palette matches the biome.

## Scene composition

- [ ] Entry view is readable.
- [ ] Bridge view is dramatic.
- [ ] Landmark view is dramatic.
- [ ] Combat view remains coherent.
- [ ] The room works without debug-only knowledge.

---

# 26. Exit Criteria for the Next Phase

Do not move on to broad content production until:

1. The benchmark room passes the checklist above.
2. The four core characters are visually readable.
3. Exploration and combat presentation share the same visual language.
4. The environment no longer primarily reads as a procedural tilemap.
5. The bridge/abyss composition demonstrates convincing depth.
6. HUD is stable and non-clipping.
7. A complete short exploration path can be played through the benchmark room using the normal game flow.

At that point, the project can move from **visual benchmark construction** to **complete exploration slice production**.

---

# 27. Guiding Rule for Every Commit

Before merging a visual commit, ask:

> **What is visibly better when I launch the game?**

If the answer is only “the architecture is cleaner,” it is not the next task.

If the answer is “the room has more depth, stronger silhouettes, better lighting, clearer interaction, or more convincing materials,” it belongs in this phase.
