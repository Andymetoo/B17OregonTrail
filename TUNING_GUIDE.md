# B17 Oregon Trail - Tuning Guide

Quick reference for all exposed gameplay parameters and tuning knobs added to the Inspector.

---

## 🎯 ChaosSimulator (Hazard System)
**Script:** `Assets/Scripts/Core/Managers/ChaosSimulator.cs`  
**Purpose:** Controls dynamic hazard phases (Cruise/Flak/Fighters), event timing, and danger progression

### Phase Duration Ranges
- `cruiseMinDuration` / `cruiseMaxDuration` (default: 8-20s)
  - How long peaceful Cruise phases last
- `flakMinDuration` / `flakMaxDuration` (default: 6-14s)
  - Duration of Flak hazard phases
- `fightersMinDuration` / `fightersMaxDuration` (default: 6-14s)
  - Duration of Fighter hazard phases

### Hazard Event Timing
- `eventIntervalAtSafe` (default: 20s)
  - Seconds between flak/fighter events when danger = 0
- `eventIntervalAtDanger` (default: 6s)
  - Seconds between events when danger = 1
  - **Scales linearly with danger level**
- `useExponentialTiming` (default: true)
  - Toggle between exponential (Poisson) and uniform random intervals
- `hazardIntervalJitter` (default: 0.35)
  - Randomness applied to intervals when not using exponential timing

### Crew Injury Timing
- `injuryIntervalAtSafe` (default: 40s)
  - Seconds between crew injury rolls when danger = 0
- `injuryIntervalAtDanger` (default: 12s)
  - Injury interval at danger = 1
- `injuryIntervalJitter` (default: 0.35)

### Damage & Fire
- `minDamage` / `maxDamage` (default: 5-25)
  - Damage range for flak/fighter hits, scaled by danger
- `planeDamageWeightMin` / `planeDamageWeightMax` (0-1 range)
  - How likely damage events are at safe vs dangerous moments
- `fireWeightMin` / `fireWeightMax` (0-1 range)
  - Fire start probability scaling
- `crewInjuryWeightMin` / `crewInjuryWeightMax` (0-1 range)
  - Injury probability scaling

### Injury Severity (0-1 ranges)
- `lightSeverityWeightMin` / `lightSeverityWeightMax`
- `seriousSeverityWeightMin` / `seriousSeverityWeightMax`
- `criticalSeverityWeightMin` / `criticalSeverityWeightMax`
  - **Scaled by danger:** low danger = more light injuries, high danger = more critical

### Other
- `enableChaos` (default: true)
  - Master toggle for all hazard generation
- `cruiseBias` (default: 0.6)
  - Fallback Cruise weight when no leg-specific weights configured

**Public Monitors:**
- `CurrentPhase` - Current hazard phase (Cruise/Flak/Fighters)
- `CurrentDanger` - 0-1 danger level
- `PhaseProgress` - 0-1 progress through current phase
- `PhaseTimeRemaining` - Seconds left in current phase

---

## ✈️ MissionManager (Travel & Progression)
**Script:** `Assets/Scripts/Core/Managers/ChaosSimulator.cs`  
**Purpose:** Manages node-to-node travel, fuel, and mission progression

### Travel Speed
- `travelSpeedScale` (default: 8.0)
  - **Time compression multiplier** for travel between nodes
  - Does NOT change displayed cruise speed
  - Higher = arrive at waypoints faster (more game time compression)
  - Lower = slower travel, more time for hazards to occur

### Fuel
- `startingFuel` (default: 100)
  - Initial fuel amount at mission start

### Other
- `useDistanceForTiming` (default: true)
  - Calculate travel time from distance & plane speed vs fixed node TravelTime
- `autoStartFirstSegment` (default: true)
  - Skip node selection screen and start first leg immediately

---

## 🛠️ PlaneManager (Damage & Systems)
**Script:** `Assets/Scripts/Core/Managers/PlaneManager.cs`  
**Purpose:** Manages plane sections, systems, fires, and structural integrity

### Speed
- `baseCruiseSpeedMph` (default: 180)
  - Cruise speed with all engines operational
- `minCruiseSpeedMph` (default: 60)
  - Minimum speed when all engines destroyed
  - **Speed scales linearly with operational engine count**

