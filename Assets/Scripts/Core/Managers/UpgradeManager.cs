using System;
using UnityEngine;

/// <summary>
/// Manages persistent upgrades to crew action performance.
/// Bonuses are added to base CrewActionConfig values and saved between sessions.
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Serializable]
    public class ActionUpgrades
    {
        [Header("Medical Upgrades")]
        [Tooltip("Bonus to base medical success chance (0.01 = +1%)")]
        public float baseMedicalSuccessBonus = 0f;
        
        [Tooltip("Reduction to base medical duration in seconds (negative = faster)")]
        public float baseMedicalDurationBonus = 0f;

        [Header("Repair Upgrades")]
        [Tooltip("Bonus to base repair success chance (0.01 = +1%)")]
        public float baseRepairSuccessBonus = 0f;
        
        [Tooltip("Reduction to base repair duration in seconds (negative = faster)")]
        public float baseRepairDurationBonus = 0f;
        
        [Tooltip("Bonus to minimum repair amount")]
        public int baseRepairAmountMinBonus = 0;
        
        [Tooltip("Bonus to maximum repair amount")]
        public int baseRepairAmountMaxBonus = 0;

        [Header("Fire Fighting Upgrades")]
        [Tooltip("Bonus to base extinguish success chance (0.01 = +1%)")]
        public float baseExtinguishSuccessBonus = 0f;
        
        [Tooltip("Reduction to base extinguish duration in seconds (negative = faster)")]
        public float baseExtinguishDurationBonus = 0f;

        [Header("Consumable Upgrades")]
        [Tooltip("Reduction to medkit duration in seconds (negative = faster)")]
        public float medkitDurationBonus = 0f;
        
        [Tooltip("Reduction to repair kit duration in seconds (negative = faster)")]
        public float repairKitDurationBonus = 0f;
        
        [Tooltip("Reduction to fire extinguisher duration in seconds (negative = faster)")]
        public float fireExtinguisherDurationBonus = 0f;
        
        [Tooltip("Bonus to repair kit minimum amount")]
        public int repairKitAmountMinBonus = 0;
        
        [Tooltip("Bonus to repair kit maximum amount")]
        public int repairKitAmountMaxBonus = 0;
    }

    [SerializeField] private ActionUpgrades currentUpgrades = new ActionUpgrades();
    
    public ActionUpgrades Upgrades => currentUpgrades;

    // Events for UI to react to upgrades
    public event Action<string> OnUpgradeUnlocked;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        LoadUpgrades();
    }

    // ------------------------------------------------------------------
    // GETTER METHODS: Apply upgrades to base config values
    // ------------------------------------------------------------------

    public float GetModifiedMedicalDuration()
    {
        var config = CrewActionConfig.Instance;
        if (config == null) return 10f;
        return Mathf.Max(1f, config.baseMedicalDuration + currentUpgrades.baseMedicalDurationBonus);
    }

    public float GetModifiedMedicalSuccess()
    {
        var config = CrewActionConfig.Instance;
        if (config == null) return 0.65f;
        return Mathf.Clamp01(config.baseMedicalSuccessChance + currentUpgrades.baseMedicalSuccessBonus);
    }

    public float GetModifiedRepairDuration()
    {
        var config = CrewActionConfig.Instance;
        if (config == null) return 10f;
        return Mathf.Max(1f, config.baseRepairDuration + currentUpgrades.baseRepairDurationBonus);
    }

    public float GetModifiedRepairSuccess()
    {
        var config = CrewActionConfig.Instance;
        if (config == null) return 0.70f;
        return Mathf.Clamp01(config.baseRepairSuccessChance + currentUpgrades.baseRepairSuccessBonus);
    }

    public int GetModifiedRepairAmountMin()
    {
        var config = CrewActionConfig.Instance;
        if (config == null) return 10;
        return Mathf.Max(1, config.baseRepairAmountMin + currentUpgrades.baseRepairAmountMinBonus);
    }

    public int GetModifiedRepairAmountMax()
    {
        var config = CrewActionConfig.Instance;
        if (config == null) return 30;
        return Mathf.Max(1, config.baseRepairAmountMax + currentUpgrades.baseRepairAmountMaxBonus);
    }

    public float GetModifiedExtinguishDuration()
    {
        var config = CrewActionConfig.Instance;
        if (config == null) return 8f;
        return Mathf.Max(1f, config.baseExtinguishDuration + currentUpgrades.baseExtinguishDurationBonus);
    }

    public float GetModifiedExtinguishSuccess()
    {
        var config = CrewActionConfig.Instance;
        if (config == null) return 0.75f;
        return Mathf.Clamp01(config.baseExtinguishSuccessChance + currentUpgrades.baseExtinguishSuccessBonus);
    }

    public float GetModifiedMedkitDuration()
    {
        var config = CrewActionConfig.Instance;
        if (config == null) return 3f;
        return Mathf.Max(0.5f, config.medkitDuration + currentUpgrades.medkitDurationBonus);
    }

    public float GetModifiedRepairKitDuration()
    {
        var config = CrewActionConfig.Instance;
        if (config == null) return 4f;
        return Mathf.Max(0.5f, config.repairKitDuration + currentUpgrades.repairKitDurationBonus);
    }

    public float GetModifiedFireExtinguisherDuration()
    {
        var config = CrewActionConfig.Instance;
        if (config == null) return 2f;
        return Mathf.Max(0.5f, config.fireExtinguisherDuration + currentUpgrades.fireExtinguisherDurationBonus);
    }

    public int GetModifiedRepairKitAmountMin()
    {
        var config = CrewActionConfig.Instance;
        if (config == null) return 30;
        return Mathf.Max(1, config.repairKitAmountMin + currentUpgrades.repairKitAmountMinBonus);
    }

    public int GetModifiedRepairKitAmountMax()
    {
        var config = CrewActionConfig.Instance;
        if (config == null) return 50;
        return Mathf.Max(1, config.repairKitAmountMax + currentUpgrades.repairKitAmountMaxBonus);
    }

    // ------------------------------------------------------------------
    // UPGRADE APPLICATION
    // ------------------------------------------------------------------

    /// <summary>
    /// Apply a named upgrade. Add more cases as you design your upgrade tree.
    /// </summary>
    public void UnlockUpgrade(string upgradeId)
    {
        switch (upgradeId)
        {
            // Medical upgrades
            case "medical_success_1":
                currentUpgrades.baseMedicalSuccessBonus += 0.05f; // +5%
                break;
            case "medical_success_2":
                currentUpgrades.baseMedicalSuccessBonus += 0.05f;
                break;
            case "medical_speed_1":
                currentUpgrades.baseMedicalDurationBonus -= 1f; // -1 second
                break;

            // Repair upgrades
            case "repair_success_1":
                currentUpgrades.baseRepairSuccessBonus += 0.05f;
                break;
            case "repair_success_2":
                currentUpgrades.baseRepairSuccessBonus += 0.05f;
                break;
            case "repair_speed_1":
                currentUpgrades.baseRepairDurationBonus -= 1f;
                break;
            case "repair_amount_1":
                currentUpgrades.baseRepairAmountMinBonus += 5;
                currentUpgrades.baseRepairAmountMaxBonus += 5;
                break;

            // Fire fighting upgrades
            case "extinguish_success_1":
                currentUpgrades.baseExtinguishSuccessBonus += 0.05f;
                break;
            case "extinguish_success_2":
                currentUpgrades.baseExtinguishSuccessBonus += 0.05f;
                break;
            case "extinguish_speed_1":
                currentUpgrades.baseExtinguishDurationBonus -= 1f;
                break;

            // Consumable upgrades
            case "medkit_speed_1":
                currentUpgrades.medkitDurationBonus -= 0.5f;
                break;
            case "repairkit_speed_1":
                currentUpgrades.repairKitDurationBonus -= 0.5f;
                break;
            case "repairkit_amount_1":
                currentUpgrades.repairKitAmountMinBonus += 10;
                currentUpgrades.repairKitAmountMaxBonus += 10;
                break;
            case "extinguisher_speed_1":
                currentUpgrades.fireExtinguisherDurationBonus -= 0.5f;
                break;

            default:
                Debug.LogWarning($"[UpgradeManager] Unknown upgrade ID: {upgradeId}");
                return;
        }

        Debug.Log($"[UpgradeManager] Unlocked upgrade: {upgradeId}");
        OnUpgradeUnlocked?.Invoke(upgradeId);
        SaveUpgrades();
    }

    /// <summary>
    /// Reset all upgrades (useful for new game or testing)
    /// </summary>
    [ContextMenu("Reset All Upgrades")]
    public void ResetAllUpgrades()
    {
        currentUpgrades = new ActionUpgrades();
        SaveUpgrades();
        Debug.Log("[UpgradeManager] All upgrades reset");
    }

    // ------------------------------------------------------------------
    // PERSISTENCE
    // ------------------------------------------------------------------

    private void SaveUpgrades()
    {
        // Medical
        PlayerPrefs.SetFloat("Upgrade_MedicalSuccessBonus", currentUpgrades.baseMedicalSuccessBonus);
        PlayerPrefs.SetFloat("Upgrade_MedicalDurationBonus", currentUpgrades.baseMedicalDurationBonus);
        
        // Repair
        PlayerPrefs.SetFloat("Upgrade_RepairSuccessBonus", currentUpgrades.baseRepairSuccessBonus);
        PlayerPrefs.SetFloat("Upgrade_RepairDurationBonus", currentUpgrades.baseRepairDurationBonus);
        PlayerPrefs.SetInt("Upgrade_RepairAmountMinBonus", currentUpgrades.baseRepairAmountMinBonus);
        PlayerPrefs.SetInt("Upgrade_RepairAmountMaxBonus", currentUpgrades.baseRepairAmountMaxBonus);
        
        // Extinguish
        PlayerPrefs.SetFloat("Upgrade_ExtinguishSuccessBonus", currentUpgrades.baseExtinguishSuccessBonus);
        PlayerPrefs.SetFloat("Upgrade_ExtinguishDurationBonus", currentUpgrades.baseExtinguishDurationBonus);
        
        // Consumables
        PlayerPrefs.SetFloat("Upgrade_MedkitDurationBonus", currentUpgrades.medkitDurationBonus);
        PlayerPrefs.SetFloat("Upgrade_RepairKitDurationBonus", currentUpgrades.repairKitDurationBonus);
        PlayerPrefs.SetFloat("Upgrade_FireExtinguisherDurationBonus", currentUpgrades.fireExtinguisherDurationBonus);
        PlayerPrefs.SetInt("Upgrade_RepairKitAmountMinBonus", currentUpgrades.repairKitAmountMinBonus);
        PlayerPrefs.SetInt("Upgrade_RepairKitAmountMaxBonus", currentUpgrades.repairKitAmountMaxBonus);
        
        PlayerPrefs.Save();
    }

    private void LoadUpgrades()
    {
        // Medical
        currentUpgrades.baseMedicalSuccessBonus = PlayerPrefs.GetFloat("Upgrade_MedicalSuccessBonus", 0f);
        currentUpgrades.baseMedicalDurationBonus = PlayerPrefs.GetFloat("Upgrade_MedicalDurationBonus", 0f);
        
        // Repair
        currentUpgrades.baseRepairSuccessBonus = PlayerPrefs.GetFloat("Upgrade_RepairSuccessBonus", 0f);
        currentUpgrades.baseRepairDurationBonus = PlayerPrefs.GetFloat("Upgrade_RepairDurationBonus", 0f);
        currentUpgrades.baseRepairAmountMinBonus = PlayerPrefs.GetInt("Upgrade_RepairAmountMinBonus", 0);
        currentUpgrades.baseRepairAmountMaxBonus = PlayerPrefs.GetInt("Upgrade_RepairAmountMaxBonus", 0);
        
        // Extinguish
        currentUpgrades.baseExtinguishSuccessBonus = PlayerPrefs.GetFloat("Upgrade_ExtinguishSuccessBonus", 0f);
        currentUpgrades.baseExtinguishDurationBonus = PlayerPrefs.GetFloat("Upgrade_ExtinguishDurationBonus", 0f);
        
        // Consumables
        currentUpgrades.medkitDurationBonus = PlayerPrefs.GetFloat("Upgrade_MedkitDurationBonus", 0f);
        currentUpgrades.repairKitDurationBonus = PlayerPrefs.GetFloat("Upgrade_RepairKitDurationBonus", 0f);
        currentUpgrades.fireExtinguisherDurationBonus = PlayerPrefs.GetFloat("Upgrade_FireExtinguisherDurationBonus", 0f);
        currentUpgrades.repairKitAmountMinBonus = PlayerPrefs.GetInt("Upgrade_RepairKitAmountMinBonus", 0);
        currentUpgrades.repairKitAmountMaxBonus = PlayerPrefs.GetInt("Upgrade_RepairKitAmountMaxBonus", 0);
    }

    // ------------------------------------------------------------------
    // DEBUG HELPERS
    // ------------------------------------------------------------------

    [ContextMenu("Test: Unlock Medical Success Upgrade")]
    private void TestMedicalUpgrade()
    {
        UnlockUpgrade("medical_success_1");
        Debug.Log($"Medical success now: {GetModifiedMedicalSuccess():P1}");
    }

    [ContextMenu("Test: Unlock Repair Amount Upgrade")]
    private void TestRepairUpgrade()
    {
        UnlockUpgrade("repair_amount_1");
        Debug.Log($"Repair amount now: {GetModifiedRepairAmountMin()}-{GetModifiedRepairAmountMax()}");
    }
}
