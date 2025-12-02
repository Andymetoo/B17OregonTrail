using UnityEngine;
using System.Collections.Generic;

public class EventTriggerManager : MonoBehaviour
{
    public static EventTriggerManager Instance { get; private set; }

    [Header("Fallback Inline Events (if no library assets)")]
    public List<GameEvent> InlineEvents = new List<GameEvent>();

    [Header("Random Flavor Event Settings")] 
    [Tooltip("Minimum seconds between random flavor events.")] public float minFlavorInterval = 45f;
    [Tooltip("Maximum seconds between random flavor events.")] public float maxFlavorInterval = 110f;
    [Tooltip("Chance to actually fire a flavor event when interval elapses.")] [Range(0f,1f)] public float flavorEventChance = 0.7f;

    private float _timer;
    private float _nextInterval;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        SampleNextInterval();
        EnsureTestEvents();
    }

    private void SampleNextInterval()
    {
        _nextInterval = Random.Range(minFlavorInterval, maxFlavorInterval);
    }

    private void EnsureTestEvents()
    {
        if (InlineEvents.Count == 0)
        {
            // Create three ephemeral test events (not assets) if list empty
            for (int i = 1; i <= 3; i++)
            {
                var evt = ScriptableObject.CreateInstance<GameEvent>();
                evt.Id = "TestEvent" + i;
                evt.Title = "Test Event " + i;
                evt.Description = i == 1 ? "Minor turbulence rattles the crew." : (i == 2 ? "Radio crackle: intercepted chatter." : "Oil leak scare—engineers double check gauges.");
                evt.DisplayColor = i == 3 ? new Color(1f,0.6f,0.1f) : Color.cyan;
                evt.FuelDelta = i == 2 ? -2f : 0f;
                evt.RandomSectionDamage = i == 3 ? 5 : 0;
                evt.FireChance = 0f;
                evt.CrewInjuryChance = i == 1 ? 0.1f : 0f;
                InlineEvents.Add(evt);
            }
        }
    }

    public void Tick(float deltaTime)
    {
        if (MissionManager.Instance == null || !MissionManager.Instance.IsTravelling) return;
        
        // Only trigger random flavor events during Cruise phase
        if (ChaosSimulator.Instance != null && ChaosSimulator.Instance.IsInHazardPhase)
        {
            return; // Skip random events during Flak/Fighter combat
        }
        
        _timer += deltaTime;
        if (_timer >= _nextInterval)
        {
            _timer = 0f;
            SampleNextInterval();
            if (InlineEvents.Count > 0 && Random.value <= flavorEventChance)
            {
                var evt = PickWeighted(InlineEvents);
                TriggerEvent(evt);
            }
        }
    }

    public void TriggerEvent(GameEvent evt)
    {
        if (evt == null) return;
        EventLogUI.Instance?.Log(evt.Title, evt.DisplayColor);
        EventPopupUI.Instance?.Show(evt, pause:true);  // Changed to pause:true for Oregon Trail style
    }

    [ContextMenu("Force Random Flavor Event")]
    private void ForceRandomFlavorEvent()
    {
        if (InlineEvents.Count == 0) return;
        TriggerEvent(PickWeighted(InlineEvents));
    }

    private GameEvent PickWeighted(List<GameEvent> list)
    {
        if (list == null || list.Count == 0) return null;
        float sum = 0f;
        foreach (var e in list) { if (e != null) sum += Mathf.Max(0f, e.Weight); }
        if (sum <= 0f) return list[Random.Range(0, list.Count)];
        float r = Random.Range(0f, sum);
        float acc = 0f;
        foreach (var e in list)
        {
            if (e == null) continue;
            acc += Mathf.Max(0f, e.Weight);
            if (r <= acc) return e;
        }
        return list[list.Count - 1];
    }
}
