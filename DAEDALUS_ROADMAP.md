# Daedalus — Revised Production Roadmap

The original roadmap treated Phases 5–9 primarily as architecture, interaction, combat, effects, and content. The completed-game visual target is substantially broader: layered 2.5D environments, authored visual language, lighting/atmosphere, four-character presentation, tactical combat presentation, biome identity, effects, and content density.

The revised roadmap therefore treats the first polished environment and first complete exploration/combat slice as production gates.

## Completed

- Phase 1 — Prototype foundation
- Phase 2 — Campaign/expedition state and persistence
- Phase 3 — Expedition economy, nodes, extraction, rewards, synthesis
- Phase 4 — Party controller, followers, formations, morale, deterministic movement

## Phase 5 — Visual Foundation

- Multi-pass exploration renderer
- Backdrop, terrain, structures, entities, foreground, effects
- Elevation and occlusion metadata
- Central biome palette and presentation profile
- Camera and 2.5D-style depth treatment
- First layered ruin presentation

**Exit gate:** one exploration room must visibly read as Daedalus rather than a generic prototype.

## Phase 6 — Exploration Vertical Slice

- Minimap and discovery presentation
- Location/depth HUD
- Four-member party HUD
- Interaction prompts
- Objective language
- Exploration events
- Environmental storytelling hooks
- Complete enter → explore → interact → discover → extract loop

**Exit gate:** a player can complete a short expedition without developer-only test controls.

## Phase 7 — Battle Vertical Slice

- Party-aware battle presentation
- Enemy tracker
- Staged arena
- Party status panel
- Command presentation
- Target presentation
- Battle state/turn-order foundations
- Clear action feedback

**Exit gate:** a battle screenshot should belong to the same visual product as the exploration screenshot.

## Phase 8 — Character and Effects Production

- Character identity and silhouette system
- Directional animation states
- Hit/heal/death feedback
- Core and terminal glow
- Ambient tint
- Mist/dust/particles
- Screen feedback
- Color grading
- Controlled visual effects vocabulary

**Exit gate:** movement, combat, loot and discovery all have readable visual feedback.

## Phase 9 — First Production Biome and Content

- Four biome presentation profiles
- Biome-specific enemy families
- Starter gear and expanded recipes
- Progression/leveling
- Expanded loot materials
- Exploration event catalog
- First production-quality biome
- Economy and progression tuning

**Exit gate:** one biome supports a repeatable expedition loop with meaningful progression.

## After Phase 9

### Phase 10 — Campaign and Progression
Roster expansion, overworld destinations, alignment/flags, synthesis depth, camp/recovery, persistent progression.

### Phase 11 — Biome Expansion
Three to four production biomes with unique architecture, enemies, props, events, loot, lighting and environmental storytelling.

### Phase 12 — UX / Accessibility / Save Polish
Menus, party management, inventory, synthesis UI, save slots, autosave, migration validation, input remapping, readable UI and reduced-effects options.

### Phase 13 — Alpha / Beta / Release Candidate
Balance, pacing, performance, bug fixing, content consistency, accessibility and final asset polish.

## Production Rule

Do not measure progress primarily by C# file count or architectural abstractions.

Every major milestone must produce a visible, playable improvement.

The target is not "a complete architecture that could make the game."

The target is the game.
