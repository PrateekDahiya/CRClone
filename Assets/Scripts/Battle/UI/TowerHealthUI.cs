using UnityEngine;
using UnityEngine.UI;
using CRClone.Battle.Simulation;

namespace CRClone.Battle.UI
{
    public class TowerHealthUI : MonoBehaviour
    {
        [SerializeField] private Image _healthFill;
        [SerializeField] private Text _hpText;
        [SerializeField] private GameObject _crownIcon;
        [SerializeField] private Color _fullColor = Color.green;
        [SerializeField] private Color _lowColor = Color.red;

        private Tower _tower;

        public void Initialize(Tower tower)
        {
            _tower = tower;
            UpdateDisplay();
        }

        private void Update()
        {
            if (_tower != null)
            {
                UpdateDisplay();
            }
        }

        private void UpdateDisplay()
        {
            if (_tower.MaxHP <= 0) return;

            float percent = (float)_tower.CurrentHP / _tower.MaxHP;

            if (_healthFill != null)
            {
                _healthFill.fillAmount = percent;
                _healthFill.color = Color.Lerp(_lowColor, _fullColor, percent);
            }

            if (_hpText != null)
            {
                _hpText.text = $"{_tower.CurrentHP}/{_tower.MaxHP}";
            }

            if (_crownIcon != null)
            {
                _crownIcon.SetActive(_tower.Type != TowerType.King && _tower.IsDead);
            }
        }

        public void PlayDamageEffect()
        {
            // Shake or flash animation
        }
    }
}