using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Modal popup for choosing between base action or using a consumable item.
/// Shows comparison of duration, success chance, and effect strength.
/// </summary>
public class ActionConfirmationPopup : MonoBehaviour
{
    public static ActionConfirmationPopup Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI baseActionText;
    [SerializeField] private TextMeshProUGUI consumableActionText;
    [SerializeField] private Button baseActionButton;
    [SerializeField] private Button consumableButton;
    [SerializeField] private Button cancelButton;

    private Action<bool> onConfirmCallback; // bool parameter: true = use consumable, false = use base

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (baseActionButton != null)
            baseActionButton.onClick.AddListener(() => OnChoice(useConsumable: false));
        if (consumableButton != null)
            consumableButton.onClick.AddListener(() => OnChoice(useConsumable: true));
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancel);

        HideImmediate();
    }

    /// <summary>
    /// Show the confirmation popup with base vs consumable options.
    /// </summary>
    public void Show(
        string actionTitle,
        float baseDuration,
        float baseSuccessChance,
        string baseEffectText,
        float consumableDuration,
        float consumableSuccessChance,
        string consumableEffectText,
        int consumablesAvailable,
        Action<bool> onConfirm)
    {
        onConfirmCallback = onConfirm;

        if (titleText != null)
            titleText.text = actionTitle;

        if (baseActionText != null)
        {
            baseActionText.text = $"<b>Base Action</b>\n" +
                                   $"Duration: {baseDuration:0.0}s\n" +
                                   $"Success: {baseSuccessChance * 100:0}%\n" +
                                   $"{baseEffectText}";
        }

        if (consumableActionText != null)
        {
            consumableActionText.text = $"<b>Use Item</b> ({consumablesAvailable} left)\n" +
                                         $"Duration: {consumableDuration:0.0}s\n" +
                                         $"Success: {consumableSuccessChance * 100:0}%\n" +
                                         $"{consumableEffectText}";
        }

        if (panel != null)
            panel.SetActive(true);
    }

    private void OnChoice(bool useConsumable)
    {
        HideImmediate();
        onConfirmCallback?.Invoke(useConsumable);
        onConfirmCallback = null;
    }

    private void OnCancel()
    {
        HideImmediate();
        onConfirmCallback = null;
    }

    private void HideImmediate()
    {
        if (panel != null)
            panel.SetActive(false);
    }
}
