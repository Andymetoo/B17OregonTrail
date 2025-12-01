# Chaos Simulator Guide

This document explains how between-node hazards are generated and how to tune them.

## Concepts

- Danger (0–1): The single dial that represents how risky the current leg is. Danger typically starts at the leg's `StartDanger` and linearly increases toward `EndDanger` as you progress along the leg.
- Phases: Short alternating segments of time while traveling: `Cruise`, `Flak`, `Fighters`. Mission nodes define how likely each phase is via `PhaseWeights`.
- Events: Within hazard phases (Flak/Fighters), the simulator periodically triggers events (flak bursts or fighter passes). The rate and intensity of events scale with Danger.

## Where Values Live

- MissionNode (to-destination):
  - `StartDanger` (0–1): Danger at the start of the leg.
  - `EndDanger` (0–1): Danger at the end of the leg.
  - `PhaseWeights`: Cruise/Flak/Fighters weights used to randomly choose the next phase.

- ChaosSimulator (global tuning):
  - `eventIntervalAtSafe` / `eventIntervalAtDanger` (seconds): Mean time between hazard events when Danger=0 vs Danger=1.
  - `injuryIntervalAtSafe` / `injuryIntervalAtDanger` (seconds): Mean time between crew-injury rolls. Also scaled by a Danger-derived injury weight.
  - `useExponentialTiming`: If true, event timing uses an exponential (Poisson) process — inter-event times are random with the configured mean. If false, uniform jitter is applied.
  - `hazardIntervalJitter` / `injuryIntervalJitter`: Uniform ±jitter applied to mean interval when not using exponential timing.
  - Event Weights (0–1 ranges): Min/Max ranges for how strongly Danger scales:
    - `planeDamageWeightMin/Max`: Higher -> more/stronger damage.
    - `fireWeightMin/Max`: Higher -> more fires from hits.
    - `crewInjuryWeightMin/Max`: Higher -> more frequent crew injuries.
  - Injury Severity Weights (0–1 ranges): Min/Max ranges for `Light`, `Serious`, `Critical` injury proportions as Danger changes. These are normalized into a distribution per injury occurrence.
  - Phase Scheduling Defaults:
    - `phaseMinDuration` / `phaseMaxDuration` (seconds): How long a phase typically lasts before we roll a new one.
    - `cruiseBias`: Fallback bias for Cruise if node weights are unavailable.

## Flow Overview

1. MissionManager starts a leg and calls:
   - `ChaosSimulator.ConfigureLeg(StartDanger, EndDanger, PhaseWeights)`
2. While traveling:
   - Current Danger = Lerp(StartDanger, EndDanger, SegmentProgress).
   - Choose/advance the current Phase. Durations are randomized between `phaseMinDuration` and `phaseMaxDuration`.
  - In Flak/Fighters phases, hazard events occur on a stochastic timer. Mean = `Lerp(eventIntervalAtSafe, eventIntervalAtDanger, Danger)`, with randomness (exponential or jitter) applied to avoid predictability.
  - Crew injuries use a separate stochastic timer. Mean = `Lerp(injuryIntervalAtSafe, injuryIntervalAtDanger, injuryWeight(Danger))`.
3. When segment progress would reach 100% during a hazard phase, MissionManager waits until the phase returns to Cruise to complete the leg.

## Tuning Tips

- To make a leg feel increasingly risky, set node `StartDanger` low and `EndDanger` high (e.g., 0.2 -> 0.8). For the return trip, invert them.
- Use `PhaseWeights` on the destination node to shape overall experience:
  - Long overland: Cruise 0.7, Flak 0.15, Fighters 0.15
  - Over target: Cruise 0.4, Flak 0.35, Fighters 0.25
- Adjust global ranges in ChaosSimulator to control how Danger maps to frequency and intensity:
  - `planeDamageWeightMin/Max`: Raise Max for harsher damage as Danger climbs.
  - `fireWeightMin/Max`: Raise Max to increase fire chance in flak/fighter hits.
  - `crewInjuryWeightMin/Max`: Raise Max to increase injury frequency.
  - `light/serious/critical severity` ranges: With higher Danger, shift weights toward more serious outcomes.

## Plain-English Definitions

- Event Interval (safe/danger): Mean time between flak/fighter occurrences at low/high Danger. Actual timings are randomized per event (exponential/jitter) to avoid a metronome feel.
- Phase Duration (min/max): How long we stay in Cruise/Flak/Fighters before rolling the next phase. Keeps pacing varied and prevents instant toggling.
- Cruise Bias: Fallback probability favoring Cruise phase when node weights are missing or invalid.
- Weights (min/max): 0–1 sliders that describe how much an effect should be at Danger=0 vs Danger=1. The simulator lerps between them using current Danger.

## Notes

- EventManager travel rolls are disabled by default (`enabledForTravel=false`) to avoid duplication; ChaosSimulator owns between-node hazards.
- Damage events flow through `PlaneManager`, so `DamageLogUI` reflects them automatically.
- Oregon Trail-style messages/popups use `EventLogUI` and `EventPopupUI`. Ensure both are present in the scene.
