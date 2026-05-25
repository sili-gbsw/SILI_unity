public struct WeldData
{
    public string partId;
    public string pointId;
    public float current;
    public float weldTime;
    public float force;
    public int cumulativeHits;
    public long timestamp;
}

public enum WeldStatus
{
    Normal,
    Caution,
    Reinspect
}

public struct WeldScoreWeights
{
    public float current;
    public float weldTime;
    public float force;
    public float wear;
}

public struct WeldScoreBreakdown
{
    public float currentScore;
    public float weldTimeScore;
    public float forceScore;
    public float wearScore;
    public float total;
    public WeldScoreWeights weights;
}

public struct JudgementResult
{
    public WeldData data;
    public WeldScoreBreakdown breakdown;
    public WeldStatus status;

    public float Score => breakdown.total;
}
