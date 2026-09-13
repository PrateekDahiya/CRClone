using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CRClone.UI.Components
{
    public class ToastUI : MonoBehaviour
    {
        [SerializeField] private Text _messageText;
        [SerializeField] private TMP_Text _tmpMessageText;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private float _fadeDuration = 0.3f;
        [SerializeField] private float _displayDuration = 3f;
        [SerializeField] private float _slideDistance = 50f;

        private RectTransform _rectTransform;
        private Coroutine _displayCoroutine;
        private Vector2 _originalPosition;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _originalPosition = _rectTransform.anchoredPosition;
        }

        public void Show(string message, ToastType type = ToastType.Info)
        {
            if (_messageText != null) _messageText.text = message;
            if (_tmpMessageText != null) _tmpMessageText.text = message;

            SetToastColor(type);

            if (_displayCoroutine != null)
            {
                StopCoroutine(_displayCoroutine);
            }
            _displayCoroutine = StartCoroutine(DisplayRoutine());
        }

        private void SetToastColor(ToastType type)
        {
            if (_backgroundImage == null) return;

            Color color = type switch
            {
                ToastType.Success => new Color(0.2f, 0.7f, 0.3f, 0.9f),
                ToastType.Error => new Color(0.9f, 0.2f, 0.2f, 0.9f),
                ToastType.Warning => new Color(0.9f, 0.7f, 0.1f, 0.9f),
                _ => new Color(0.2f, 0.2f, 0.3f, 0.9f)
            };
            _backgroundImage.color = color;
        }

        private IEnumerator DisplayRoutine()
        {
            _rectTransform.anchoredPosition = _originalPosition + Vector2.up * _slideDistance;
            _canvasGroup.alpha = 0f;
            gameObject.SetActive(true);

            // Fade in and slide down
            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / _fadeDuration;
                _canvasGroup.alpha = t;
                _rectTransform.anchoredPosition = Vector2.Lerp(
                    _originalPosition + Vector2.up * _slideDistance, 
                    _originalPosition, 
                    t);
                yield return null;
            }
            _canvasGroup.alpha = 1f;
            _rectTransform.anchoredPosition = _originalPosition;

            // Wait
            yield return new WaitForSecondsRealtime(_displayDuration);

            // Fade out and slide up
            if (AccessibilityManager.Instance?.ReduceMotion != true)
            {
                elapsed = 0f;
                while (elapsed < _fadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = elapsed / _fadeDuration;
                    _canvasGroup.alpha = 1f - t;
                    _rectTransform.anchoredPosition = Vector2.Lerp(
                        _originalPosition, 
                        _originalPosition - Vector2.up * _slideDistance, 
                        t);
                    yield return null;
                }
            }
            else
            {
                _canvasGroup.alpha = 0f;
            }

            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        public enum ToastType
        {
            Info,
            Success,
            Error,
            Warning
        }
    }
}