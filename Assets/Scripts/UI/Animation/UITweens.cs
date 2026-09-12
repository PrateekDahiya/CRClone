using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace CRClone.UI.Animation
{
    public static class UITweens
    {
        public static IEnumerator ScaleTo(this Transform transform, Vector3 targetScale, float duration, AnimationCurve curve = null, Action onComplete = null, bool unscaledTime = false)
        {
            if (AccessibilityManager.Instance?.ReduceMotion == true && duration > 0.1f)
            {
                transform.localScale = targetScale;
                onComplete?.Invoke();
                yield break;
            }

            Vector3 startScale = transform.localScale;
            float elapsed = 0f;
            curve ??= AnimationCurve.EaseInOut(0, 0, 1, 1);

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

        public static IEnumerator ScaleTo(this RectTransform rect, Vector3 targetScale, float duration, AnimationCurve curve = null, Action onComplete = null, bool unscaledTime = false)
        {
            return rect.transform.ScaleTo(targetScale, duration, curve, onComplete, unscaledTime);
        }

        public static IEnumerator Pulse(this Transform transform, float scaleMultiplier = 1.2f, float duration = 0.3f, int loops = 1, Action onComplete = null)
        {
            if (AccessibilityManager.Instance?.ReduceMotion == true)
            {
                onComplete?.Invoke();
                yield break;
            }

            Vector3 originalScale = transform.localScale;
            Vector3 targetScale = originalScale * scaleMultiplier;

            for (int i = 0; i < loops; i++)
            {
                yield return transform.ScaleTo(targetScale, duration * 0.5f, AnimationCurve.EaseInOut(0, 0, 1, 1));
                yield return transform.ScaleTo(originalScale, duration * 0.5f, AnimationCurve.EaseInOut(0, 0, 1, 1));
            }

            onComplete?.Invoke();
        }

        public static IEnumerator FadeTo(this CanvasGroup canvasGroup, float targetAlpha, float duration, AnimationCurve curve = null, Action onComplete = null, bool unscaledTime = false)
        {
            if (AccessibilityManager.Instance?.ReduceMotion == true && duration > 0.1f)
            {
                canvasGroup.alpha = targetAlpha;
                onComplete?.Invoke();
                yield break;
            }

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;
            curve ??= AnimationCurve.EaseInOut(0, 0, 1, 1);

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

        public static IEnumerator FadeTo(this Graphic graphic, float targetAlpha, float duration, AnimationCurve curve = null, Action onComplete = null, bool unscaledTime = false)
        {
            if (AccessibilityManager.Instance?.ReduceMotion == true && duration > 0.1f)
            {
                var color = graphic.color;
                color.a = targetAlpha;
                graphic.color = color;
                onComplete?.Invoke();
                yield break;
            }

            Color startColor = graphic.color;
            float elapsed = 0f;
            curve ??= AnimationCurve.EaseInOut(0, 0, 1, 1);

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

        public static IEnumerator Shake(this Transform transform, float magnitude = 10f, float duration = 0.3f, int vibrations = 10, Action onComplete = null, bool unscaledTime = false)
        {
            if (AccessibilityManager.Instance?.ReduceMotion == true)
            {
                onComplete?.Invoke();
                yield break;
            }

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

        public static IEnumerator ShakePosition(this RectTransform rect, float magnitude = 10f, float duration = 0.3f, int vibrations = 10, Action onComplete = null, bool unscaledTime = false)
        {
            return rect.transform.Shake(magnitude, duration, vibrations, onComplete, unscaledTime);
        }

        public static IEnumerator Bounce(this Transform transform, float height = 20f, float duration = 0.5f, int bounces = 2, Action onComplete = null, bool unscaledTime = false)
        {
            if (AccessibilityManager.Instance?.ReduceMotion == true)
            {
                onComplete?.Invoke();
                yield break;
            }

            Vector3 originalPos = transform.localPosition;
            float elapsed = 0f;

            for (int i = 0; i < bounces; i++)
            {
                float bounceDuration = duration / bounces;
                float halfDuration = bounceDuration * 0.5f;

                yield return MoveY(transform, originalPos.y + height, halfDuration, AnimationCurve.EaseInOut(0, 0, 1, 1), unscaledTime);
                yield return MoveY(transform, originalPos.y, halfDuration, AnimationCurve.EaseInOut(0, 0, 1, 1), unscaledTime);
                height *= 0.5f;
            }

            transform.localPosition = originalPos;
            onComplete?.Invoke();
        }

        private static IEnumerator MoveY(Transform transform, float targetY, float duration, AnimationCurve curve, bool unscaledTime)
        {
            float startY = transform.localPosition.y;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = curve.Evaluate(elapsed / duration);
                Vector3 pos = transform.localPosition;
                pos.y = Mathf.Lerp(startY, targetY, t);
                transform.localPosition = pos;
                yield return null;
            }

            Vector3 finalPos = transform.localPosition;
            finalPos.y = targetY;
            transform.localPosition = finalPos;
        }

        public static IEnumerator ColorTo(this Graphic graphic, Color targetColor, float duration, AnimationCurve curve = null, Action onComplete = null, bool unscaledTime = false)
        {
            Color startColor = graphic.color;
            float elapsed = 0f;
            curve ??= AnimationCurve.EaseInOut(0, 0, 1, 1);

            while (elapsed < duration)
            {
                elapsed += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = curve.Evaluate(elapsed / duration);
                graphic.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            graphic.color = targetColor;
            onComplete?.Invoke();
        }

        public static IEnumerator FillAmount(this Image image, float targetFill, float duration, AnimationCurve curve = null, Action onComplete = null, bool unscaledTime = false)
        {
            float startFill = image.fillAmount;
            float elapsed = 0f;
            curve ??= AnimationCurve.EaseInOut(0, 0, 1, 1);

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

        public static IEnumerator RotateTo(this Transform transform, Quaternion targetRotation, float duration, AnimationCurve curve = null, Action onComplete = null, bool unscaledTime = false)
        {
            Quaternion startRotation = transform.rotation;
            float elapsed = 0f;
            curve ??= AnimationCurve.EaseInOut(0, 0, 1, 1);

            while (elapsed < duration)
            {
                elapsed += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = curve.Evaluate(elapsed / duration);
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                yield return null;
            }

            transform.rotation = targetRotation;
            onComplete?.Invoke();
        }

        public static IEnumerator MoveTo(this RectTransform rect, Vector2 targetPosition, float duration, AnimationCurve curve = null, Action onComplete = null, bool unscaledTime = false)
        {
            Vector2 startPosition = rect.anchoredPosition;
            float elapsed = 0f;
            curve ??= AnimationCurve.EaseInOut(0, 0, 1, 1);

            while (elapsed < duration)
            {
                elapsed += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = curve.Evaluate(elapsed / duration);
                rect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            rect.anchoredPosition = targetPosition;
            onComplete?.Invoke();
        }

        public static IEnumerator Sequence(this MonoBehaviour behaviour, params Func<IEnumerator>[] tweens)
        {
            foreach (var tween in tweens)
            {
                yield return behaviour.StartCoroutine(tween());
            }
        }

        public static IEnumerator Parallel(this MonoBehaviour behaviour, params Func<IEnumerator>[] tweens)
        {
            var coroutines = new System.Collections.Generic.List<Coroutine>();
            foreach (var tween in tweens)
            {
                coroutines.Add(behaviour.StartCoroutine(tween()));
            }

            foreach (var coroutine in coroutines)
            {
                yield return coroutine;
            }
        }

        public static IEnumerator Delay(this MonoBehaviour behaviour, float delay, bool unscaledTime = false)
        {
            if (unscaledTime)
            {
                yield return new WaitForSecondsRealtime(delay);
            }
            else
            {
                yield return new WaitForSeconds(delay);
            }
        }

        public static IEnumerator WaitForFrame(this MonoBehaviour behaviour, int frames = 1)
        {
            for (int i = 0; i < frames; i++)
            {
                yield return null;
            }
        }
    }

    public static class AnimationCurves
    {
        public static readonly AnimationCurve EaseOutBack = new AnimationCurve(
            new Keyframe(0, 0, 0, 1.70158f),
            new Keyframe(1, 1, 0, 0)
        );

        public static readonly AnimationCurve EaseInBack = new AnimationCurve(
            new Keyframe(0, 0, 0, 0),
            new Keyframe(1, 1, 1.70158f, 0)
        );

        public static readonly AnimationCurve EaseInOutBack = new AnimationCurve(
            new Keyframe(0, 0, 0, 1.70158f * 1.525f),
            new Keyframe(0.5f, 0.5f, 1.70158f * 1.525f, 1.70158f * 1.525f),
            new Keyframe(1, 1, 0, 0)
        );

        public static readonly AnimationCurve ElasticOut = new AnimationCurve(
            new Keyframe(0, 0, 0, 10),
            new Keyframe(0.3f, 1.2f, 0, 0),
            new Keyframe(0.6f, 0.9f, 0, 0),
            new Keyframe(1, 1, 0, 0)
        );

        public static readonly AnimationCurve BounceOut = new AnimationCurve(
            new Keyframe(0, 0, 0, 7.5625f),
            new Keyframe(0.4f, 0.75f, 0, 0),
            new Keyframe(0.7f, 0.9375f, 0, 0),
            new Keyframe(0.9f, 0.984375f, 0, 0),
            new Keyframe(1, 1, 0, 0)
        );
    }
}