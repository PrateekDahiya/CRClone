using UnityEngine;
using CRClone.Battle.Simulation;
using CRClone.Core;

namespace CRClone.Battle.Presentation
{
    public class UnitView : MonoBehaviour, CRClone.Systems.IPoolable
    {
        [Header("Components")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Animator _animator;
        [SerializeField] private HealthBar _healthBar;
        [SerializeField] private GameObject _selectionRing;
        [SerializeField] private ParticleSystem _hitParticles;
        [SerializeField] private ParticleSystem _deathParticles;

        private Unit _unit;
        private Vector3 _targetPosition;
        private Vector3 _visualPosition;
        private float _interpolationSpeed = 15f;

        public void Initialize(Unit unit)
        {
            _unit = unit;
            _targetPosition = new Vector3(unit.Position.x, unit.Position.y, GetZPosition(unit.Position.y));
            _visualPosition = _targetPosition;
            transform.position = _visualPosition;
            _selectionRing.SetActive(false);

            // Setup visuals based on unit data
            SetupVisuals(unit);

            // Health bar
            _healthBar.SetHealth(unit.CurrentHP, unit.MaxHP);
        }

        private float GetZPosition(float y)
        {
            // Sort by Y position for proper 2D layering
            return -y * 0.01f;
        }

        private void SetupVisuals(Unit unit)
        {
            // Load sprite from card data
            // _spriteRenderer.sprite = Services.Get<AssetManager>().LoadSprite(unit.CardData.spriteId);
            
            // Set sorting order based on Y
            _spriteRenderer.sortingOrder = Mathf.RoundToInt(-unit.Position.y * 100) + 1000;

            // Set color based on player
            _spriteRenderer.color = unit.OwnerPlayerId == 1 ? Color.white : new Color(0.8f, 0.8f, 1f);
        }

        public void UpdatePosition(Vector2 position, float interpolationFactor)
        {
            _targetPosition = new Vector3(position.x, position.y, GetZPosition(position.y));
            
            // Smooth interpolation
            _visualPosition = Vector3.Lerp(_visualPosition, _targetPosition, Time.deltaTime * _interpolationSpeed);
            transform.position = _visualPosition;

            // Update sorting order
            _spriteRenderer.sortingOrder = Mathf.RoundToInt(-position.y * 100) + 1000;
        }

        public void UpdateHealth(int current, int max)
        {
            _healthBar.SetHealth(current, max);
        }

        public void UpdateState(UnitState state)
        {
            if (_animator != null)
            {
                _animator.SetInteger("State", (int)state);
            }
        }

        public void PlayHitEffect()
        {
            if (_hitParticles != null)
                _hitParticles.Play();
        }

        public void PlayDeathAnimation(DeathCause cause)
        {
            if (_animator != null)
                _animator.SetTrigger("Die");

            if (_deathParticles != null)
                _deathParticles.Play();

            _healthBar.gameObject.SetActive(false);
            _spriteRenderer.enabled = false;
        }

        public void OnSpawn()
        {
            gameObject.SetActive(true);
            _spriteRenderer.enabled = true;
            _healthBar.gameObject.SetActive(true);
            _selectionRing.SetActive(false);
            _animator.OrNull()?.Rebind();
        }

        public void OnDespawn()
        {
            // Cleanup handled by BattleView
        }

        public void SetSelected(bool selected)
        {
            _selectionRing.SetActive(selected);
        }
    }
}