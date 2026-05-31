using UnityEngine;

// 백엔드 판정 실패 시 로컬 폴백 엔진. 기준값은 0.8+1.2 MILD 스펙 기준.
[DisallowMultipleComponent]
public class WeldJudgementEngine : MonoBehaviour
{
    [Header("Reference Targets (0.8+1.2 MILD)")]
    [SerializeField] private float currentCenter      = 9500f;   // A  — backend center 9.5 kA
    [SerializeField] private float currentTolerance   = 1000f;   // ±1.0 kA
    [SerializeField] private float weldTimeCenter     = 12f;     // cycle — backend center 12.0
    [SerializeField] private float weldTimeTolerance  = 2f;      // ±2 cycle
    [SerializeField] private float forceCenter        = 265f;    // kgf — backend center 2.6 kN
    [SerializeField] private float forceTolerance     = 40f;     // ±40 kgf (~±0.4 kN)

    [Header("Wear Thresholds")]
    [SerializeField] private int wearCautionHits    = 1800;
    [SerializeField] private int wearReinspectHits  = 2200;

    [Header("Score Weights")]
    [SerializeField, Range(0f, 1f)] private float currentWeight  = 0.35f;
    [SerializeField, Range(0f, 1f)] private float weldTimeWeight = 0.25f;
    [SerializeField, Range(0f, 1f)] private float forceWeight    = 0.25f;
    [SerializeField, Range(0f, 1f)] private float wearWeight     = 0.15f;

    [Header("Status Thresholds")]
    [SerializeField, Range(0f, 100f)] private float cautionThreshold   = 30f;
    [SerializeField, Range(0f, 100f)] private float reinspectThreshold = 60f;

    public JudgementResult Judge(WeldData data)
    {
        var breakdown = BuildBreakdown(data);
        return new JudgementResult { data = data, breakdown = breakdown, status = Classify(breakdown.total) };
    }

    private WeldScoreBreakdown BuildBreakdown(WeldData d)
    {
        float cScore = Deviation(d.current,  currentCenter,  currentTolerance)  * currentWeight;
        float tScore = Deviation(d.weldTime, weldTimeCenter, weldTimeTolerance) * weldTimeWeight;
        float fScore = Deviation(d.force,    forceCenter,    forceTolerance)    * forceWeight;
        float wScore = WearScore(d.cumulativeHits)                              * wearWeight;

        return new WeldScoreBreakdown
        {
            currentScore  = cScore,
            weldTimeScore = tScore,
            forceScore    = fScore,
            wearScore     = wScore,
            total         = Mathf.Clamp(cScore + tScore + fScore + wScore, 0f, 100f),
            weights = new WeldScoreWeights
            {
                current  = currentWeight,
                weldTime = weldTimeWeight,
                force    = forceWeight,
                wear     = wearWeight,
            },
        };
    }

    private WeldStatus Classify(float score)
    {
        if (score <= cautionThreshold)   return WeldStatus.Normal;
        if (score <= reinspectThreshold) return WeldStatus.Caution;
        return WeldStatus.Reinspect;
    }

    private static float Deviation(float measured, float center, float tolerance)
    {
        if (tolerance <= 0f) return 0f;
        return Mathf.Clamp(Mathf.Abs(measured - center) / tolerance * 100f, 0f, 100f);
    }

    private float WearScore(int hits)
    {
        if (hits <= wearCautionHits)    return 0f;
        if (hits <= wearReinspectHits)  return 50f;
        return 100f;
    }

    private void OnValidate()
    {
        wearReinspectHits = Mathf.Max(wearReinspectHits, wearCautionHits);
        cautionThreshold  = Mathf.Min(cautionThreshold, reinspectThreshold);

        float sum = currentWeight + weldTimeWeight + forceWeight + wearWeight;
        if (sum > 0f && !Mathf.Approximately(sum, 1f))
            Debug.LogWarning($"{nameof(WeldJudgementEngine)}: weights sum = {sum:F2} (expected 1.0)", this);
    }
}
