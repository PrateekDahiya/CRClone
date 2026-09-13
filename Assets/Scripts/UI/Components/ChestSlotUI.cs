using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.UI.Animation;

namespace CRClone.UI.Components
{
    public class ChestSlotUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image _chestImage;
        [SerializeField] private Text _timerText;
        [SerializeField] private Image _progressFill;
        [SerializeField] private GameObject _lockIcon;
        [SerializeField] private GameObject _readyIcon;
        [SerializeField] private GameObject _emptyState;
        [SerializeField] private Button _slotButton;
        [SerializeField] private ParticleSystem _unlockParticles;
        [SerializeField] private float _bounceInterval = 2f;
        [SerializeField] private float _bounceScale = 1.15f;

        [Header("Chest Sprites")]
        [SerializeField] private Sprite[] _chestSprites;

        private int _slotIndex;
        private ChestData _chestData;
        private Coroutine _timerCoroutine;
        private Coroutine _bounceCoroutine;
        private bool _isUnlocked;

        public int SlotIndex => _slotIndex;

        public void Initialize(int index)
        {
            _slotIndex = index;
            _slotButton?.onClick.AddListener(OnSlotClicked);
            SetEmpty();
        }

        public void SetChestData(ChestData chestData)
        {
            _chestData = chestData;
            _isUnlocked = false;

            if (chestData == null || chestData.chestTypeId < 0)
            {
                SetEmpty();
                return;
            }

            gameObject.SetActive(true);
            _emptyState?.SetActive(false);
            _chestImage?.gameObject.SetActive(true);

            if (_chestImage != null && chestData.chestTypeId < _chestSprites.Length)
            {
                _chestImage.sprite = _chestSprites[chestData.chestTypeId];
            }

            if (chestData.unlockTime <= DateTime.UtcNow)
            {
                OnUnlockReady();
            }
            else
            {
                StartUnlockTimer(chestData.unlockTime);
            }
        }

        private void SetEmpty()
        {
            _chestData = null;
            StopTimers();
            _chestImage?.gameObject.SetActive(false);
            _timerText?.gameObject.SetActive(false);
            _progressFill?.gameObject.SetActive(false);
            _lockIcon?.SetActive(false);
            _readyIcon?.SetActive(false);
            _emptyState?.SetActive(true);
        }

        private void StartUnlockTimer(DateTime unlockTime)
        {
            StopTimers();
            _lockIcon?.SetActive(true);
            _readyIcon?.SetActive(false);
            _progressFill?.gameObject.SetActive(true);
            _timerText?.gameObject.SetActive(true);

            _timerCoroutine = StartCoroutine(UnlockTimerRoutine(unlockTime));
        }

        private IEnumerator UnlockTimerRoutine(DateTime unlockTime)
        {
            DateTime startTime = unlockTime.AddHours(-3); // Assume 3 hour unlock
            float totalDuration = (float)(unlockTime - startTime).TotalSeconds;

            while (DateTime.UtcNow < unlockTime)
            {
                TimeSpan remaining = unlockTime - DateTime.UtcNow;
                
                if (_timerText != null)
                {
                    _timerText.text = FormatTime(remaining);
                }

                if (_progressFill != null)
                {
                    float elapsed = (float)(DateTime.UtcNow - startTime).TotalSeconds;
                    _progressFill.fillAmount = Mathf.Clamp01(elapsed / totalDuration);
                }

                yield return new WaitForSeconds(1f);
            }

            OnUnlockReady();
        }

        private void OnUnlockReady()
        {
            StopTimers();
            _isUnlocked = true;
            _lockIcon?.SetActive(false);
            _readyIcon?.SetActive(true);
            _progressFill?.gameObject.SetActive(false);
            _timerText?.gameObject.SetActive(false);

            if (_unlockParticles != null)
            {
                _unlockParticles.Play();
            }

            UISoundPlayer.Instance?.PlayChestUnlock();
            StartBounceAnimation();
        }

        private void StartBounceAnimation()
        {
            if (AccessibilityManager.Instance?.ReduceMotion == true) return;
            
            _bounceCoroutine = StartCoroutine(BounceAnimation());
        }

        private IEnumerator BounceAnimation()
        {
            while (_isUnlocked)
            {
                yield return new WaitForSeconds(_bounceInterval);
                
                if (!_isUnlocked) break;

                yield return transform.ScaleTo(Vector3.one * _bounceScale, 0.2f, AnimationCurve.EaseInOut(0, 0, 1, 1), null, true);
                yield return transform.ScaleTo(Vector3.one, 0.2f, AnimationCurve.EaseInOut(0, 0, 1, 1), null, true);
            }
        }

        private void StopTimers()
        {
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }
            if (_bounceCoroutine != null)
            {
                StopCoroutine(_bounceCoroutine);
                _bounceCoroutine = null;
            }
        }

        private void OnSlotClicked()
        {
            if (_chestData == null) return;

            if (_isUnlocked)
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
            _isUnlocked = false;
            StopTimers();

            UISoundPlayer.Instance?.PlayChestUnlock();

            var chestUnlockScreen = UIManager.Instance?.ShowScreen(ScreenType.ChestUnlock);
            // ChestUnlockScreen would handle the animation and rewards
        }

        private void ShowSpeedUpOption()
        {
            var modal = UIManager.Instance?.ShowModal(Resources.Load<GameObject>("UI/SpeedUpChestModal"));
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

        private void OnDestroy()
        {
            StopTimers();
        }
    }

    [Serializable]
    public class ChestData
    {
        public int chestTypeId;
        public DateTime unlockTime;
        public System.Collections.Generic.List<EventBus.ChestReward> rewards;
    }
}