using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[DisallowMultipleComponent]
public class ApiClient : MonoBehaviour
{
    [SerializeField] private string baseUrl = "http://15.164.245.147:8000/api/v1";
    [SerializeField] private string lineId = "LINE-DEFAULT";
    [SerializeField] private string materialCode = "MILD";
    [SerializeField] private string electrodeShape = "C-TYPE";

    public event Action<JudgementResult> OnResult;
    public event Action<string> OnError;

    public void PostWeldEvent(WeldData data)
    {
        StartCoroutine(PostRoutine(data));
    }

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
            t1              = 0.8f,
            t2              = 1.2f,
            material_code   = materialCode,
            electrode_shape = electrodeShape,
            timestamp       = DateTimeOffset.UtcNow.ToString("o"),
        };

        string json = JsonUtility.ToJson(payload);
        byte[] body = Encoding.UTF8.GetBytes(json);

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

        // judgement가 null이면 (Phase 2 미구현) 로컬 판정 엔진으로 폴백
        WeldStatus status;
        float score;
        if (resp.data.judgement != null && !string.IsNullOrEmpty(resp.data.judgement.status))
        {
            status = ParseStatus(resp.data.judgement.status);
            score  = resp.data.judgement.score;
        }
        else
        {
            Debug.LogWarning("[ApiClient] judgement null — 로컬 폴백");
            yield break;
        }

        var result = new JudgementResult
        {
            data      = data,
            status    = status,
            breakdown = new WeldScoreBreakdown { total = score },
        };

        OnResult?.Invoke(result);
    }

    private static WeldStatus ParseStatus(string s) => s switch
    {
        "NORMAL"  => WeldStatus.Normal,
        "CAUTION" => WeldStatus.Caution,
        "REJECT"  => WeldStatus.Reinspect,
        _         => WeldStatus.Normal,
    };

    // ── JSON 직렬화 구조체 ──

    [Serializable]
    private class WeldEventPayload
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

    [Serializable]
    private class ApiResponse
    {
        public bool      success;
        public EventData data;
    }

    [Serializable]
    private class EventData
    {
        public string     event_id;
        public string     part_id;
        public string     point_id;
        public Judgement  judgement;
    }

    [Serializable]
    private class Judgement
    {
        public string status;
        public float  score;
    }
}
