# Dagon - Corruption Tradeoff Implementation

## Purpose

This document records the current corruption tradeoff implementation, what shipped in the first pass, and what should happen next.

Use this as the corruption-specific source of truth before adding more corruption weapons, actives, events, or bosses.

## Current Goal

Corruption is no longer just a passive punishment meter.

The new direction is:

- corruption gives the player real power
- corruption also makes the world more dangerous
- the player should be choosing how long to ride higher corruption instead of only enduring it

## Implemented First Pass

## Stage Structure

Corruption now uses four stages:

- Stage 1: `25`
- Stage 2: `50`
- Stage 3: `75`
- Stage 4: `100`

Key runtime:

- [CorruptionMeter](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Core/CorruptionMeter.cs)

Current support:

- upward and downward threshold handling
- current stage tracking
- corruption gain scaling
- explicit corruption reduction

## Corruption Choice Popup

Crossing a new stage threshold now pauses the run and opens a corruption popup.

The player must choose:

- `1 boon`
- `1 drawback`

Those choices are remembered for the run.

If corruption later drops below that stage:

- the effects from that stage are disabled

If corruption rises back into that stage later:

- the previously chosen effects reactivate
- the player does not choose again

Key runtime:

- [CorruptionRuntimeEffects](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/CorruptionRuntimeEffects.cs)
- [ExperienceHud](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/ExperienceHud.cs)

## Current Boon Pool

### Stage 1

- `+15% Fire Rate`
- `+10% Damage`
- `+15% Radius`

### Stage 2

- `+20% Fire Rate`
- `+20% Damage`
- `+1 Max Heart`

### Stage 3

- `+25% Fire Rate`
- `+25% Damage`
- `+25% Radius`

### Stage 4

- `+35% Fire Rate`
- `+35% Damage`
- `+2 Max Hearts`

## Current Drawback Pool

### Stage 1

- `-25% Healing`
- `+20% Fodder Waves`
- `+15% Corruption Gain`

### Stage 2

- `+1 Specialist Cap`
- `+20% Specialist Waves`
- `-40% Healing`

### Stage 3

- `Early Elite Waves`
- `+25% Elite Waves`
- `+15% Contact Damage`

### Stage 4

- `+1 Elite Cap`
- `Boss Pressure Up`
- `+25% All Damage`

## Runtime Effect Hooks

The first pass supports both player-side and world-side corruption effects.

### Player-side

Current player-facing hooks:

- global weapon fire rate bonus
- global weapon damage bonus
- active radius bonus
- reversible bonus max health
- healing efficiency penalty
- incoming contact damage penalty
- incoming all-damage penalty

Key runtime:

- [CorruptionRuntimeEffects](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/CorruptionRuntimeEffects.cs)
- [Health](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Core/Health.cs)

### World-side

Current world-facing hooks:

- corruption gain multiplier from pickups
- fodder wave size multiplier
- specialist wave size multiplier
- elite wave size multiplier
- specialist concurrent cap bonus
- elite concurrent cap bonus
- early elite-wave unlock override
- boss ambient pressure interval penalty

Key runtime:

- [CorruptionRuntimeEffects](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/CorruptionRuntimeEffects.cs)
- [SpawnDirector](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/SpawnDirector.cs)

## Fountain Cleanse System

First-pass corruption relief now exists through world fountains.

Current behavior:

- one fountain at a time
- first spawn after an early delay
- respawns after a cooldown once used
- does not spawn during boss fights
- reduces corruption by `25`
- walk-over trigger interaction for now

Key runtime:

- [CorruptionFountainDirector](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/CorruptionFountainDirector.cs)
- [CorruptionFountain](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/CorruptionFountain.cs)
- [SceneRuntimeBuilder](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Bootstrap/SceneRuntimeBuilder.cs)

## HUD Changes

Corruption is now visually more important in the HUD.

Current changes:

- corruption bar moved to top center
- it sits under the XP bar
- threshold markers show `25 / 50 / 75 / 100`
- current corruption value remains visible
- current tier is shown as `Corruption T1-T4`

Key runtime:

- [ExperienceHud](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/ExperienceHud.cs)

## Pause / Flow Integration

The corruption popup now behaves like a run-stopping choice screen.

Current integration:

- corruption popup pauses the run
- pause menu will not open over active upgrade/corruption choice overlays

Key runtime:

- [RunStateManager](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/RunStateManager.cs)

## What This First Pass Does Well

