# DAEDALUS — Revised Completion Roadmap

The original 9-phase plan was a useful systems/architecture roadmap, but it underestimated the work required to turn those systems into the visual and experiential game represented by the concept art.

The remaining project is therefore organized around **vertical slices and production gates**, not primarily around technical abstractions.

## The New Philosophy

The old model:

> Build the systems → build the renderer → add effects → add content → polish.

The production model:

> **Build one small piece of the actual finished game → make it look and feel right → use it as the template for everything else.**

The target includes layered environments, strong environmental composition, elevation, atmospheric depth, lighting, emissive effects, four-character presentation, authored character identities, tactical combat presentation, polished HUD, multiple biome identities, environmental storytelling, particles/feedback, cohesive UI, and substantial content.

## Current Position

### Completed

- Phase 1 — Foundation
- Phase 2 — State & Persistence
- Phase 3 — Expedition Systems
- Phase 4 — Party Architecture

The current architecture remains:

Campaign → Roster / Progression → Party Selection → Expedition → Party / World / Economy → Game Session

The architecture is sound enough to support production. The priority is now closing the presentation/gameplay gap.

# Phase 5 — Visual Foundation

## Goal

Turn the functional renderer into the beginning of the actual **Daedalus visual language**.

### 5.1 World Projection

Establish the definitive exploration presentation:

- 2D gameplay presented as layered 2.5D environments
- camera model and following
- tile-to-screen transform
- elevation offset
- world scaling
- screen-space UI separation

### 5.2 Render Architecture

Use explicit passes:

BACKDROP → DISTANT STRUCTURES → TERRAIN → RAISED STRUCTURES → PARTY / ENTITIES → PROPS → FOREGROUND → ATMOSPHERE / EFFECTS → HUD

### 5.3 Elevation

- elevation metadata
- bridge height
- raised structures
- depth layers
- occlusion behavior
- vertical screen offsets
- depth sorting

**Elevation remains separate from collision.**

### 5.4 Reference Environment

Build one authored ruin environment containing:

- floor
- walls
- broken walls
- pillars
- bridge
- abyss/drop
- stairs
- debris
- doorway
- chest
- terminal
- environmental landmark
- foreground obstruction

### Phase 5 Exit Gate

A screenshot of one room should be unmistakably **Daedalus**, not a generic prototype.

# Phase 6 — Exploration Vertical Slice

## Goal

Create the first complete playable slice of the finished game: a real 10–20 minute experience.

The player should:

enter → see party → receive location introduction → explore fog → discover landmarks → loot → interact with terminal → encounter enemy → fight → receive loot → reach extraction → extract → return to campaign state

### Core Work

- minimap
- discovery/fog presentation
- location/depth HUD
- four-member party HUD
- interaction language
- objectives
- exploration events
- environmental storytelling
- complete expedition loop

### Phase 6 Exit Gate

A player can complete a short expedition without developer-only test controls.

# Phase 7 — Combat Vertical Slice

## Goal

Make combat look and feel like part of the same product as exploration.

### Combat

- party turn order
- enemy turn order
- selected actor
- target selection
- attack
- skill
- item
- defend
- status effects
- damage feedback
- death
- victory
- XP
- gold
- cores
- combat animation/effects

### Phase 7 Exit Gate

A complete tactical battle visually belongs to the same game as the exploration room.

# Phase 8 — Character & Asset Production

## Goal

Turn data records into recognizable game characters.

Initial production characters:

- Arden
- Lyra
- Marek
- Sera

Voss becomes the fifth roster character.

Each production character needs:

### Exploration

- idle
- walk up/down/left/right
- contextual states

### Combat

- idle
- attack
- hit
- defeat
- skill

### UI

- portrait
- party icon
- selection state
- status icon

### Identity

- silhouette
- equipment language
- restrained color identity
- role readability

### Phase 8 Exit Gate

Four characters are visually identifiable and animated.

# Phase 9 — Effects, Lighting & Atmosphere

Do this before mass biome production.

### Lighting Language

- **Base:** cool, dark, restrained
- **Safe areas:** warm amber
- **Mystery/discovery:** teal
- **Danger:** red/orange

### Effects

- torch glow
- terminal glow
- core glow
- magical energy
- hit sparks
- damage feedback
- healing
- death effects
- discovery pulse
- extraction effect
- particles
- mist
- dust
- embers

### Atmosphere

- ambient tint
- fog
- depth haze
- parallax
- soft light
- screen-space effects
- color grading

