using System;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class ReinspectQueueUI : MonoBehaviour
{
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField, Min(1)] private int maxEntries = 10;

    private static readonly Color ReinspectColor = new Color(0.9f, 0.2f, 0.2f);

    private void OnEnable() => SimulatorEvents.OnJudged += Render;
    private void OnDisable() => SimulatorEvents.OnJudged -= Render;

    private void Render(JudgementResult r)
    {
        if (r.status != WeldStatus.Reinspect) return;
        if (entryPrefab == null || contentParent == null) return;

        TrimToCapacity();
        AppendEntry(r.data);
    }

    private void TrimToCapacity()
    {
        int overflow = contentParent.childCount - (maxEntries - 1);
        for (int i = 0; i < overflow; i++)
        {
            var child = contentParent.GetChild(0);
            child.SetParent(null, worldPositionStays: false);
            Destroy(child.gameObject);
        }
    }

    private void AppendEntry(WeldData data)
    {
        var entry = Instantiate(entryPrefab, contentParent);
        var label = entry.GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
        if (label == null) return;

        var time = DateTimeOffset.FromUnixTimeMilliseconds(data.timestamp).LocalDateTime;
        label.text  = $"{data.partId}   재검권장   {time:HH:mm:ss}";
        label.color = ReinspectColor;
    }
}