- gives corruption real upside instead of only punishment
- makes corruption reversible and manageable
- ties corruption into the enemy/world systems instead of leaving it as a side meter
- creates clear milestone moments at the four thresholds
- gives the game a stronger identity hook

## Current Limitations

This first pass is intentionally still narrow.

Current limitations:

- no corruption-only weapons yet
- no corruption-only actives yet
- no corruption-specific enemy variants yet
- no corruption-specific bosses yet
- fountain visuals are still simple runtime pickup-style objects
- corruption popup/UI needs in-editor tuning for readability and feel
- numeric tuning has not been playtested enough yet

## Corruption Events

Corruption now has a separate event lane layered on top of normal spawning.

Current behavior:

- regular waves still run normally
- corruption events are additive and do not replace normal event cadence
- corruption events begin once corruption reaches `25`
- higher corruption tiers shorten the corruption-event timer
- corruption events are spawn-pressure events only in v1
- each event shows a visible corruption banner

Current event pool:

- Stage 1: `Rotting Swell`
- Stage 2: `Rotting Swell`, `Siren Pressure`
- Stage 3: `Rotting Swell`, `Siren Pressure`, `Deep Tide`
- Stage 4: all of the above plus `Corruption Front`

Current implementation:

- `Rotting Swell` triggers a fodder corruption wave
- `Siren Pressure` triggers a specialist corruption wave
- `Deep Tide` triggers an elite corruption wave
- `Corruption Front` triggers a specialist wave, then follows with repeated fodder corruption waves for a short window

Key runtime:

- [CorruptionEventDirector](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/CorruptionEventDirector.cs)
- [SpawnDirector](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/SpawnDirector.cs)
- [DeveloperSandboxController](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Bootstrap/DeveloperSandboxController.cs)

## Recommended Next Work

The immediate next step is tuning, not more feature breadth.

## 1. Tune Existing Corruption Numbers

Priority checks:

- do the boons feel worth taking at each threshold
- are the drawbacks legible and meaningful
- does healing punishment stack too harshly
- do specialist/elite cap penalties become unfair too fast
- does early elite-wave unlock come online too brutally
- are fountain timing and spawn distance actually useful

## 2. Add One Corruption Active

Best next feature after tuning:

- unlock `1 corruption-only active` at stage 2 or stage 3

Why:

- it makes corruption feel more unique than stat-only scaling
- it is smaller and safer than adding full corruption weapons first
- it gives a clear power fantasy without needing a giant content pass

Good candidates:

- a corruption pulse around the player
- a temporary rot aura
- a short overdrive state tied to corruption tier

## 3. Add Corruption Events

After the first corruption active, add visible world-state responses to corruption.

Best direction:

- corruption-driven wave escalation
- specialist pressure events
- battlefield pressure events

These should be distinct from normal spawn pressure so corruption changes the feel of the run, not only the numbers.

## 4. Add Corruption Content Unlocks

Later follow-up content:

- corruption-only weapons
- corruption-only enemy variants
- corruption-only bosses or boss modifiers

This should happen after tuning proves the base tradeoff loop works.

## Recommended Near-Term Order

1. Tune stage boon/drawback values in-editor
2. Tune fountain cadence and cleanse amount
3. Improve corruption popup feel and HUD emphasis
4. Add one corruption-only active
5. Add corruption-driven threat events
6. Only then expand into corruption weapons / enemies / bosses

## Testing Notes

Still needs direct Unity playtest validation for:

- threshold crossing flow
- reactivation when corruption rises again after a cleanse
- healing penalties versus health drop rates
- wave-size penalties under later-wave scaling
- boss ambient pressure penalty feel
- top-center HUD readability

## Current File Map

- meter and stage logic: [CorruptionMeter](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Core/CorruptionMeter.cs)
- runtime choice/effect system: [CorruptionRuntimeEffects](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/CorruptionRuntimeEffects.cs)
- player damage/healing hooks: [Health](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Core/Health.cs)
- world spawn hooks: [SpawnDirector](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/SpawnDirector.cs)
- corruption HUD/popup: [ExperienceHud](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/ExperienceHud.cs)
- pause integration: [RunStateManager](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/RunStateManager.cs)
- fountain loop: [CorruptionFountainDirector](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/CorruptionFountainDirector.cs)
- fountain pickup: [CorruptionFountain](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Gameplay/CorruptionFountain.cs)
- bootstrap hookup: [SceneRuntimeBuilder](/Users/jakeferrigan/Echo Rift/Assets/Scripts/Bootstrap/SceneRuntimeBuilder.cs)
