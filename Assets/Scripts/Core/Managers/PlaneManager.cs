using System;
using System.Collections.Generic;
using UnityEngine;

public class PlaneManager : MonoBehaviour
{
    public static PlaneManager Instance { get; private set; }

    /// <summary>
    /// All structural sections of the plane (nose, cockpit, wings, tail, etc.)
    /// </summary>
    public List<PlaneSectionState> Sections = new List<PlaneSectionState>();

    /// <summary>
    /// All systems (engines, oxygen, turrets, etc.) that live in sections.
    /// </summary>
    public List<PlaneSystemState> Systems = new List<PlaneSystemState>();

    // Simple fire damage tuning
    [Header("Fire Settings")]
    [Tooltip("Damage per second to section integrity while on fire.")]
    public float fireDamagePerSecond = 2f;

    [Tooltip("Minimum integrity before a section is considered destroyed.")]
    public int destroyedIntegrityThreshold = 0;

    // Events for other systems / UI
    public event Action<PlaneSectionState> OnSectionDamaged;
    public event Action<PlaneSectionState> OnSectionDestroyed;
    public event Action<PlaneSectionState> OnFireStarted;
    public event Action<PlaneSectionState> OnFireExtinguished;

    public event Action<PlaneSystemState> OnSystemStatusChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Called by GameStateManager once per simulation tick.
    /// </summary>
    public void Tick(float deltaTime)
    {
        TickFires(deltaTime);
    }

    private void TickFires(float deltaTime)
    {
        foreach (var section in Sections)
        {
            if (!section.OnFire) continue;

            // Continuous fire damage over time
            int oldIntegrity = section.Integrity;

            float damageFloat = fireDamagePerSecond * deltaTime;
            int damage = Mathf.FloorToInt(damageFloat);

            if (damage <= 0) continue;

            section.Integrity = Mathf.Max(destroyedIntegrityThreshold, section.Integrity - damage);

            if (section.Integrity != oldIntegrity)
            {
                OnSectionDamaged?.Invoke(section);

                if (section.Integrity <= destroyedIntegrityThreshold)
                {
                    OnSectionDestroyed?.Invoke(section);
                }
            }
        }
    }

    // ------------------------------------------------------------------
    // DAMAGE / FIRE APPLICATION
    // ------------------------------------------------------------------

    /// <summary>
    /// Apply a hit to a specific section, optionally starting a fire.
    /// </summary>
    public void ApplyHitToSection(string sectionId, int damageAmount, bool canStartFire, float fireStartChance = 0.25f)
    {
        var section = GetSection(sectionId);
        if (section == null) return;

        int oldIntegrity = section.Integrity;

        section.Integrity = Mathf.Max(destroyedIntegrityThreshold, section.Integrity - damageAmount);

        if (section.Integrity != oldIntegrity)
        {
            OnSectionDamaged?.Invoke(section);

            if (section.Integrity <= destroyedIntegrityThreshold)
            {
                OnSectionDestroyed?.Invoke(section);
            }
        }

        if (canStartFire && !section.OnFire)
        {
            if (UnityEngine.Random.value < fireStartChance)
            {
                section.OnFire = true;
                OnFireStarted?.Invoke(section);
            }
        }
    }

    /// <summary>
    /// Apply a hit that may pick a random section, useful for flak.
    /// </summary>
    public void ApplyRandomHit(int damageAmount, bool canStartFire, float fireStartChance = 0.25f)
    {
        if (Sections == null || Sections.Count == 0) return;

        int index = UnityEngine.Random.Range(0, Sections.Count);
        var section = Sections[index];

        ApplyHitToSection(section.Id, damageAmount, canStartFire, fireStartChance);
    }

    /// <summary>
    /// Extinguish fire in a section (called by crew actions later).
    /// </summary>
    public bool TryExtinguishFire(string sectionId)
    {
        var section = GetSection(sectionId);
        if (section == null) return false;
        if (!section.OnFire) return false;

        section.OnFire = false;
        OnFireExtinguished?.Invoke(section);
        return true;
    }

    // ------------------------------------------------------------------
    // SYSTEM STATUS / REPAIRS
    // ------------------------------------------------------------------

    public PlaneSystemState GetSystem(string systemId)
    {
        return Systems.Find(s => s.Id == systemId);
    }

    public PlaneSectionState GetSection(string sectionId)
    {
        return Sections.Find(s => s.Id == sectionId);
    }

    /// <summary>
    /// Mark a system as damaged or destroyed.
    /// </summary>
    public void SetSystemStatus(string systemId, SystemStatus newStatus, SpecialState special = SpecialState.None)
    {
        var system = GetSystem(systemId);
        if (system == null) return;

        system.Status = newStatus;
        system.Special = special;

        OnSystemStatusChanged?.Invoke(system);
    }

    /// <summary>
    /// Example repair method. You can tune this later.
    /// </summary>
    public bool TryRepairSystem(string systemId, float repairStrength = 1f)
    {
        var system = GetSystem(systemId);
        if (system == null) return false;

        if (system.Status == SystemStatus.Destroyed)
        {
            // Maybe destroyed systems can't be repaired at all
            return false;
        }

        // Simple example: any repair puts it back to Operational
        system.Status = SystemStatus.Operational;
        system.Special = SpecialState.None;
        OnSystemStatusChanged?.Invoke(system);
        return true;
    }

    /// <summary>
    /// Is a station "usable" - i.e. its section isn't destroyed and the system is operational?
    /// </summary>
    public bool IsStationFunctional(string stationSystemId)
    {
        var system = GetSystem(stationSystemId);
        if (system == null) return false;

        var section = GetSection(system.SectionId);
        if (section == null) return false;

        bool sectionAlive = section.Integrity > destroyedIntegrityThreshold;
        bool systemOk = system.Status == SystemStatus.Operational;

        return sectionAlive && systemOk;
    }

    public void DebugDamageSection(string sectionId)
    {
        var section = GetSection(sectionId);
        if (section == null)
        {
            Debug.LogWarning($"[Plane] DebugDamageSection: no section with id {sectionId}");
            return;
        }

        section.Integrity = Mathf.Max(0, section.Integrity - 50);
        section.OnFire = true;

        Debug.Log($"[Plane] Debug damaged {sectionId}: integrity={section.Integrity}, onFire={section.OnFire}");

        // If you have an event, raise it here, e.g.:
        // OnSectionStateChanged?.Invoke(section);
    }

}
