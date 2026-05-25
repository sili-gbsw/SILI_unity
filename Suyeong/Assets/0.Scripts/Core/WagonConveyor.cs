using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class WagonConveyor : MonoBehaviour
{
    public enum State { Idle, MovingToWeld, Welding, MovingToExit, Resetting }

    [Header("Wagon")]
    [SerializeField] private Transform wagon;

    [Header("Waypoints")]
    [SerializeField] private Transform entryPoint;
    [SerializeField] private Transform weldPoint;
    [SerializeField] private Transform exitPoint;

    [Header("Motion")]
    [SerializeField, Min(0.01f)] private float travelSpeed = 2.5f;
    [SerializeField, Min(0f)] private float weldDwellSeconds = 1.5f;
    [SerializeField, Min(0f)] private float exitDwellSeconds = 0.4f;
    [SerializeField, Min(0f)] private float startupDelaySeconds = 0.25f;
    [SerializeField, Min(0f)] private float arriveEpsilon = 0.02f;

    [Header("Particle")]
    [SerializeField] private ParticleSystem weldParticle;
    [SerializeField, Min(0f)] private float particleLeadSeconds = 0f;

    [Header("Judgement")]
    [SerializeField] private WeldDataGenerator generator;
    [SerializeField] private bool resetGeneratorOnStart = true;

    [Header("Lifecycle")]
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool loop = true;

    private Coroutine routine;
    private State state = State.Idle;
    public State CurrentState => state;

    private void Awake()
    {
        PrepareParticle();
        ResetWagonState();
    }

    private void OnEnable()
    {
        ResetWagonState();
        if (autoStart) StartCycle();
    }

    private void OnDisable()
    {
        StopCycle();
    }

    public void StartCycle()
    {
        if (!ValidateReferences()) return;
        StopCycle();
        ResetWagonState();
        if (resetGeneratorOnStart && generator != null) generator.ResetCycle();
        routine = StartCoroutine(RunCycle());
    }

    public void StopCycle()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        StopParticleHard();
        state = State.Idle;
    }

    private void ResetWagonState()
    {
        SnapTo(entryPoint);
        StopParticleHard();
        state = State.Idle;
    }

    private void PrepareParticle()
    {
        if (weldParticle == null) return;
        var main = weldParticle.main;
        main.playOnAwake = false;
        weldParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        weldParticle.Clear(true);
        foreach (var child in weldParticle.GetComponentsInChildren<ParticleSystem>(includeInactive: true))
        {
            var m = child.main;
            m.playOnAwake = false;
            child.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            child.Clear(true);
        }
    }

    private IEnumerator RunCycle()
    {
        if (startupDelaySeconds > 0f) yield return new WaitForSeconds(startupDelaySeconds);

        while (true)
        {
            state = State.MovingToWeld;
            yield return MoveTo(weldPoint);

            state = State.Welding;
            if (particleLeadSeconds > 0f) yield return new WaitForSeconds(particleLeadSeconds);
            PlayParticleOnce();
            if (generator != null) generator.TriggerJudge();
            yield return WaitForParticle(weldDwellSeconds);

            state = State.MovingToExit;
            yield return MoveTo(exitPoint);

            state = State.Resetting;
            if (exitDwellSeconds > 0f) yield return new WaitForSeconds(exitDwellSeconds);

            if (!loop) break;
            SnapTo(entryPoint);
        }

        state = State.Idle;
        routine = null;
    }

    private IEnumerator MoveTo(Transform target)
    {
        if (wagon == null || target == null) yield break;

        while (true)
        {
            Vector3 to = target.position;
            Vector3 from = wagon.position;
            float remaining = Vector3.Distance(from, to);
            if (remaining <= arriveEpsilon)
            {
                wagon.position = to;
                yield break;
            }

            float step = travelSpeed * Time.deltaTime;
            wagon.position = Vector3.MoveTowards(from, to, step);
            yield return null;
        }
    }

    private void SnapTo(Transform target)
    {
        if (wagon == null || target == null) return;
        wagon.SetPositionAndRotation(target.position, target.rotation);
    }

    private void PlayParticleOnce()
    {
        if (weldParticle == null) return;
        weldParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        weldParticle.Clear(true);
        weldParticle.Play(true);
    }

    private void StopParticleHard()
    {
        if (weldParticle == null) return;
        weldParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        weldParticle.Clear(true);
    }

    private IEnumerator WaitForParticle(float minSeconds)
    {
        float elapsed = 0f;
        while (elapsed < minSeconds)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (weldParticle != null)
        {
            while (weldParticle.IsAlive(true) && weldParticle.isEmitting)
                yield return null;
        }
    }

    private bool ValidateReferences()
    {
        if (wagon == null)      { Debug.LogError($"{nameof(WagonConveyor)}: wagon transform missing.", this);  return false; }
        if (entryPoint == null) { Debug.LogError($"{nameof(WagonConveyor)}: entryPoint missing.", this);       return false; }
        if (weldPoint == null)  { Debug.LogError($"{nameof(WagonConveyor)}: weldPoint missing.", this);        return false; }
        if (exitPoint == null)  { Debug.LogError($"{nameof(WagonConveyor)}: exitPoint missing.", this);        return false; }
        if (generator == null)  { Debug.LogWarning($"{nameof(WagonConveyor)}: generator missing — judge skipped.", this); }
        return true;
    }

    private void OnDrawGizmos()
    {
        if (entryPoint != null && weldPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(entryPoint.position, weldPoint.position);
            Gizmos.DrawSphere(entryPoint.position, 0.08f);
        }
        if (weldPoint != null && exitPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(weldPoint.position, exitPoint.position);
            Gizmos.DrawSphere(weldPoint.position, 0.08f);
            Gizmos.DrawSphere(exitPoint.position, 0.08f);
        }
    }
}
