using System;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.UI.Animation;

namespace CRClone.Battle.UI
{
    public class ElixirBar : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image[] _elixirSegments = new Image[10];
        [SerializeField] private Text _elixirText;
        [SerializeField] private Image _elixirIcon;

        [Header("Colors")]
        [SerializeField] private Color _fullColor = new Color(0f, 0.9f, 1f);
        [SerializeField] private Color _emptyColor = new Color(0.1f, 0.1f, 0.2f);
        [SerializeField] private Color _generatingColor = Color.white;
        [SerializeField] private Color _doubleElixirColor = new Color(1f, 0.8f, 0f);
        [SerializeField] private Color _tripleElixirColor = new Color(1f, 0.3f, 0.3f);

        [Header("Animation")]
        [SerializeField] private float _fillDuration = 0.2f;
        [SerializeField] private float _pulseDuration = 0.3f;
        [SerializeField] private float _pulseScale = 1.2f;
        [SerializeField] private AnimationCurve _fillCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve _pulseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private PlayerState _player;
        private float _previousElixir = 0f;
        private int _previousFullSegments = 0;
        private bool _isDoubleElixir = false;
        private bool _isTripleElixir = false;
        private Coroutine _pulseCoroutine;

        private void Awake()
        {
            _player = Services.Get<GameManager>().BattleSim?.Player1;
            InitializeSegments();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            EventBus.OnElixirChanged += OnElixirChanged;
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.OnElixirChanged -= OnElixirChanged;
        }

        private void InitializeSegments()
        {
            for (int i = 0; i < 10; i++)
            {
                if (_elixirSegments[i] != null)
                {
                    _elixirSegments[i].fillAmount = 0f;
                    _elixirSegments[i].color = _emptyColor;
                }
            }
        }

        private void OnElixirChanged(EventBus.ElixirChangedEvent evt)
        {
            if (evt.playerId != 1) return; // Only local player

            SetElixir(evt.currentElixir);
        }

        public void SetElixir(float elixir)
        {
            if (_player == null) return;

            int fullSegments = Mathf.FloorToInt(elixir);
            float partial = elixir - fullSegments;

            for (int i = 0; i < 10; i++)
            {
                Image segment = _elixirSegments[i];
                if (segment == null) continue;

                Color targetColor = GetSegmentColor(i, fullSegments);
                float targetFill = 0f;

                if (i < fullSegments)
                {
                    targetFill = 1f;
                }
                else if (i == fullSegments && partial > 0)
                {
                    targetFill = partial;
                }

                if (AccessibilityManager.Instance?.ReduceMotion == true)
                {
                    segment.fillAmount = targetFill;
                    segment.color = targetColor;
                }
                else
                {
                    StartCoroutine(AnimateSegment(segment, targetFill, targetColor));
                }
            }

            if (_elixirText != null)
            {
                _elixirText.text = Mathf.Floor(elixir).ToString();
            }

            CheckElixirGain(elixir, fullSegments);
            UpdateElixirRateVisual(elixir);

            _previousElixir = elixir;
            _previousFullSegments = fullSegments;
        }

        private Color GetSegmentColor(int index, int fullSegments)
        {
            if (_isTripleElixir) return _tripleElixirColor;
            if (_isDoubleElixir) return _doubleElixirColor;
            return _fullColor;
        }

        private IEnumerator AnimateSegment(Image segment, float targetFill, Color targetColor)
        {
            float startFill = segment.fillAmount;
            Color startColor = segment.color;
            float elapsed = 0f;

            while (elapsed < _fillDuration)
            {
                elapsed += Time.deltaTime;
                float t = _fillCurve.Evaluate(elapsed / _fillDuration);
                segment.fillAmount = Mathf.Lerp(startFill, targetFill, t);
                segment.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            segment.fillAmount = targetFill;
            segment.color = targetColor;
        }

        private void CheckElixirGain(float currentElixir, int currentFullSegments)
        {
            if (currentElixir > _previousElixir && currentFullSegments > _previousFullSegments)
            {
                int newSegment = currentFullSegments - 1;
                if (newSegment >= 0 && newSegment < 10)
                {
                    StartCoroutine(PulseSegment(newSegment));
                }
            }
        }

        private IEnumerator PulseSegment(int index)
        {
            if (index < 0 || index >= 10 || _elixirSegments[index] == null) yield break;

            var segment = _elixirSegments[index];
            var rectTransform = segment.rectTransform;
            Vector3 originalScale = rectTransform.localScale;
            Vector3 targetScale = originalScale * _pulseScale;
            Color originalColor = segment.color;

            if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);

            float elapsed = 0f;
            while (elapsed < _pulseDuration)
            {
                elapsed += Time.deltaTime;
                float t = _pulseCurve.Evaluate(elapsed / _pulseDuration);
                rectTransform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                segment.color = Color.Lerp(_generatingColor, GetSegmentColor(index, _previousFullSegments), t);
                yield return null;
            }

            rectTransform.localScale = originalScale;
            segment.color = GetSegmentColor(index, _previousFullSegments);
        }

        private void UpdateElixirRateVisual(float elixir)
        {
            var config = Services.Get<GameManager>()?.BattleSim?._config;
            if (config == null) return;

            bool wasDouble = _isDoubleElixir;
            bool wasTriple = _isTripleElixir;

            float battleTime = Services.Get<GameManager>().BattleSim.CurrentTick * BattleSimulation.FIXED_DT;
            float remainingTime = config.battleDuration - battleTime;

            _isDoubleElixir = remainingTime <= 60f && remainingTime > 0;
            _isTripleElixir = remainingTime <= 0;

            if (_isDoubleElixir != wasDouble || _isTripleElixir != wasTriple)
            {
                RefreshAllSegments();
            }
        }

        private void RefreshAllSegments()
        {
            if (_player == null) return;
            SetElixir(_player.Elixir);
        }

        public void SetDoubleElixirMode(bool enabled)
        {
            _isDoubleElixir = enabled;
            RefreshAllSegments();
        }

        public void SetTripleElixirMode(bool enabled)
        {
            _isTripleElixir = enabled;
            RefreshAllSegments();
        }

        public void PlayElixirGainPulse(int segmentIndex)
        {
            if (segmentIndex >= 0 && segmentIndex < 10)
            {
                StartCoroutine(PulseSegment(segmentIndex));
            }
        }
    }
}