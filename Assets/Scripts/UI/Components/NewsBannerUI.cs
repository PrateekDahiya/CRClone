using System;
using UnityEngine;
using UnityEngine.UI;
using CRClone.UI.Animation;
using CRClone.Core;

namespace CRClone.UI.Components
{
    public class NewsBannerUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _bannerRoot;
        [SerializeField] private Image _bannerImage;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _bodyText;
        [SerializeField] private Button _actionButton;
        [SerializeField] private Text _actionButtonText;
        [SerializeField] private Button _dismissButton;
        [SerializeField] private Toggle _dontShowAgainToggle;

        [Header("Animation")]
        [SerializeField] private float _slideDuration = 0.3f;
        [SerializeField] private AnimationCurve _slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private NewsData _currentNews;
        private Action _onActionClicked;
        private bool _isVisible;

        public void Initialize()
        {
            _actionButton.OrNull()?.onClick.AddListener(OnActionClicked);
            _dismissButton.OrNull()?.onClick.AddListener(OnDismissClicked);
            Hide();
        }

        public void ShowNews(NewsData news)
        {
            _currentNews = news;
            _onActionClicked = news.onActionClicked;

            if (_titleText != null) _titleText.text = news.title;
            if (_bodyText != null) _bodyText.text = news.body;
            if (_bannerImage != null && news.bannerSprite != null) _bannerImage.sprite = news.bannerSprite;

            bool hasAction = !string.IsNullOrEmpty(news.actionText) && news.onActionClicked != null;
            _actionButton.OrNull()?.gameObject.SetActive(hasAction);
            if (_actionButtonText != null) _actionButtonText.text = news.actionText;

            _dontShowAgainToggle.OrNull()?.gameObject.SetActive(news.showDontShowAgain);
            if (_dontShowAgainToggle != null)
            {
                _dontShowAgainToggle.isOn = false;
                _dontShowAgainToggle.onValueChanged.RemoveAllListeners();
                _dontShowAgainToggle.onValueChanged.AddListener(OnDontShowAgainChanged);
            }

            Show();
        }

        public void Hide()
        {
            if (!_isVisible) return;

            if (AccessibilityManager.Instance?.ReduceMotion != true)
            {
                StartCoroutine(HideAnimation());
            }
            else
            {
                _bannerRoot.OrNull()?.SetActive(false);
                _isVisible = false;
            }
        }

        private void Show()
        {
            if (_isVisible) return;

            _bannerRoot.OrNull()?.SetActive(true);
            _isVisible = true;

            if (AccessibilityManager.Instance?.ReduceMotion != true)
            {
                StartCoroutine(ShowAnimation());
            }
        }

        private System.Collections.IEnumerator ShowAnimation()
        {
            var canvasGroup = _bannerRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = _bannerRoot.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            var rect = _bannerRoot.GetComponent<RectTransform>();
            Vector2 startPos = rect.anchoredPosition + Vector2.up * 100f;
            Vector2 endPos = rect.anchoredPosition;
            rect.anchoredPosition = startPos;

            float elapsed = 0f;
            while (elapsed < _slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = _slideCurve.Evaluate(elapsed / _slideDuration);
                canvasGroup.alpha = t;
                rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            rect.anchoredPosition = endPos;
        }

        private System.Collections.IEnumerator HideAnimation()
        {
            var canvasGroup = _bannerRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = _bannerRoot.AddComponent<CanvasGroup>();

            var rect = _bannerRoot.GetComponent<RectTransform>();
            Vector2 startPos = rect.anchoredPosition;
            Vector2 endPos = startPos + Vector2.up * 100f;

            float elapsed = 0f;
            while (elapsed < _slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = _slideCurve.Evaluate(elapsed / _slideDuration);
                canvasGroup.alpha = 1f - t;
                rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            rect.anchoredPosition = endPos;
            _bannerRoot.OrNull()?.SetActive(false);
            _isVisible = false;
        }

        private void OnActionClicked()
        {
            _onActionClicked?.Invoke();
            if (!_currentNews.persistAfterAction)
            {
                Hide();
            }
        }

        private void OnDismissClicked()
        {
            if (_dontShowAgainToggle != null && _dontShowAgainToggle.isOn && _currentNews != null)
            {
                PlayerPrefs.SetInt($"news_dismissed_{_currentNews.newsId}", 1);
                PlayerPrefs.Save();
            }
            Hide();
        }

        private void OnDontShowAgainChanged(bool value)
        {
        }

        public static bool WasNewsDismissed(string newsId)
        {
            return PlayerPrefs.GetInt($"news_dismissed_{newsId}", 0) == 1;
        }
    }

    [Serializable]
    public class NewsData
    {
        public string newsId;
        public string title;
        public string body;
        public Sprite bannerSprite;
        public string actionText;
        public Action onActionClicked;
        public bool persistAfterAction = false;
        public bool showDontShowAgain = true;
        public DateTime expiryDate;
    }
}