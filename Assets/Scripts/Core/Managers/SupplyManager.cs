using System;
using UnityEngine;

/// <summary>
/// Manages consumable supplies and equipment.
/// Supports the manual action system where players must use supplies to handle emergencies.
/// </summary>
[Serializable]
public class SupplyInventory
{
    [Header("Medical Supplies")]
    public int medKits = 3;
    public int morphine = 2;
    
    [Header("Damage Control")]  
    public int fireExtinguishers = 4;
    public int repairKits = 3;
    
    [Header("Ammunition")]
    public int machineGunAmmo = 1000; // Rounds remaining
    public int flares = 6;
    
    [Header("Oxygen/Emergency")]
    public int oxygenBottles = 2;
    public int emergencyRations = 5;
    
    public bool TryConsume(SupplyType type, int amount = 1)
    {
        switch (type)
        {
            case SupplyType.MedKit:
                if (medKits >= amount) { medKits -= amount; return true; }
                break;
            case SupplyType.Morphine:
                if (morphine >= amount) { morphine -= amount; return true; }
                break;
            case SupplyType.FireExtinguisher:
                if (fireExtinguishers >= amount) { fireExtinguishers -= amount; return true; }
                break;
            case SupplyType.RepairKit:
                if (repairKits >= amount) { repairKits -= amount; return true; }
                break;
            case SupplyType.Flares:
                if (flares >= amount) { flares -= amount; return true; }
                break;
            case SupplyType.OxygenBottle:
                if (oxygenBottles >= amount) { oxygenBottles -= amount; return true; }
                break;
        }
        return false;
    }
    
    public int GetCount(SupplyType type)
    {
        return type switch
        {
            SupplyType.MedKit => medKits,
            SupplyType.Morphine => morphine,
            SupplyType.FireExtinguisher => fireExtinguishers,
            SupplyType.RepairKit => repairKits,
            SupplyType.Flares => flares,
            SupplyType.OxygenBottle => oxygenBottles,
            _ => 0
        };
    }
}

public enum SupplyType
{
    MedKit,
    Morphine,
    FireExtinguisher,
    RepairKit,
    Flares,
    OxygenBottle
}

public class SupplyManager : MonoBehaviour
{
    public static SupplyManager Instance { get; private set; }
    
    [SerializeField] private SupplyInventory currentInventory = new SupplyInventory();
    
    public SupplyInventory Inventory => currentInventory;
    
    // Events
    public event Action<SupplyType, int> OnSupplyUsed; // type, remaining count
    public event Action<SupplyType> OnSupplyDepleted; // type
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    public bool TryUseSupply(SupplyType type, int amount = 1)
    {
        bool success = currentInventory.TryConsume(type, amount);
        
        if (success)
        {
            int remaining = currentInventory.GetCount(type);
            OnSupplyUsed?.Invoke(type, remaining);
            
            if (remaining == 0)
            {
                OnSupplyDepleted?.Invoke(type);
            }
            
            Debug.Log($"[Supply] Used {amount}x {type}, {remaining} remaining");
        }
        else
        {
            Debug.Log($"[Supply] Insufficient {type} (requested {amount})");
        }
        
        return success;
    }
    
    public int GetSupplyCount(SupplyType type)
    {
        return currentInventory.GetCount(type);
    }
    
    public bool HasSupply(SupplyType type, int amount = 1)
    {
        return currentInventory.GetCount(type) >= amount;
    }
    
    /// <summary>
    /// For debugging/testing
    /// </summary>
    [ContextMenu("Use Med Kit")]
    private void DebugUseMedKit() => TryUseSupply(SupplyType.MedKit);
    
    [ContextMenu("Use Fire Extinguisher")]
    private void DebugUseExtinguisher() => TryUseSupply(SupplyType.FireExtinguisher);
}