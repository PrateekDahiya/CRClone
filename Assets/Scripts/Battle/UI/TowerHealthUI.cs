using System;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Battle.Simulation;
using CRClone.UI.Animation;

namespace CRClone.Battle.UI
{
    public class TowerHealthUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image _healthFill;
        [SerializeField] private Text _hpText;
        [SerializeField] private GameObject _crownIcon;
        [SerializeField] private Image _crownImage;
        [SerializeField] private Sprite _crownFilled;
        [SerializeField] private Sprite _crownEmpty;
        [SerializeField] private GameObject _kingCrownIcon;

        [Header("Colors")]
        [SerializeField] private Color _fullColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _mediumColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color _lowColor = new Color(1f, 0.2f, 0.2f);
        [SerializeField] private Color _criticalColor = new Color(0.8f, 0f, 0f);

        [Header("Damage Effect")]
        [SerializeField] private float _damageFlashDuration = 0.1f;
        [SerializeField] private Color _damageFlashColor = Color.red;
        [SerializeField] private float _shakeMagnitude = 5f;
        [SerializeField] private float _shakeDuration = 0.2f;

        [Header("Destroy Animation")]
        [SerializeField] private float _destroyDuration = 2f;
        [SerializeField] private AnimationCurve _destroyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private ParticleSystem _destroyParticles;
        [SerializeField] private GameObject _towerModel;

        private Tower _tower;
        private int _previousHP;
        private bool _isDestroyed;
        private Coroutine _destroyCoroutine;

        public void Initialize(Tower tower)
        {
            _tower = tower;
            _previousHP = tower?.CurrentHP ?? 0;
            _isDestroyed = false;
            UpdateDisplay();
        }

        private void Update()
        {
            if (_tower == null || _isDestroyed) return;

            UpdateDisplay();
            CheckDamage();
        }

        private void UpdateDisplay()
        {
            if (_tower.MaxHP <= 0) return;

            float percent = (float)_tower.CurrentHP / _tower.MaxHP;

            if (_healthFill != null)
            {
                if (AccessibilityManager.Instance?.ReduceMotion == true)
                {
                    _healthFill.fillAmount = percent;
                }
                else
                {
                    _healthFill.fillAmount = Mathf.Lerp(_healthFill.fillAmount, percent, Time.deltaTime * 5f);
                }

                _healthFill.color = GetHealthColor(percent);
            }

            if (_hpText != null)
            {
                _hpText.text = $"{_tower.CurrentHP}/{_tower.MaxHP}";
            }

            UpdateCrownDisplay();
        }

        private Color GetHealthColor(float percent)
        {
            if (percent > 0.6f)
                return Color.Lerp(_mediumColor, _fullColor, (percent - 0.6f) / 0.4f);
            else if (percent > 0.3f)
                return Color.Lerp(_lowColor, _mediumColor, (percent - 0.3f) / 0.3f);
            else
                return Color.Lerp(_criticalColor, _lowColor, percent / 0.3f);
        }

        private void UpdateCrownDisplay()
        {
            if (_tower.Type == TowerType.King)
            {
                _crownIcon?.SetActive(false);
                _kingCrownIcon?.SetActive(_tower.IsDead);
            }
            else
            {
                _crownIcon?.SetActive(_tower.IsDead);
                if (_crownImage != null)
                {
                    _crownImage.sprite = _tower.IsDead ? _crownFilled : _crownEmpty;
                }
                _kingCrownIcon?.SetActive(false);
            }
        }

        private void CheckDamage()
        {
            if (_tower.CurrentHP < _previousHP)
            {
                int damage = _previousHP - _tower.CurrentHP;
                PlayDamageEffect(damage);
            }
            else if (_tower.CurrentHP <= 0 && !_isDestroyed)
            {
                PlayDestroyAnimation();
            }

            _previousHP = _tower.CurrentHP;
        }

        public void PlayDamageEffect(int damage)
        {
            if (AccessibilityManager.Instance?.ReduceMotion == true) return;

            StartCoroutine(DamageFlashCoroutine());
            StartCoroutine(ShakeCoroutine());
        }

        private System.Collections.IEnumerator DamageFlashCoroutine()
        {
            if (_healthFill == null) yield break;

            Color originalColor = _healthFill.color;
            _healthFill.color = _damageFlashColor;

            yield return new WaitForSeconds(_damageFlashDuration);

            _healthFill.color = originalColor;
        }

        private System.Collections.IEnumerator ShakeCoroutine()
        {
            Vector3 originalPos = transform.localPosition;
            float elapsed = 0f;

            while (elapsed < _shakeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _shakeDuration;
                float magnitude = _shakeMagnitude * (1f - t);

                float offsetX = UnityEngine.Random.Range(-magnitude, magnitude);
                float offsetY = UnityEngine.Random.Range(-magnitude, magnitude);

                transform.localPosition = originalPos + new Vector3(offsetX, offsetY, 0);
                yield return null;
            }

            transform.localPosition = originalPos;
        }

        private void PlayDestroyAnimation()
        {
            _isDestroyed = true;
            if (_destroyCoroutine != null) StopCoroutine(_destroyCoroutine);
            _destroyCoroutine = StartCoroutine(DestroyAnimationCoroutine());
        }

        private System.Collections.IEnumerator DestroyAnimationCoroutine()
        {
            if (_destroyParticles != null)
            {
                _destroyParticles.Play();
            }

            UISoundPlayer.Instance?.PlayDefeat();

            float elapsed = 0f;
            Vector3 originalScale = _towerModel != null ? _towerModel.transform.localScale : transform.localScale;
            Quaternion originalRotation = _towerModel != null ? _towerModel.transform.localRotation : transform.localRotation;

            while (elapsed < _destroyDuration)
            {
                elapsed += Time.deltaTime;
                float t = _destroyCurve.Evaluate(elapsed / _destroyDuration);

                if (_towerModel != null)
                {
                    _towerModel.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
                    _towerModel.transform.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-15f, 15f) * t);
                }
                else
                {
                    transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
                }

                if (_healthFill != null)
                {
                    _healthFill.color = Color.Lerp(_healthFill.color, _criticalColor, t);
                }

                yield return null;
            }

            if (_towerModel != null)
            {
                _towerModel.transform.localScale = Vector3.zero;
            }
            else
            {
                transform.localScale = Vector3.zero;
            }

            gameObject.SetActive(false);
        }

        public void SetTower(Tower tower)
        {
            _tower = tower;
            _previousHP = tower?.CurrentHP ?? 0;
            _isDestroyed = false;
            gameObject.SetActive(true);
            UpdateDisplay();
        }

        public TowerType GetTowerType()
        {
            return _tower?.Type ?? TowerType.King;
        }

        public void PlayKingActivation()
        {
            if (AccessibilityManager.Instance?.ReduceMotion == true) return;

            StartCoroutine(KingActivationCoroutine());
        }

        private System.Collections.IEnumerator KingActivationCoroutine()
        {
            // Flash gold color and pulse
            Color originalColor = _healthFill.color;
            Color kingColor = new Color(1f, 0.85f, 0f);

            for (int i = 0; i < 3; i++)
            {
                _healthFill.color = kingColor;
                yield return new WaitForSeconds(0.1f);
                _healthFill.color = originalColor;
                yield return new WaitForSeconds(0.1f);
            }

            // Show king crown icon
            if (_kingCrownIcon != null)
            {
                _kingCrownIcon.SetActive(true);
            }
        }
    }
}