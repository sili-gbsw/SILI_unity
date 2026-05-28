using System.Collections;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace UnityFactorySceneHDRP
{
    [ExecuteAlways]
    public class CustomSplineAnimate : MonoBehaviour
    {
        [System.Serializable]
        private struct StopPoint
        {
            public float time;
            public float duration;
            public Animation robotArmAnimation;

            [Header("Weld Effects")]
            public ParticleSystem spark;
            public GameObject bead;
            public float sparkStartDelay;
            public float sparkEndDelay;
            public float beadVisibleDuration;
        }

        [SerializeField] private SplineContainer _normalPath;
        [SerializeField] private SplineContainer _reinspectPath;
        [SerializeField] private WeldDataGenerator _generator;
        [SerializeField] private float _duration;
        [SerializeField] private float _startOffset;
        [SerializeField] private StopPoint[] _stopPoints;

        [Header("Defaults")]
        [SerializeField, Min(0f)] private float _defaultBeadVisibleDuration = 15f;
        [SerializeField, Min(0.1f)] private float _sparkFallbackDuration = 1f;

        [Header("Preview")]
        [SerializeField, Range(0, 1)] private float _previewTime;

        private SplineContainer _spline;
        private Transform _transform;
        private float _time = 0;
        private WeldStatus _lastStatus = WeldStatus.Normal;
        private bool _waitingForResult = false;
        private Coroutine[] _weldRoutines;
        private Coroutine[] _beadHideRoutines;

        private void Awake()
        {
            if (Application.isPlaying)
            {
                _transform = transform;
                _time += _startOffset;
                _spline = _normalPath;
                int count = _stopPoints?.Length ?? 0;
                _weldRoutines = new Coroutine[count];
                _beadHideRoutines = new Coroutine[count];
                InitializeWeldEffects();
            }
        }

        private void OnEnable()
        {
            SimulatorEvents.OnJudged += OnJudged;
        }

        private void OnDisable()
        {
            SimulatorEvents.OnJudged -= OnJudged;
            CleanupAllWeldEffects();
        }

        private void OnJudged(JudgementResult result)
        {
            _lastStatus = result.status;
            _waitingForResult = false;
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                if (_normalPath != null && _duration > 0)
                    StartCoroutine(Animate());
            }
        }

        private IEnumerator Animate()
        {
            bool[] isPassed = new bool[_stopPoints.Length];
            float stopOverrun = 0;

            while (true)
            {
                if (_spline != _reinspectPath)
                {
                    for (int i = 0; i < _stopPoints.Length; i++)
                    {
                        if (_time > _stopPoints[i].time && !isPassed[i])
                        {
                            isPassed[i] = true;
                            stopOverrun = _time - _stopPoints[i].time;
                            _time = _stopPoints[i].time;
                            SetPositionAndRotation(_time);

                            LaunchWeldEffects(i);

                            if (_stopPoints[i].robotArmAnimation != null)
                            {
                                _stopPoints[i].robotArmAnimation.Play();
                                yield return new WaitForSeconds(
                                    _stopPoints[i].robotArmAnimation.clip != null
                                    ? _stopPoints[i].robotArmAnimation.clip.length
                                    : _stopPoints[i].duration
                                );
                            }
                            else
                            {
                                yield return new WaitForSeconds(_stopPoints[i].duration);
                            }

                            if (i == 0)
                            {
                                _waitingForResult = true;
                                _generator.TriggerJudge();
                                yield return new WaitUntil(() => !_waitingForResult);

                                if (_lastStatus == WeldStatus.Reinspect)
                                    _spline = _reinspectPath;
                                else
                                    _spline = _normalPath;
                            }
                        }
                    }
                }

                _time += stopOverrun + Time.deltaTime / _duration;
                stopOverrun = 0;

                if (_time > 1)
                {
                    _time %= 1;
                    _spline = _normalPath;

                    for (int i = 0; i < isPassed.Length; i++)
                        isPassed[i] = false;
                }

                SetPositionAndRotation(_time);
                yield return null;
            }
        }

        private void LaunchWeldEffects(int index)
        {
            if (_weldRoutines == null || index < 0 || index >= _weldRoutines.Length) return;

            if (_weldRoutines[index] != null)
            {
                StopCoroutine(_weldRoutines[index]);
                _weldRoutines[index] = null;
            }
            if (_beadHideRoutines[index] != null)
            {
                StopCoroutine(_beadHideRoutines[index]);
                _beadHideRoutines[index] = null;
            }

            StopPoint sp = _stopPoints[index];
            if (sp.spark != null)
                sp.spark.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            _weldRoutines[index] = StartCoroutine(RunWeldEffects(index));
        }

        private IEnumerator RunWeldEffects(int index)
        {
            StopPoint sp = _stopPoints[index];

            if (sp.spark == null && sp.bead == null)
            {
                _weldRoutines[index] = null;
                yield break;
            }

            float startDelay = Mathf.Max(0f, sp.sparkStartDelay);
            if (startDelay > 0f)
                yield return new WaitForSeconds(startDelay);

            if (sp.bead != null)
            {
                sp.bead.SetActive(true);
                float lifetime = sp.beadVisibleDuration > 0f
                    ? sp.beadVisibleDuration
                    : _defaultBeadVisibleDuration;

                if (_beadHideRoutines[index] != null)
                    StopCoroutine(_beadHideRoutines[index]);
                _beadHideRoutines[index] = StartCoroutine(HideBeadAfter(sp.bead, lifetime));
            }

            if (sp.spark != null)
            {
                GameObject sparkGo = sp.spark.gameObject;
                if (!sparkGo.activeSelf) sparkGo.SetActive(true);
                sp.spark.Clear(true);
                sp.spark.Play(true);
            }

            float stopAt = sp.sparkEndDelay > startDelay
                ? sp.sparkEndDelay
                : Mathf.Max(sp.duration, startDelay + _sparkFallbackDuration);

            float wait = stopAt - startDelay;
            if (wait > 0f)
                yield return new WaitForSeconds(wait);

            if (sp.spark != null)
                sp.spark.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            _weldRoutines[index] = null;
        }

        private IEnumerator HideBeadAfter(GameObject bead, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (bead != null) bead.SetActive(false);
        }

        private void InitializeWeldEffects()
        {
            if (_stopPoints == null) return;
            foreach (var sp in _stopPoints)
            {
                if (sp.bead != null) sp.bead.SetActive(false);
                if (sp.spark != null) sp.spark.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void CleanupAllWeldEffects()
        {
            if (_stopPoints == null) return;
            for (int i = 0; i < _stopPoints.Length; i++)
            {
                if (_weldRoutines != null && i < _weldRoutines.Length && _weldRoutines[i] != null)
                {
                    StopCoroutine(_weldRoutines[i]);
                    _weldRoutines[i] = null;
                }
                if (_beadHideRoutines != null && i < _beadHideRoutines.Length && _beadHideRoutines[i] != null)
                {
                    StopCoroutine(_beadHideRoutines[i]);
                    _beadHideRoutines[i] = null;
                }

                StopPoint sp = _stopPoints[i];
                if (sp.spark != null)
                    sp.spark.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (sp.bead != null)
                    sp.bead.SetActive(false);
            }
        }

        private void SetPositionAndRotation(float time)
        {
            Vector3 position = _spline.EvaluatePosition(time);
            float3 tangent = _spline.EvaluateTangent(time);
            _transform.position = position;
            _transform.rotation = Quaternion.LookRotation(tangent, Vector3.up);
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (!Application.isPlaying && _spline != null)
            {
                if (_transform == null)
                    _transform = transform;
                SetPositionAndRotation(_previewTime);
            }
        }
#endif
    }
}
