using UnityEngine;

[RequireComponent(typeof(ApiClient))]
[DisallowMultipleComponent]
public class SimulatorManager : MonoBehaviour
{
    private ApiClient apiClient;

    private void Awake()
    {
        apiClient = GetComponent<ApiClient>();
        apiClient.OnResult += result => SimulatorEvents.RaiseJudged(result);
        apiClient.OnError  += err   => Debug.LogWarning($"[SimulatorManager] {err}");
    }

    private void OnEnable()  => WeldDataGenerator.OnDataGenerated += apiClient.PostWeldEvent;
    private void OnDisable() => WeldDataGenerator.OnDataGenerated -= apiClient.PostWeldEvent;
}
