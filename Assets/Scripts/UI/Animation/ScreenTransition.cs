using System;
using UnityEngine;
using UnityEngine.UI;
using CRClone.UI.Animation;

namespace CRClone.UI
{
    public enum TransitionType
    {
        SlideHorizontal,
        Fade,
        ScaleFade
    }

    public class ScreenTransition : MonoBehaviour
    {
        [Header("Transition Settings")]
        [SerializeField] private TransitionType _transitionType = TransitionType.SlideHorizontal;
        [SerializeField] private float _duration = 0.3f;
        [SerializeField] private AnimationCurve _curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Vector2 _originalPosition;
        private bool _isTransitioning;

        public static ScreenTransition Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            _originalPosition = _rectTransform.anchoredPosition;
        }

        public void TransitionIn(TransitionType type, Action onComplete = null)
        {
            if (_isTransitioning) return;
            StartCoroutine(TransitionInRoutine(type, onComplete));
        }

        public void TransitionOut(TransitionType type, Action onComplete = null)
        {
            if (_isTransitioning) return;
            StartCoroutine(TransitionOutRoutine(type, onComplete));
        }

        private System.Collections.IEnumerator TransitionInRoutine(TransitionType type, Action onComplete)
        {
            _isTransitioning = true;
            gameObject.SetActive(true);

            if (AccessibilityManager.Instance?.ReduceMotion == true)
            {
                _canvasGroup.alpha = 1f;
                _rectTransform.anchoredPosition = _originalPosition;
                transform.localScale = Vector3.one;
                _isTransitioning = false;
                onComplete?.Invoke();
                yield break;
            }

            switch (type)
            {
                case TransitionType.SlideHorizontal:
                    yield return SlideInHorizontal();
                    break;
                case TransitionType.Fade:
                    yield return FadeIn();
                    break;
                case TransitionType.ScaleFade:
                    yield return ScaleFadeIn();
                    break;
            }

            _isTransitioning = false;
            onComplete?.Invoke();
        }

        private System.Collections.IEnumerator TransitionOutRoutine(TransitionType type, Action onComplete)
        {
            _isTransitioning = true;

            if (AccessibilityManager.Instance?.ReduceMotion == true)
            {
                _canvasGroup.alpha = 0f;
                gameObject.SetActive(false);
                _isTransitioning = false;
                onComplete?.Invoke();
                yield break;
            }

            switch (type)
            {
                case TransitionType.SlideHorizontal:
                    yield return SlideOutHorizontal();
                    break;
                case TransitionType.Fade:
                    yield return FadeOut();
                    break;
                case TransitionType.ScaleFade:
                    yield return ScaleFadeOut();
                    break;
            }

            gameObject.SetActive(false);
            _isTransitioning = false;
            onComplete?.Invoke();
        }

        private System.Collections.IEnumerator SlideInHorizontal()
        {
            float elapsed = 0f;
            float screenWidth = Screen.width;
            Vector2 startPos = new Vector2(screenWidth, 0);
            Vector2 endPos = _originalPosition;

            _rectTransform.anchoredPosition = startPos;
            _canvasGroup.alpha = 1f;

            while (elapsed < _duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = _curve.Evaluate(elapsed / _duration);
                _rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }
            _rectTransform.anchoredPosition = endPos;
        }

        private System.Collections.IEnumerator SlideOutHorizontal()
        {
            float elapsed = 0f;
            float screenWidth = Screen.width;
            Vector2 startPos = _originalPosition;
            Vector2 endPos = new Vector2(-screenWidth, 0);

            while (elapsed < _duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = _curve.Evaluate(elapsed / _duration);
                _rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }
            _rectTransform.anchoredPosition = endPos;
        }

        private System.Collections.IEnumerator FadeIn()
        {
            float elapsed = 0f;
            _canvasGroup.alpha = 0f;
            _rectTransform.anchoredPosition = _originalPosition;

            while (elapsed < _duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = _curve.Evaluate(elapsed / _duration);
                _canvasGroup.alpha = t;
                yield return null;
            }
            _canvasGroup.alpha = 1f;
        }

        private System.Collections.IEnumerator FadeOut()
        {
            float elapsed = 0f;
            _canvasGroup.alpha = 1f;

            while (elapsed < _duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = _curve.Evaluate(elapsed / _duration);
                _canvasGroup.alpha = 1f - t;
                yield return null;
            }
            _canvasGroup.alpha = 0f;
        }

        private System.Collections.IEnumerator ScaleFadeIn()
        {
            float elapsed = 0f;
            _canvasGroup.alpha = 0f;
            transform.localScale = Vector3.one * 0.8f;
            _rectTransform.anchoredPosition = _originalPosition;

            while (elapsed < _duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = _curve.Evaluate(elapsed / _duration);
                _canvasGroup.alpha = t;
                transform.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, t);
                yield return null;
            }
            _canvasGroup.alpha = 1f;
            transform.localScale = Vector3.one;
        }

        private System.Collections.IEnumerator ScaleFadeOut()
        {
            float elapsed = 0f;
            _canvasGroup.alpha = 1f;
            transform.localScale = Vector3.one;

            while (elapsed < _duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = _curve.Evaluate(elapsed / _duration);
                _canvasGroup.alpha = 1f - t;
                transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.8f, t);
                yield return null;
            }
            _canvasGroup.alpha = 0f;
            transform.localScale = Vector3.one * 0.8f;
        }

        public void ResetTransform()
        {
            _rectTransform.anchoredPosition = _originalPosition;
            transform.localScale = Vector3.one;
            _canvasGroup.alpha = 1f;
        }
    }
}