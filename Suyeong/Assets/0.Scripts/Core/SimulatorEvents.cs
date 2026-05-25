using System;
using UnityEngine;

public static class SimulatorEvents
{
    public static event Action<JudgementResult> OnJudged;

    internal static void RaiseJudged(JudgementResult result) => OnJudged?.Invoke(result);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => OnJudged = null;
}
