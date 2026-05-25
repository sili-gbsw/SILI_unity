using UnityEngine;

[RequireComponent(typeof(WeldJudgementEngine))]
[DisallowMultipleComponent]
public class SimulatorManager : MonoBehaviour
{
    private WeldJudgementEngine engine;

    private void Awake()
    {
        engine = GetComponent<WeldJudgementEngine>();
    }

    private void OnEnable()
    {
        WeldDataGenerator.OnDataGenerated += HandleData;
    }

    private void OnDisable()
    {
        WeldDataGenerator.OnDataGenerated -= HandleData;
    }

    private void HandleData(WeldData data)
    {
        SimulatorEvents.RaiseJudged(engine.Judge(data));
    }
}
