using System;
using UnityEngine;

[DisallowMultipleComponent]
public class WeldDataGenerator : MonoBehaviour
{
    [Serializable]
    public class Scenario
    {
        public string label = "Unnamed";
        public Vector2 currentRange  = new Vector2(9500f, 10500f);
        public Vector2 weldTimeRange = new Vector2(13f, 15f);
        public Vector2 forceRange    = new Vector2(280f, 320f);
        public Vector2Int hitsRange  = new Vector2Int(300, 800);
    }

    public static event Action<WeldData> OnDataGenerated;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => OnDataGenerated = null;

    [SerializeField] private Scenario[] scenarios = new[]
    {
        new Scenario { label = "A 정상", currentRange = new Vector2(9300f, 9700f), weldTimeRange = new Vector2(11.5f, 12.5f), forceRange = new Vector2(255f, 275f), hitsRange = new Vector2Int(300, 600) },
        new Scenario { label = "B 전류저하",        currentRange  = new Vector2(8200f, 8800f) },
        new Scenario { label = "C 통전시간 부족",   weldTimeRange = new Vector2(9f, 11f) },
        new Scenario { label = "D 가압력 부족",     forceRange    = new Vector2(200f, 240f) },
        new Scenario { label = "E 전극마모",        hitsRange     = new Vector2Int(2300, 2800) },
        new Scenario
        {
            label = "F 복합 이상",
            currentRange = new Vector2(7800f, 8800f),
            forceRange   = new Vector2(200f, 250f),
            hitsRange    = new Vector2Int(2000, 2400),
        },
    };

    [SerializeField] private int[] autoCycleOrder = { 0, 0, 0, 0, 0, 0, 1, 2, 3, 5 };
    [SerializeField] private bool startInAutoCycle = true;

    private int partCounter;
    private int cycleIndex;
    private int selectedScenario;
    private bool autoCycleMode;

    public int ScenarioCount => scenarios?.Length ?? 0;
    public bool AutoCycleMode => autoCycleMode;
    public int SelectedScenario => selectedScenario;

    public string GetScenarioLabel(int index) =>
        (scenarios != null && index >= 0 && index < scenarios.Length)
            ? scenarios[index].label
            : string.Empty;

    private void Awake()
    {
        autoCycleMode = startInAutoCycle;
        ResetCycle();
    }

    public void ResetCycle()
    {
        partCounter = 0;
        cycleIndex = 0;
    }

    public void SelectScenario(int index)
    {
        if (scenarios == null || index < 0 || index >= scenarios.Length) return;
        selectedScenario = index;
        autoCycleMode = false;
    }

    public void EnableAutoCycle()
    {
        autoCycleMode = true;
        cycleIndex = 0;
    }

    public void TriggerJudge()
    {
        int index = ResolveScenarioIndex();
        if (index < 0) return;

        partCounter++;
        OnDataGenerated?.Invoke(BuildData(index));
    }

    private int ResolveScenarioIndex()
    {
        if (scenarios == null || scenarios.Length == 0) return -1;

        if (autoCycleMode)
        {
            if (autoCycleOrder == null || autoCycleOrder.Length == 0) return 0;
            int raw = autoCycleOrder[cycleIndex];
            cycleIndex = (cycleIndex + 1) % autoCycleOrder.Length;
            return Mathf.Clamp(raw, 0, scenarios.Length - 1);
        }

        return Mathf.Clamp(selectedScenario, 0, scenarios.Length - 1);
    }

    // 백엔드에 등록된 part_id 목록
    private static readonly string[] RegisteredPartIds = { "BODY-0042", "TEST-PHASE3", "BODY-3294" };

    private WeldData BuildData(int index)
    {
        var s = scenarios[index];
        string partId = RegisteredPartIds[partCounter % RegisteredPartIds.Length];
        return new WeldData
        {
            partId          = partId,
            pointId         = $"P-{(index + 1):D3}",
            current         = UnityEngine.Random.Range(s.currentRange.x,  s.currentRange.y),
            weldTime        = UnityEngine.Random.Range(s.weldTimeRange.x, s.weldTimeRange.y),
            force           = UnityEngine.Random.Range(s.forceRange.x,    s.forceRange.y),
            cumulativeHits  = UnityEngine.Random.Range(s.hitsRange.x,     s.hitsRange.y + 1),
            timestamp       = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };
    }
}
