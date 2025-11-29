using UnityEngine;
using UnityEditor;

/// <summary>
/// Diagnostic tool to check UI Toolkit text rendering configuration.
/// This helps diagnose NullReferenceException errors in the Inspector.
/// </summary>
public class UIToolkitDiagnostics
{
    [MenuItem("Tools/Diagnostics/Check UI Toolkit Text Settings")]
    public static void CheckUIToolkitSettings()
    {
        Debug.Log("=== UI Toolkit Diagnostics ===");
        
        // Check TMP Settings
        string[] tmpGuids = AssetDatabase.FindAssets("t:TMP_Settings");
        if (tmpGuids.Length == 0)
        {
            Debug.LogError("TMP_Settings asset not found! This can cause Inspector rendering issues.");
            Debug.LogError("Fix: Window > TextMeshPro > Import TMP Essential Resources");
        }
        else
        {
            foreach (var guid in tmpGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var tmpSettings = AssetDatabase.LoadAssetAtPath<TMPro.TMP_Settings>(path);
                if (tmpSettings != null)
                {
                    Debug.Log($"✓ TMP Settings found: {tmpSettings.name} at {path}");
                }
            }
        }
        
        // Check for PanelSettings assets
        string[] guids = AssetDatabase.FindAssets("t:PanelSettings");
        if (guids.Length == 0)
        {
            Debug.LogWarning("No PanelSettings assets found. This may cause UI Toolkit rendering issues.");
            Debug.Log("Consider creating a PanelSettings asset via: Assets > Create > UI Toolkit > Panel Settings Asset");
        }
        else
        {
            Debug.Log($"✓ Found {guids.Length} PanelSettings asset(s)");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Debug.Log($"  - {path}");
            }
        }
        
        Debug.Log("=== Diagnostics Complete ===");
        Debug.Log("If you're still seeing NullReferenceException errors in the Inspector:");
        Debug.Log("  1. Try: Tools > Diagnostics > Force Reimport TextMeshPro");
        Debug.Log("  2. Restart Unity Editor");
        Debug.Log("  3. Delete Library/ folder and reopen Unity (nuclear option)");
    }
    
    [MenuItem("Tools/Diagnostics/Force Reimport TextMeshPro")]
    public static void ForceReimportTMP()
    {
        Debug.Log("Force reimporting TextMeshPro assets...");
        AssetDatabase.ImportAsset("Assets/TextMesh Pro", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        Debug.Log("Reimport complete. Try reopening the Inspector.");
    }
}