### Phase 9 Exit Gate

Movement, combat, loot, and discovery all have readable visual feedback and the environment carries the target mood.

# Phase 10 — One Complete Biome

## Goal

Build one production-quality biome rather than several incomplete ones.

Example: **The Ruined Depths**

Target:

- 10–20 reusable environment pieces
- 10+ props
- 3–5 enemy types
- 3–5 events
- controlled loot table
- multiple room types
- consistent lighting
- discoverable landmarks
- several combat compositions
- environmental storytelling

The player should be able to spend substantial time there and feel like they are exploring a coherent place.

### Phase 10 Exit Gate

One biome supports a repeatable expedition loop with meaningful progression.

# Phase 11 — Progression & Campaign

Scale the systems after the first biome works.

### Roster

5–8 characters.

### Gear

- weapons
- armor
- accessories
- rarity
- conditional effects

### Cores

- families
- tiers
- synthesis

### Materials

- common
- uncommon
- rare

### Recipes

Meaningful choices rather than collection for its own sake.

### Other Systems

- morale with actual consequences
- alignment and campaign consequences
- overworld destinations
- camp/recovery/management
- persistent progression

### Phase 11 Exit Gate

The player has a reason to leave, return, upgrade, and go again.

# Phase 12 — Content Expansion

Expand to 3–4 production biomes:

1. The Ruined Depths
2. The Ashen Halls
3. The Verdant Below
4. The Crystal Wastes

Each biome gets:

- unique architecture
- tiles
- palette
- lighting
- backdrop
- hazards
- enemies
- props
- loot
- events
- landmarks
- audio eventually
- visual storytelling

### Phase 12 Exit Gate

All planned content exists at production-quality consistency.

# Phase 13 — UX, Accessibility & Save Polish

### UI

- menus
- party management
- inventory
- gear
- synthesis
- campaign map
- expedition summary
- extraction results
- settings

### Accessibility

- text scaling
- contrast
- animation options
- screen-effects toggle
- input remapping
- readable UI
- reduced flashes
- color-independent information

### Save

- autosave
- manual save
- save slots
- validation
- migration
- recovery

# Phase 14 — Alpha → Beta → Release Candidate

## Alpha

Everything exists. Bugs, balance problems, ugly assets, missing transitions, and incomplete content are acceptable.

## Beta

Everything is playable.

Focus:

- balance
- pacing
- difficulty
- economy
- usability
- performance
- content consistency
- bugs

No major systems should be introduced.

## Release Candidate

No new features.

Only:

- bugs
- optimization
- accessibility
- polish
- balance
- asset consistency
- save reliability
- UX

# Production Tracks

Treat Daedalus as four concurrent tracks:

SYSTEMS | PRESENTATION | CONTENT | VERTICAL SLICE

### Systems

State, party, economy, combat, events.

### Presentation

Renderer, camera, lighting, effects, animation.

### Content

Biomes, enemies, props, items, events.

### Vertical Slice

The integration point that proves the other three tracks are creating the actual game.

# Milestone Gates

1. **Foundation** — passed
2. **Party** — passed
3. **Visual Room** — one room looks like Daedalus
4. **Exploration Slice** — enter → explore → interact → fight → loot → extract
5. **Combat Slice** — tactical battle looks and feels complete
6. **Character Slice** — four identifiable animated characters
7. **Biome Slice** — one biome feels like a real location
8. **Campaign Slice** — leave, return, upgrade, go again
9. **Content Complete** — all planned content exists
10. **Release** — stable, balanced, polished

# Development Guardrail

Do not measure progress primarily by C# file count or architectural abstractions.

Every major engineering milestone must produce a **visible, playable improvement**.

Bad milestone:

> Implement RenderPass abstraction.

Good milestone:

> Implement RenderPass abstraction and use it to produce a layered ruin room with elevated bridge, foreground pillar, and four-character party.

The risk is no longer only presentation outrunning gameplay.

The new guardrail is:

> **Architecture must not outrun the game.**

# Current Priority

The project has the skeleton.

The next concrete target is:

> **Build the first 10 minutes of the finished game, starting with a single spectacular ruin room.**

That room should ultimately demonstrate:

- four-person party
- authored character sprites
- layered stone
- elevation
- bridge
- abyss/backdrop
- foreground occlusion
- fog of war
- chest
- terminal
- enemy
- lighting
- teal environmental glow
- minimap
- party HUD
- interaction prompt
- basic effects

Once that room works, it becomes the visual and technical production standard for everything that follows.
