# DAEDALUS — Roadmap

The authoritative project plan is now:

**DAEDALUS_IMPLEMENTATION_PLAN.md**

The project has moved from an architecture-first roadmap to a production / vertical-slice roadmap.

## Completed Foundation

These systems are already in place:

- campaign state and persistence
- expedition state
- four-person party runtime
- roster and formation support
- exploration movement
- visibility / fog of war
- minimap
- interactables and exploration events
- multi-enemy tactical battle state
- battle commands and status effects
- extraction and campaign return
- progression / morale / synthesis foundations
- authored ruin features
- feedback effects
- green CI validation

## Production Gates

1. **Visual Benchmark Room**
2. **Character Presentation**
3. **Atmosphere & Effects**
4. **Complete Exploration Slice**
5. **Complete Combat Slice**
6. **Campaign Loop**
7. **First Production Biome**
8. **Progression Depth**
9. **Content Expansion**
10. **UX / Save / Accessibility**
11. **Alpha / Beta / Release Candidate**

## Immediate Objective

Build the first spectacular Ruined Depths room and use it as the production benchmark for:

- environment art
- character scale and readability
- elevation and occlusion
- abyss depth
- lighting
- effects
- interaction
- exploration HUD

The next engineering work should directly improve that room or unblock its production.

## Development Guardrail

> **Architecture must not outrun the game.**

Do not introduce broad systems or speculative abstractions unless they directly support an active production milestone.

For detailed implementation order, acceptance criteria, refactor timing, tests, and the immediate sprint backlog, see:

**DAEDALUS_IMPLEMENTATION_PLAN.md**
