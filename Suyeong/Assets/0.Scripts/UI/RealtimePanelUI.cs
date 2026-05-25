using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class RealtimePanelUI : MonoBehaviour
{
    [Header("Realtime Data")]
    [SerializeField] private TextMeshProUGUI currentText;
    [SerializeField] private TextMeshProUGUI weldTimeText;
    [SerializeField] private TextMeshProUGUI forceText;
    [SerializeField] private TextMeshProUGUI hitsText;
    [SerializeField] private TextMeshProUGUI partIdText;

    [Header("Score Breakdown")]
    [SerializeField] private TextMeshProUGUI calcCurrentText;
    [SerializeField] private TextMeshProUGUI calcTimeText;
    [SerializeField] private TextMeshProUGUI calcForceText;
    [SerializeField] private TextMeshProUGUI calcHitsText;
    [SerializeField] private TextMeshProUGUI calcTotalText;

    private void Awake() => ResetPanel();
    private void OnEnable() => SimulatorEvents.OnJudged += Render;
    private void OnDisable() => SimulatorEvents.OnJudged -= Render;

    private void Render(JudgementResult r)
    {
        var d = r.data;
        SetText(currentText,  $"전류:    {d.current:F0} A");
        SetText(weldTimeText, $"시간:    {d.weldTime:F1} cycle");
        SetText(forceText,    $"가압력:  {d.force:F0} kgf");
        SetText(hitsText,     $"누적타수: {d.cumulativeHits} 타");
        SetText(partIdText,   $"부품ID:  {d.partId}");

        var b = r.breakdown;
        SetText(calcCurrentText, FormatAxis("전류 이탈", b.currentScore,  b.weights.current));
        SetText(calcTimeText,    FormatAxis("시간 이탈", b.weldTimeScore, b.weights.weldTime));
        SetText(calcForceText,   FormatAxis("가압 이탈", b.forceScore,    b.weights.force));
        SetText(calcHitsText,    FormatAxis("마모 이탈", b.wearScore,     b.weights.wear));
        SetText(calcTotalText,   $"총 점수:    {b.total:F1}점");
    }

    private static void SetText(TextMeshProUGUI label, string text)
    {
        if (label != null) label.text = text;
    }

    private static string FormatAxis(string label, float score, float weight) =>
        $"{label}:  {score:F1}점  (가중치 {weight * 100f:F0}%)";

    private void ResetPanel()
    {
        SetText(currentText,  "전류:    - A");
        SetText(weldTimeText, "시간:    - cycle");
        SetText(forceText,    "가압력:  - kgf");
        SetText(hitsText,     "누적타수: - 타");
        SetText(partIdText,   "부품ID:  -");

        SetText(calcCurrentText, "전류 이탈:  -점");
        SetText(calcTimeText,    "시간 이탈:  -점");
        SetText(calcForceText,   "가압 이탈:  -점");
        SetText(calcHitsText,    "마모 이탈:  -점");
        SetText(calcTotalText,   "총 점수:    -점");
    }
}
