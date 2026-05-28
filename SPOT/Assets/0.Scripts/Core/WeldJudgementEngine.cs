using UnityEngine;

[DisallowMultipleComponent]
public class WeldJudgementEngine : MonoBehaviour
{
    [Header("Reference Targets")]
    [SerializeField] private float currentCenter = 10000f;
    [SerializeField] private float currentTolerance = 1000f;
    [SerializeField] private float weldTimeCenter = 14f;
    [SerializeField] private float weldTimeTolerance = 2f;
    [SerializeField] private float forceCenter = 300f;
    [SerializeField] private float forceTolerance = 50f;

    [Header("Wear Thresholds")]
    [SerializeField] private int wearCautionHits = 1800;
    [SerializeField] private int wearReinspectHits = 2200;

    [Header("Score Weights")]
    [SerializeField, Range(0f, 1f)] private float currentWeight = 0.35f;
    [SerializeField, Range(0f, 1f)] private float weldTimeWeight = 0.25f;
    [SerializeField, Range(0f, 1f)] private float forceWeight = 0.25f;
    [SerializeField, Range(0f, 1f)] private float wearWeight = 0.15f;

    [Header("Status Thresholds")]
    [SerializeField, Range(0f, 100f)] private float cautionThreshold = 30f;
    [SerializeField, Range(0f, 100f)] private float reinspectThreshold = 60f;

    public JudgementResult Judge(WeldData data)
    {
        var breakdown = BuildBreakdown(data);
        return new JudgementResult
        {
            data = data,
            breakdown = breakdown,
            status = Classify(breakdown.total),
        };
    }

    private WeldScoreBreakdown BuildBreakdown(WeldData d)
    {
        float currentDev  = Deviation(d.current,  currentCenter,  currentTolerance);
        float weldTimeDev = Deviation(d.weldTime, weldTimeCenter, weldTimeTolerance);
        float forceDev    = Deviation(d.force,    forceCenter,    forceTolerance);
        float wearDev     = WearScore(d.cumulativeHits);

        float currentScore  = currentDev  * currentWeight;
        float weldTimeScore = weldTimeDev * weldTimeWeight;
        float forceScore    = forceDev    * forceWeight;
        float wearScore     = wearDev     * wearWeight;
        float total = Mathf.Clamp(currentScore + weldTimeScore + forceScore + wearScore, 0f, 100f);

        return new WeldScoreBreakdown
        {
            currentScore  = currentScore,
            weldTimeScore = weldTimeScore,
            forceScore    = forceScore,
            wearScore     = wearScore,
            total         = total,
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
        if (score <= cautionThreshold) return WeldStatus.Normal;
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
        if (hits <= wearCautionHits) return 0f;
        if (hits <= wearReinspectHits) return 50f;
        return 100f;
    }

    private void OnValidate()
    {
        wearReinspectHits = Mathf.Max(wearReinspectHits, wearCautionHits);
        cautionThreshold  = Mathf.Min(cautionThreshold, reinspectThreshold);

        float sum = currentWeight + weldTimeWeight + forceWeight + wearWeight;
        if (sum > 0f && !Mathf.Approximately(sum, 1f))
            Debug.LogWarning(
                $"{nameof(WeldJudgementEngine)}: weights sum = {sum:F2}, expected 1.0", this);
    }
}
