using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class JudgementResultUI : MonoBehaviour
{
    [SerializeField] private Slider scoreSlider;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Image statusPanel;

    private static readonly Color ColorNormal    = new Color(0.2f, 0.8f, 0.2f);
    private static readonly Color ColorCaution   = new Color(1f,   0.75f, 0f);
    private static readonly Color ColorReinspect = new Color(0.9f, 0.2f, 0.2f);
    private static readonly Color ColorIdle      = Color.gray;

    private void Awake() => ResetUI();
    private void OnEnable() => SimulatorEvents.OnJudged += Render;
    private void OnDisable() => SimulatorEvents.OnJudged -= Render;

    private void Render(JudgementResult r)
    {
        if (scoreSlider != null) scoreSlider.value = r.Score / 100f;
        if (scoreText != null)   scoreText.text = $"점수: {r.Score:F1}";

        var (label, color) = StyleFor(r.status);
        if (statusText != null)  statusText.text = $"상태: {label}";
        if (statusPanel != null) statusPanel.color = color;
    }

    private static (string label, Color color) StyleFor(WeldStatus status) => status switch
    {
        WeldStatus.Normal    => ("정상",     ColorNormal),
        WeldStatus.Caution   => ("주의",     ColorCaution),
        WeldStatus.Reinspect => ("재검권장", ColorReinspect),
        _                    => ("-",        ColorIdle),
    };

    private void ResetUI()
    {
        if (scoreSlider != null) scoreSlider.value = 0f;
        if (scoreText != null)   scoreText.text = "점수: -";
        if (statusText != null)  statusText.text = "상태: -";
        if (statusPanel != null) statusPanel.color = ColorIdle;
    }
}
