using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace CRClone.UI.Animation
{
    public static class UITweens
    {
        public static Coroutine ScaleTo(this Transform transform, Vector3 targetScale, float duration, AnimationCurve curve = null, Action onComplete = null, bool unscaledTime = false)
        {
            if (AccessibilityManager.Instance?.ReduceMotion == true)
            {
                transform.localScale = targetScale;
                onComplete?.Invoke();
                return null;
            }
            return CoroutineRunner.Instance.StartCoroutine(ScaleToRoutine(transform, targetScale, duration, curve ?? AnimationCurve.EaseInOut(0, 0, 1, 1), onComplete, unscaledTime));
        }

        public static Coroutine FadeTo(this CanvasGroup canvasGroup, float targetAlpha, float duration, AnimationCurve curve = null, Action onComplete = null, bool unscaledTime = false)
        {
            if (AccessibilityManager.Instance?.ReduceMotion == true)
            {
                canvasGroup.alpha = targetAlpha;
                onComplete?.Invoke();
                return null;
            }
            return CoroutineRunner.Instance.StartCoroutine(FadeToRoutine(canvasGroup, targetAlpha, duration, curve ?? AnimationCurve.EaseInOut(0, 0, 1, 1), onComplete, unscaledTime));
        }

        public static Coroutine FadeTo(this Graphic graphic, float targetAlpha, float duration, AnimationCurve curve = null, Action onComplete = null, bool unscaledTime = false)
        {
            if (AccessibilityManager.Instance?.ReduceMotion == true)
            {
                var color = graphic.color;
                color.a = targetAlpha;
                graphic.color = color;
                onComplete?.Invoke();
                return null;
            }
            return CoroutineRunner.Instance.StartCoroutine(FadeToRoutine(graphic, targetAlpha, duration, curve ?? AnimationCurve.EaseInOut(0, 0, 1, 1), onComplete, unscaledTime));
        }

        public static Coroutine Shake(this Transform transform, float magnitude, float duration, int vibrations = 10, Action onComplete = null, bool unscaledTime = false)
        {
            if (AccessibilityManager.Instance?.ReduceMotion == true)
            {
                onComplete?.Invoke();
                return null;
            }
            return CoroutineRunner.Instance.StartCoroutine(ShakeRoutine(transform, magnitude, duration, vibrations, onComplete, unscaledTime));
        }

        public static Coroutine Pulse(this Transform transform, float scaleMultiplier, float duration, int loops = 1, Action onComplete = null, bool unscaledTime = false)
        {
            if (AccessibilityManager.Instance?.ReduceMotion == true)
            {
                onComplete?.Invoke();
                return null;
            }
            return CoroutineRunner.Instance.StartCoroutine(PulseRoutine(transform, scaleMultiplier, duration, loops, onComplete, unscaledTime));
        }

        public static Coroutine MoveTo(this RectTransform rectTransform, Vector2 targetPosition, float duration, AnimationCurve curve = null, Action onComplete = null, bool unscaledTime = false)
        {
            if (AccessibilityManager.Instance?.ReduceMotion == true)
            {
                rectTransform.anchoredPosition = targetPosition;
                onComplete?.Invoke();
                return null;
            }
            return CoroutineRunner.Instance.StartCoroutine(MoveToRoutine(rectTransform, targetPosition, duration, curve ?? AnimationCurve.EaseInOut(0, 0, 1, 1), onComplete, unscaledTime));
        }

        public static Coroutine FillAmount(this Image image, float targetFill, float duration, AnimationCurve curve = null, Action onComplete = null, bool unscaledTime = false)
        {
            if (AccessibilityManager.Instance?.ReduceMotion == true)
            {
                image.fillAmount = targetFill;
                onComplete?.Invoke();
                return null;
            }
            return CoroutineRunner.Instance.StartCoroutine(FillAmountRoutine(image, targetFill, duration, curve ?? AnimationCurve.EaseInOut(0, 0, 1, 1), onComplete, unscaledTime));
        }

        private static IEnumerator ScaleToRoutine(Transform transform, Vector3 targetScale, float duration, AnimationCurve curve, Action onComplete, bool unscaledTime)
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = curve.Evaluate(elapsed / duration);
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }
            transform.localScale = targetScale;
            onComplete?.Invoke();
        }

        private static IEnumerator FadeToRoutine(CanvasGroup canvasGroup, float targetAlpha, float duration, AnimationCurve curve, Action onComplete, bool unscaledTime)
        {
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = curve.Evaluate(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }
            canvasGroup.alpha = targetAlpha;
            onComplete?.Invoke();
        }

        private static IEnumerator FadeToRoutine(Graphic graphic, float targetAlpha, float duration, AnimationCurve curve, Action onComplete, bool unscaledTime)
        {
            Color startColor = graphic.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = curve.Evaluate(elapsed / duration);
                Color newColor = startColor;
                newColor.a = Mathf.Lerp(startColor.a, targetAlpha, t);
                graphic.color = newColor;
                yield return null;
            }
            Color finalColor = graphic.color;
            finalColor.a = targetAlpha;
            graphic.color = finalColor;
            onComplete?.Invoke();
        }

        private static IEnumerator ShakeRoutine(Transform transform, float magnitude, float duration, int vibrations, Action onComplete, bool unscaledTime)
        {
            Vector3 originalPos = transform.localPosition;
            float elapsed = 0f;
            float vibrationInterval = duration / vibrations;

            while (elapsed < duration)
            {
                elapsed += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = elapsed / duration;
                float currentMagnitude = magnitude * (1f - t);

                float offsetX = UnityEngine.Random.Range(-currentMagnitude, currentMagnitude);
                float offsetY = UnityEngine.Random.Range(-currentMagnitude, currentMagnitude);

                transform.localPosition = originalPos + new Vector3(offsetX, offsetY, 0);
                yield return null;
            }
            transform.localPosition = originalPos;
            onComplete?.Invoke();
        }

        private static IEnumerator PulseRoutine(Transform transform, float scaleMultiplier, float duration, int loops, Action onComplete, bool unscaledTime)
        {
            Vector3 originalScale = transform.localScale;
            Vector3 targetScale = originalScale * scaleMultiplier;
            float halfDuration = duration / 2f;

            for (int i = 0; i < loops; i++)
            {
                yield return ScaleToRoutine(transform, targetScale, halfDuration, AnimationCurve.EaseInOut(0, 0, 1, 1), null, unscaledTime);
                yield return ScaleToRoutine(transform, originalScale, halfDuration, AnimationCurve.EaseInOut(0, 0, 1, 1), null, unscaledTime);
            }
            onComplete?.Invoke();
        }

        private static IEnumerator MoveToRoutine(RectTransform rectTransform, Vector2 targetPosition, float duration, AnimationCurve curve, Action onComplete, bool unscaledTime)
        {
            Vector2 startPosition = rectTransform.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = curve.Evaluate(elapsed / duration);
                rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
                yield return null;
            }
            rectTransform.anchoredPosition = targetPosition;
            onComplete?.Invoke();
        }

        private static IEnumerator FillAmountRoutine(Image image, float targetFill, float duration, AnimationCurve curve, Action onComplete, bool unscaledTime)
        {
            float startFill = image.fillAmount;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = curve.Evaluate(elapsed / duration);
                image.fillAmount = Mathf.Lerp(startFill, targetFill, t);
                yield return null;
            }
            image.fillAmount = targetFill;
            onComplete?.Invoke();
        }
    }

    public class CoroutineRunner : MonoBehaviour
    {
        public static CoroutineRunner Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}