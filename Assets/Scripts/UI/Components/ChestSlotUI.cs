using System;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;

namespace CRClone.UI.Components
{
    public class ChestSlotUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image _chestImage;
        [SerializeField] private Text _timerText;
        [SerializeField] private GameObject _lockIcon;
        [SerializeField] private GameObject _readyIcon;
        [SerializeField] private GameObject _emptyState;
        [SerializeField] private Button _slotButton;
        [SerializeField] private ParticleSystem _unlockParticles;
        [SerializeField] private AnimationCurve _bounceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Chest Sprites")]
        [SerializeField] private Sprite _woodenChestSprite;
        [SerializeField] private Sprite _silverChestSprite;
        [SerializeField] private Sprite _goldenChestSprite;
        [SerializeField] private Sprite _magicalChestSprite;
        [SerializeField] private Sprite _giantChestSprite;
        [SerializeField] private Sprite _legendaryChestSprite;
        [SerializeField] private Sprite _epicChestSprite;
        [SerializeField] private Sprite _championChestSprite;

        private int _slotIndex;
        private ChestData _currentChest;
        private Coroutine _timerCoroutine;
        private bool _isUnlocking;

        public int SlotIndex => _slotIndex;

        public void Initialize(int slotIndex)
        {
            _slotIndex = slotIndex;

            if (_slotButton != null)
            {
                _slotButton.onClick.AddListener(OnSlotClicked);
            }

            SetEmpty();
        }

        public void SetChestData(ChestData chestData)
        {
            _currentChest = chestData;

            if (chestData == null || chestData.chestTypeId <= 0)
            {
                SetEmpty();
                return;
            }

            gameObject.SetActive(true);
            _emptyState?.SetActive(false);
            _chestImage?.gameObject.SetActive(true);

            UpdateChestVisual(chestData.chestTypeId);

            if (chestData.unlockTime > DateTime.UtcNow)
            {
                StartUnlockTimer(chestData.unlockTime);
                _lockIcon?.SetActive(true);
                _readyIcon?.SetActive(false);
            }
            else
            {
                StopTimer();
                _lockIcon?.SetActive(false);
                _readyIcon?.SetActive(true);
                StartBounceAnimation();
            }
        }

        private void UpdateChestVisual(int chestTypeId)
        {
            if (_chestImage == null) return;

            Sprite sprite = chestTypeId switch
            {
                1 => _woodenChestSprite,
                2 => _silverChestSprite,
                3 => _goldenChestSprite,
                4 => _magicalChestSprite,
                5 => _giantChestSprite,
                6 => _legendaryChestSprite,
                7 => _epicChestSprite,
                8 => _championChestSprite,
                _ => _woodenChestSprite
            };

            _chestImage.sprite = sprite;
        }

        private void SetEmpty()
        {
            _currentChest = null;
            StopTimer();
            _chestImage?.gameObject.SetActive(false);
            _timerText?.gameObject.SetActive(false);
            _lockIcon?.SetActive(false);
            _readyIcon?.SetActive(false);
            _emptyState?.SetActive(true);
        }

        private void StartUnlockTimer(DateTime unlockTime)
        {
            StopTimer();
            _timerCoroutine = StartCoroutine(UnlockTimerRoutine(unlockTime));
        }

        private System.Collections.IEnumerator UnlockTimerRoutine(DateTime unlockTime)
        {
            while (DateTime.UtcNow < unlockTime)
            {
                TimeSpan remaining = unlockTime - DateTime.UtcNow;
                if (_timerText != null)
                {
                    _timerText.text = FormatTime(remaining);
                    _timerText.gameObject.SetActive(true);
                }
                yield return new WaitForSeconds(1f);
            }

            OnUnlockReady();
        }

        private void OnUnlockReady()
        {
            StopTimer();
            if (_timerText != null) _timerText.gameObject.SetActive(false);
            _lockIcon?.SetActive(false);
            _readyIcon?.SetActive(true);
            StartBounceAnimation();

            if (_unlockParticles != null)
            {
                _unlockParticles.Play();
            }

            UISoundPlayer.Instance?.PlayChestUnlock();
        }

        private void StartBounceAnimation()
        {
            if (AccessibilityManager.Instance?.ReduceMotion == true) return;

            StartCoroutine(BounceAnimation());
        }

        private System.Collections.IEnumerator BounceAnimation()
        {
            Vector3 originalScale = transform.localScale;
            Vector3 bounceScale = originalScale * 1.1f;

            while (_currentChest != null && _currentChest.unlockTime <= DateTime.UtcNow)
            {
                yield return transform.ScaleTo(bounceScale, 0.8f, _bounceCurve);
                yield return transform.ScaleTo(originalScale, 0.8f, _bounceCurve);
                yield return new WaitForSeconds(2f);
            }

            transform.localScale = originalScale;
        }

        private void StopTimer()
        {
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }
        }

        private void OnSlotClicked()
        {
            if (_currentChest == null) return;

            if (_currentChest.unlockTime <= DateTime.UtcNow)
            {
                OpenChest();
            }
            else
            {
                ShowSpeedUpOption();
            }
        }

        private void OpenChest()
        {
            _isUnlocking = true;
            UISoundPlayer.Instance?.PlayChestUnlock();

            UIManager.Instance?.ShowChestUnlock(_currentChest.chestTypeId, _currentChest.rewards);
        }

        private void ShowSpeedUpOption()
        {
        }

        private string FormatTime(TimeSpan time)
        {
            if (time.TotalHours >= 1)
                return $"{(int)time.TotalHours}h {time.Minutes}m";
            else if (time.TotalMinutes >= 1)
                return $"{time.Minutes}m {time.Seconds}s";
            else
                return $"{time.Seconds}s";
        }

        private void OnDisable()
        {
            StopTimer();
        }

        private void OnDestroy()
        {
            StopTimer();
        }
    }

    [Serializable]
    public class ChestData
    {
        public int chestTypeId;
        public DateTime unlockTime;
        public System.Collections.Generic.List<EventBus.ChestReward> rewards;
        public bool isUnlocking;
    }
}