### Fire Damage
- `fireDamagePerSecond` (default: 2.0)
  - Integrity damage per second while section is on fire
- `destroyedIntegrityThreshold` (default: 0)
  - Integrity level at which a section is considered destroyed

---

## 👥 CrewActionConfig (Action Parameters)
**ScriptableObject:** `Assets/ScriptableObjects/CrewActionConfig.asset`  
**Purpose:** Tunable parameters for all crew actions (repair, extinguish, treat injury)

### Base Actions (No Consumables)
- **Duration:** How long the action takes (seconds)
- **Success Chance:** 0-1 probability of success
- **Failure Chance:** 0-1 probability something goes wrong on failure
- **Repair Amount Min/Max:** Integrity restored (uses bell curve between min/max)

### Consumable-Enhanced Actions
- Same parameters as base, but with better values
- **Consumes:** Fire extinguisher, repair kit, or medical supplies on use

### Action Types
- **Extinguish Fire** - Put out section fires
- **Repair System** - Fix damaged engines, oxygen, turrets, etc.
- **Treat Injury** - Stabilize or heal injured crew

---

## 🎮 DebugHUD Display
**Script:** `Assets/Scripts/UI/DebugHUD.cs`  
**Purpose:** Real-time display of game state for testing

### Display Elements
- **Hazard Phase & Time Remaining** - Current ChaosSimulator phase with countdown
- **Danger Level** - 0-1 danger progression
- **Fuel Remaining** - Current fuel
- **Speed** - Dynamic cruise speed (mph)
- **Node Progress** - Current → Next node, miles remaining
- **Crew Status** - Health, position, current action for watched crew member

---

## 📊 DamageLogUI (Event Logging)
**Script:** `Assets/Scripts/UI/DamageLogUI.cs`  
**Purpose:** Rolling log of damage events and optional popups

### Popup Toggles
- `popupOnFireStart` (default: true)
  - Show popup when fire starts
- `popupOnSectionDestroyed` (default: true)
  - Show popup when section integrity reaches 0
- `popupOnCrewDeath` (default: true)
  - Show popup when crew member dies
- `popupOnSeriousCrewInjury` (default: **false**)
  - Disabled to avoid duplication with GameEvent injury popups

---

## 🎲 EventTriggerManager (Random Cruise Events)
**Script:** `Assets/Scripts/Core/Events/EventTriggerManager.cs`  
**Purpose:** Triggers random flavor events during Cruise phases only

### Configuration
- `eventCheckInterval` (default: 5s)
  - How often to roll for random events
- `baseEventChance` (default: 0.15)
  - Probability of event per check
- `fallbackEvents` (list)
  - GameEvent assets to choose from randomly

**Behavior:**
- Only fires during **Cruise** phase (suppressed during Flak/Fighters)
- Shows Oregon Trail-style popups with flavor text + outcomes
- Can apply fuel loss, section damage, fires, and crew injuries

---

## 🔧 Quick Tuning Tips

### Making Combat More Intense
1. ↓ Decrease `eventIntervalAtDanger` in ChaosSimulator (e.g., 3s instead of 6s)
2. ↑ Increase `flakMaxDuration` / `fightersMaxDuration` for longer hazard phases
3. ↑ Increase `maxDamage` for harder hits

### Making Travel Faster
1. ↑ Increase `travelSpeedScale` in MissionManager (e.g., 16 for 2x speed)
2. ↓ Decrease phase durations across the board

### Balancing Difficulty
- **Danger progression:** Configured per mission leg in MissionNode assets (`StartDanger`, `EndDanger`)
- **Phase weights:** Also per leg (`PhaseWeights.Cruise/Flak/Fighters`)
- **Crew actions:** Edit `CrewActionConfig` ScriptableObject for success rates and repair amounts

---

## 📝 Notes

- **First hazard event:** Always fires within 1-3 seconds after Flak/Fighter announcement (regardless of interval settings)
- **Grace period:** 3-5 seconds of guaranteed Cruise at leg start (hardcoded in `ChaosSimulator.ConfigureLeg`)
- **Danger scaling:** Most weights (damage, fire, injury) lerp between min/max based on current danger level
- **Phase transitions:** Use weighted random selection; weights are per-leg configurable or fall back to `cruiseBias`

---

**Last Updated:** December 2025  
**Version:** Post-Phase-System-Overhaul
