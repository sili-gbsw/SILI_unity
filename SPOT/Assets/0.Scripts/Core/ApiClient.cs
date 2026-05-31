using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[DisallowMultipleComponent]
public class ApiClient : MonoBehaviour
{
    [SerializeField] private string baseUrl       = "http://15.164.245.147:8000/api/v1";
    [SerializeField] private string lineId        = "LINE-DEFAULT";
    [SerializeField] private string materialCode  = "MILD";
    [SerializeField] private string electrodeShape = "C-TYPE";
    [SerializeField] private float  t1            = 0.8f;
    [SerializeField] private float  t2            = 1.2f;
    [SerializeField] private WeldJudgementEngine localEngine;

    public event Action<JudgementResult> OnResult;
    public event Action<string>          OnError;

    public void PostWeldEvent(WeldData data) => StartCoroutine(PostRoutine(data));

    private IEnumerator PostRoutine(WeldData data)
    {
        var payload = new WeldEventPayload
        {
            line_id         = lineId,
            part_id         = data.partId,
            point_id        = data.pointId,
            current_kA      = data.current / 1000f,
            weld_time_cycle = data.weldTime,
            force_kN        = data.force * 0.00981f,
            cumulative_hits = data.cumulativeHits,
            t1              = t1,
            t2              = t2,
            material_code   = materialCode,
            electrode_shape = electrodeShape,
            timestamp       = DateTimeOffset.FromUnixTimeMilliseconds(data.timestamp).ToString("o"),
        };

        byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));

        using var req = new UnityWebRequest($"{baseUrl}/weld-events", "POST");
        req.uploadHandler   = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            OnError?.Invoke($"API 오류: {req.responseCode} {req.error}");
            yield break;
        }

        var resp = JsonUtility.FromJson<ApiResponse>(req.downloadHandler.text);
        if (resp == null || !resp.success || resp.data == null)
        {
            OnError?.Invoke("API 응답 파싱 실패");
            yield break;
        }

        var j = resp.data.judgement;
        if (j == null || string.IsNullOrEmpty(j.status))
        {
            Debug.LogWarning("[ApiClient] judgement null — 로컬 엔진 폴백");
            if (localEngine != null) OnResult?.Invoke(localEngine.Judge(data));
            else                     OnError?.Invoke("판정 결과 없음 (로컬 엔진 미설정)");
            yield break;
        }

        var devs = j.deviations;
        OnResult?.Invoke(new JudgementResult
        {
            data   = data,
            status = ParseStatus(j.status),
            breakdown = new WeldScoreBreakdown
            {
                currentScore  = devs != null ? devs.current   : 0f,
                weldTimeScore = devs != null ? devs.weld_time  : 0f,
                forceScore    = devs != null ? devs.force      : 0f,
                wearScore     = devs != null ? devs.wear       : 0f,
                total         = j.score,
                weights       = new WeldScoreWeights
                {
                    current  = 0.35f,
                    weldTime = 0.25f,
                    force    = 0.25f,
                    wear     = 0.15f,
                },
            },
        });
    }

    private static WeldStatus ParseStatus(string s) => s switch
    {
        "NORMAL"  => WeldStatus.Normal,
        "CAUTION" => WeldStatus.Caution,
        "REJECT"  => WeldStatus.Reinspect,
        _         => WeldStatus.Normal,
    };

    [Serializable] private class WeldEventPayload
    {
        public string line_id;
        public string part_id;
        public string point_id;
        public float  current_kA;
        public float  weld_time_cycle;
        public float  force_kN;
        public int    cumulative_hits;
        public float  t1;
        public float  t2;
        public string material_code;
        public string electrode_shape;
        public string timestamp;
    }

    [Serializable] private class Deviations
    {
        public float current;
        public float weld_time;
        public float force;
        public float wear;
    }

    [Serializable] private class Judgement
    {
        public string     status;
        public float      score;
        public Deviations deviations;
    }

    [Serializable] private class EventData
    {
        public string   event_id;
        public string   part_id;
        public string   point_id;
        public Judgement judgement;
    }

    [Serializable] private class ApiResponse
    {
        public bool      success;
        public EventData data;
    }
}
