using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace CRClone.UI.Animation
{
    public enum TransitionType
    {
        SlideHorizontal,
        SlideVertical,
        Fade,
        ScaleFade,
        None
    }

    public class ScreenTransition : MonoBehaviour
    {
        [Header("Transition Settings")]
        [SerializeField] private TransitionType _defaultTransition = TransitionType.SlideHorizontal;
        [SerializeField] private float _slideDuration = 0.3f;
        [SerializeField] private float _fadeDuration = 0.2f;
        [SerializeField] private float _scaleFadeDuration = 0.2f;
        [SerializeField] private AnimationCurve _slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve _fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve _scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Vector2 _originalPosition;
        private Vector3 _originalScale;

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
            _originalScale = transform.localScale;
        }

        public IEnumerator TransitionIn(TransitionType type = TransitionType.SlideHorizontal, Action onComplete = null)
        {
            yield return PlayTransition(type, true);
            onComplete?.Invoke();
        }

        public IEnumerator TransitionOut(TransitionType type = TransitionType.SlideHorizontal, Action onComplete = null)
        {
            yield return PlayTransition(type, false);
            onComplete?.Invoke();
        }

        private IEnumerator PlayTransition(TransitionType type, bool entering)
        {
            if (!AccessibilityManager.Instance.ReduceMotion)
            {
                switch (type)
                {
                    case TransitionType.SlideHorizontal:
                        yield return SlideHorizontal(entering);
                        break;
                    case TransitionType.SlideVertical:
                        yield return SlideVertical(entering);
                        break;
                    case TransitionType.Fade:
                        yield return Fade(entering);
                        break;
                    case TransitionType.ScaleFade:
                        yield return ScaleFade(entering);
                        break;
                }
            }
            else
            {
                _canvasGroup.alpha = entering ? 1f : 0f;
                _rectTransform.anchoredPosition = _originalPosition;
                transform.localScale = _originalScale;
                yield return null;
            }
        }

        private IEnumerator SlideHorizontal(bool entering)
        {
            float duration = _slideDuration;
            float elapsed = 0f;
            float screenWidth = Screen.width;
            Vector2 startPos = entering ? new Vector2(screenWidth, 0) : _originalPosition;
            Vector2 endPos = entering ? _originalPosition : new Vector2(-screenWidth, 0);

            _rectTransform.anchoredPosition = startPos;
            _canvasGroup.alpha = 1f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = _slideCurve.Evaluate(elapsed / duration);
                _rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }

            _rectTransform.anchoredPosition = endPos;
        }

        private IEnumerator SlideVertical(bool entering)
        {
            float duration = _slideDuration;
            float elapsed = 0f;
            float screenHeight = Screen.height;
            Vector2 startPos = entering ? new Vector2(0, screenHeight) : _originalPosition;
            Vector2 endPos = entering ? _originalPosition : new Vector2(0, -screenHeight);

            _rectTransform.anchoredPosition = startPos;
            _canvasGroup.alpha = 1f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = _slideCurve.Evaluate(elapsed / duration);
                _rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }

            _rectTransform.anchoredPosition = endPos;
        }

        private IEnumerator Fade(bool entering)
        {
            float duration = _fadeDuration;
            float elapsed = 0f;
            float startAlpha = entering ? 0f : 1f;
            float endAlpha = entering ? 1f : 0f;

            _canvasGroup.alpha = startAlpha;
            _rectTransform.anchoredPosition = _originalPosition;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = _fadeCurve.Evaluate(elapsed / duration);
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                yield return null;
            }

            _canvasGroup.alpha = endAlpha;
        }

        private IEnumerator ScaleFade(bool entering)
        {
            float duration = _scaleFadeDuration;
            float elapsed = 0f;
            Vector3 startScale = entering ? Vector3.one * 0.8f : _originalScale;
            Vector3 endScale = entering ? _originalScale : Vector3.one * 0.8f;
            float startAlpha = entering ? 0f : 1f;
            float endAlpha = entering ? 1f : 0f;

            transform.localScale = startScale;
            _canvasGroup.alpha = startAlpha;
            _rectTransform.anchoredPosition = _originalPosition;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = _scaleCurve.Evaluate(elapsed / duration);
                transform.localScale = Vector3.Lerp(startScale, endScale, t);
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                yield return null;
            }

            transform.localScale = endScale;
            _canvasGroup.alpha = endAlpha;
        }

        public void ResetTransform()
        {
            _rectTransform.anchoredPosition = _originalPosition;
            transform.localScale = _originalScale;
            _canvasGroup.alpha = 1f;
        }
    }

    public static class ScreenTransitionExtensions
    {
        public static IEnumerator TransitionIn(this MonoBehaviour behaviour, TransitionType type = TransitionType.SlideHorizontal, Action onComplete = null)
        {
            var transition = behaviour.GetComponent<ScreenTransition>();
            if (transition == null) transition = behaviour.gameObject.AddComponent<ScreenTransition>();
            yield return transition.TransitionIn(type, onComplete);
        }

        public static IEnumerator TransitionOut(this MonoBehaviour behaviour, TransitionType type = TransitionType.SlideHorizontal, Action onComplete = null)
        {
            var transition = behaviour.GetComponent<ScreenTransition>();
            if (transition == null) transition = behaviour.gameObject.AddComponent<ScreenTransition>();
            yield return transition.TransitionOut(type, onComplete);
        }
    }
}