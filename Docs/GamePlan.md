# Dagon - First-Pass Game Plan

## Core Pillars

1. Isometric bullet-heaven combat in a true 3D world.
2. 2D pixel-art sprites presented as billboarded actors inside a 3D space.
3. Heavy enemy density with occasional high-priority threats that force real decisions.
4. Cosmic-horror tone centered on corruption, exposure, mutation, and collapse.
5. Run-based structure with unlocks and progression across runs.

## Player Fantasy

The player is a sailor stranded in an impossible wasteland after a catastrophic upheaval at sea. He wakes alone on a vast black mire of slime, rot, carcasses, and things that should not exist, then survives long enough to begin drawing power from the same nightmare that surrounds him. The fantasy is not heroic mastery in a clean world. It is desperate escalation inside a hostile, unknowable environment, where survival demands contamination, adaptation, and eventual transformation.

## Visual + World Model

### Camera

- Fixed isometric camera angle.
- Gameplay occurs on a flat navigable 3D plane.
- Camera movement should be restrained and readable, with only light follow behavior.

### Rendering Approach

- Characters, enemies, pickups, projectiles, and effects use 2D sprites in world space.
- Sprites are billboarded to face the camera.
- The world itself can use simple 3D geometry, planes, decals, fog, lighting, and shaders to sell depth.
- Shadows should be stylized and simple rather than physically correct.

### Pixel-Art Rules

- Base sprite resolution target: 32x32 for core units.
- Bosses, elites, or large threats can exceed 32x32 while preserving the same pixel language.
- Visual readability matters more than strict sprite-size purity once combat density rises.
- VFX should support the pixel-art look without becoming noisy or smearing into illegibility.

## Tone

### Direction

- Cosmic horror
- Lovecraftian corruption
- Organic mutation
- Ruined rituals
- Astral contamination
- Maritime dread
- Primordial sea-god worship

### Mood

- Oppressive but readable
- Strange rather than purely grotesque
- Alien and sacred at the same time

### Thematic Hooks

- Knowledge is dangerous.
- Power comes with contamination.
- The environment is being rewritten by a breach or signal from outside reality.
- Enemies are not only monsters. They are people, fauna, architecture, and space itself being altered.
- The sea is not absent. It has receded and revealed something older beneath it.
- The sailor is both witness and intruder in a domain tied to Dagon.

## Core Loop

### In-Run Loop

1. Enter a biome or encounter zone.
2. Survive escalating enemy waves.
3. Gain experience, levels, and temporary upgrades.
4. Respond to special threats, events, and elite enemies.
5. Reach a major encounter, extraction point, or run-ending boss.
6. Convert performance into persistent progression.

### Meta Loop

1. Return to a hub or recovery space.
2. Spend resources on permanent unlocks.
3. Unlock new characters, weapons, relic pools, and biome access.
4. Start another run with broader strategic options.

The fiction should begin with the sailor awakening in the mire and treating survival as immediate and bodily, not abstract. Early progression should feel like learning how to live in a place no human was meant to stand.

## Combat Model

### Desired Feel

- The screen should often feel pressured and partially flooded.
- The player should still be able to identify the most dangerous threats quickly.
- Build expression should matter, but movement and threat prioritization should stay relevant throughout the run.

### Threat Layers

#### Fodder

- Large groups
- Low individual threat
- Used to create pressure, body-blocking, and resource flow

#### Specialists

- Ranged attackers
- Chargers
- Debuff or corruption spreaders
- Summoners
- Ambushers emerging from the mire
- Wailers or watchers that punish staying still

These enemies create decision pressure and should interrupt autopilot behavior.

#### Elites

- Durable
- Distinct silhouettes
- Arena-warping attacks or persistent hazards
- Idol-bearers, deep spawn, or mire-titan variants tied to maritime horror imagery

Elites should act as moments of tactical focus inside the broader swarm.

#### Bosses

- Strong visual identity
- Multi-phase or state-shifting behavior
- Less about precision dodging than spatial control and load management under pressure

### Weapons + Abilities

Recommended initial structure:

- One starting weapon per character
- Passive auto-attacks or periodic attacks as the default bullet-heaven baseline
- Active ability on cooldown for burst decision-making
- Passive upgrades that alter projectile count, spread, proc behavior, area, duration, or on-kill effects

This creates enough input depth without turning the game into a manual-action shooter.

## Progression Structure

### Run Progression

- Experience and level-ups
- Randomized upgrade choices
- Temporary relics or mutations
- Event choices that trade power for corruption or risk

### Persistent Progression

- New playable characters
- Weapon unlocks
- Expanded upgrade pools
- Starting loadout modifiers
- Biome or chapter unlocks

### Corruption System

This is a strong candidate for the central progression mechanic.

Possible direction:

- Power can be gained by accepting corruption.
- Corruption increases strength while also adding instability, spawning new hazards, mutating abilities, or changing enemy behavior.
- Some builds lean into corruption deliberately.
- Corruption can be framed as deeper attunement to Dagon's domain rather than generic evil.

This can be both a thematic anchor and a mechanical differentiator.

### Recommended Corruption Model: Tide of Dagon

Recommended first implementation:

- A corruption meter rises during the run from kills, pickups, events, elite contact, and voluntary bargains.
- Thresholds mutate the sailor's attacks and passive effects.
- Higher corruption also increases instability in the run by altering enemy behavior, spawn pressure, hazards, or encounter modifiers.
- Rewards improve with corruption so the player is pushed toward risk instead of simply avoiding it.

This gives the run a clear arc:

- Early run: stranded sailor using practical tools to survive
- Mid run: attuned survivor wielding altered maritime weapons
- Late run: partially claimed instrument of Dagon's domain

