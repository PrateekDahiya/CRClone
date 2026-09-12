using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CRClone.UI.Components
{
    public class LoadingUI : MonoBehaviour
    {
        [Header("Progress Bar")]
        [SerializeField] private Image _progressBar;
        [SerializeField] private Text _progressText;
        [SerializeField] private TMP_Text _tmpProgressText;
        [SerializeField] private GameObject _spinner;
        [SerializeField] private float _spinSpeed = 360f;

        [Header("Tips")]
        [SerializeField] private Text _tipText;
        [SerializeField] private TMP_Text _tmpTipText;
        [SerializeField] private string[] _loadingTips;
        [SerializeField] private float _tipChangeInterval = 5f;

        private Coroutine _tipCoroutine;
        private int _currentTipIndex;

        private void Awake()
        {
            if (_progressBar != null) _progressBar.fillAmount = 0f;
        }

        private void OnEnable()
        {
            SetProgress(0f);
            StartTips();
        }

        private void OnDisable()
        {
            StopTips();
        }

        private void Update()
        {
            if (_spinner != null && _spinner.activeInHierarchy)
            {
                _spinner.transform.Rotate(0f, 0f, -_spinSpeed * Time.unscaledDeltaTime);
            }
        }

        public void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            
            if (_progressBar != null)
            {
                _progressBar.fillAmount = progress;
            }

            string progressStr = $"{progress * 100:0}%";
            if (_progressText != null) _progressText.text = progressStr;
            if (_tmpProgressText != null) _tmpProgressText.text = progressStr;
        }

        private void StartTips()
        {
            if (_loadingTips == null || _loadingTips.Length == 0) return;
            if (_tipCoroutine != null) StopCoroutine(_tipCoroutine);
            _tipCoroutine = StartCoroutine(TipRoutine());
        }

        private void StopTips()
        {
            if (_tipCoroutine != null)
            {
                StopCoroutine(_tipCoroutine);
                _tipCoroutine = null;
            }
        }

        private IEnumerator TipRoutine()
        {
            _currentTipIndex = UnityEngine.Random.Range(0, _loadingTips.Length);
            UpdateTip();

            while (true)
            {
                yield return new WaitForSecondsRealtime(_tipChangeInterval);
                _currentTipIndex = (_currentTipIndex + 1) % _loadingTips.Length;
                UpdateTip();
            }
        }

        private void UpdateTip()
        {
            if (_tipText != null) _tipText.text = _loadingTips[_currentTipIndex];
            if (_tmpTipText != null) _tmpTipText.text = _loadingTips[_currentTipIndex];
        }

        public void SetTips(string[] tips)
        {
            _loadingTips = tips;
            if (_tipCoroutine != null)
            {
                StopTips();
                StartTips();
            }
        }
    }
}