## Protagonist: The Sailor

### Role

The sailor should play as a durable mid-range survivor who shapes space and weathers pressure rather than relying on twitch-heavy precision. He begins grounded and practical, then gradually becomes something less human as corruption rises.

### Starting Weapon: Harpoon Cast

Recommended baseline:

- Auto-firing forward harpoon burst
- Good visual readability in isometric combat
- Immediate maritime identity
- Strong upgrade surface for chaining, piercing, tethering, returning, or splitting shots

Possible upgrades:

- Extra harpoons per volley
- Harpoons chain to nearby enemies
- Barbed hits inflict bleed or rot
- Returning harpoons damage enemies on the way back
- Corrupted harpoons split into bone shards or spawn mire spikes

### Active Ability: Brine Surge

Recommended baseline:

- Cooldown-based wave of black seawater or mire erupting around the sailor
- Pushes back weak enemies
- Applies a status such as Soaked, Rot-Touched, or Corrupted
- Acts as the first reliable answer to crowd compression

Alternative actives worth keeping in reserve:

- Lantern Flash
- Anchor Drop

### Combat Identity

- Mid-range control
- Strong crowd shaping
- Reliable under pressure
- Becomes more volatile and monstrous with corruption thresholds

## Recommended MVP

The first playable version should prove the core feel, not the full content plan.

### MVP Features

- One playable character
- One biome: the black mire
- Fixed isometric camera
- 3D ground plane with basic environmental set dressing
- Billboard sprite pipeline for actors
- Basic player movement
- Auto-firing primary attack
- One active skill
- Three to five enemy types
- One elite enemy
- One boss
- Level-up upgrade selection
- Simple run start and run end flow

### MVP Success Criteria

- Movement feels good in isometric space.
- Sprite billboards look convincing in the 3D world.
- Combat remains readable under high enemy counts.
- At least one enemy type consistently forces prioritization.
- One run feels replayable even with minimal content.
- The black mire feels oppressive, lonely, and distinct from a generic horror arena.

## Technical Direction For Unity

### Scene / Systems Assumptions

- Use a 3D scene, not a 2D project layout.
- Constrain gameplay to a plane.
- Use world-space sprites for all actors.
- Centralize billboard handling so all actors face the camera consistently.
- Use simple height offsets for flying enemies, projectiles, and impact presentation.

### Likely Core Systems

- Player controller
- Camera follow rig
- Billboard sprite presenter
- Enemy spawner / wave director
- Enemy state logic
- Projectile system
- Damage / health / status system
- Upgrade selection system
- Run state manager
- Meta progression data layer

### Readability Risks To Manage Early

- Sprite sorting conflicts in dense crowds
- Projectiles vanishing into the environment
- VFX clutter overwhelming small sprites
- Isometric movement feeling imprecise
- Large enemy counts causing CPU pressure

## Art Direction Notes

The environment should help sell scale and unease without becoming busy.

Useful building blocks:

- Endless black mud flats
- Half-sunken monoliths
- Fish carcasses and bone fields
- Mire vents, tar pits, and tidal scars
- Cyclopean stone fragments
- Ritual circles
- Altars, idols, chains, and diseased vegetation
- Fog volumes and limited-color lighting accents

The sprite work should favor bold silhouettes over intricate interior detail. In this genre, readability is more valuable than delicate pixel rendering once the screen fills.

### First Biome: The Black Mire

The opening location should be directly inspired by the sailor's awakening in the story: a slimy, black, seemingly endless plain exposed by some impossible upheaval. It should feel like sea floor and graveyard at the same time.

Biome goals:

- Communicate isolation immediately
- Establish fish rot, salt decay, slime, and ancient exposure as the visual language
- Suggest the presence of a colossal unseen god before the player meets anything explicit
- Make the player feel stranded in a place that should be underwater

Possible environmental set pieces:

- Enormous fish skeletons
- Mud-buried statues
- Rotted ship debris
- Cracked altars
- Wet stone pillars carved with unknown iconography
- Distant silhouettes that might be rock formations or something alive

### First Biome Enemy Roster

Recommended opening set:

- Mire Wretches: shambling fodder that emerge from the mud
- Rot Gulls: fast flankers or diving attackers
- Bloatfish: slow movers that burst into hazards on death
- Watcher Idols: specialist threats that project danger zones or pressure the player from range
- Drowned Acolytes: ranged corruption casters
- Deep Spawn: first elite, a large pressure unit that breaks through crowds and forces repositioning

### First Boss

Recommended first boss: The Mire Colossus

Concept:

- A giant humanoid-fish idol or mud-buried titan that rises from the plain itself
- Feels like the terrain becoming animate
- Establishes the scale of Dagon's domain without revealing Dagon directly too early

## Open Design Decisions

These are the next decisions worth locking down.

1. What is the sailor's name, if any, and should he remain a single protagonist or become one of several unlockable survivors later?
2. What is the core resource economy during runs beyond XP?
3. How far should corruption go mechanically: optional risk system, unavoidable meter, or build-defining axis?
4. What ends a run: timer, boss kill, extraction, chapter clear, or death only?
5. How much direct aiming should the player have versus fully automatic offense?
6. Is the hub a literal place with NPCs and fiction, or a lighter menu layer?

## Strong Recommendation

Make corruption the feature that separates this project from a generic survivor-like.

A useful framing:

- The player grows stronger by exposing themselves to the breach.
- Corruption modifies both the player build and the world state.
- High-corruption runs are more dangerous, less stable, and more rewarding.

If this system lands, it can unify progression, tone, enemy behavior, VFX, and replayability.
