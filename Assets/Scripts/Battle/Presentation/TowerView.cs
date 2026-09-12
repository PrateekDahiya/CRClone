using UnityEngine;
using CRClone.Battle.Simulation;

namespace CRClone.Battle.Presentation
{
    public class TowerView : MonoBehaviour, CRClone.Systems.IPoolable
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Animator _animator;
        [SerializeField] private HealthBar _healthBar;
        [SerializeField] private GameObject _activationEffect;
        [SerializeField] private ParticleSystem _hitParticles;
        [SerializeField] private ParticleSystem _destroyParticles;

        public Tower Tower { get; private set; }

        public void Initialize(Tower tower)
        {
            Tower = tower;
            transform.position = new Vector3(tower.Position.x, tower.Position.y, -tower.Position.y * 0.01f);
            
            SetupVisuals(tower);
            _healthBar.SetHealth(tower.CurrentHP, tower.MaxHP);
            _activationEffect.SetActive(false);
        }

        private void SetupVisuals(Tower tower)
        {
            // _spriteRenderer.sprite = Services.Get<AssetManager>().LoadSprite(GetTowerSprite(tower.Type));
            _spriteRenderer.sortingOrder = Mathf.RoundToInt(-tower.Position.y * 100) + 200;
            _spriteRenderer.color = tower.OwnerPlayerId == 1 ? Color.white : new Color(0.8f, 0.8f, 1f);
        }

        public void UpdateHealth(int current, int max)
        {
            _healthBar.SetHealth(current, max);
        }

        public void UpdateActivation(bool activated)
        {
            _activationEffect.SetActive(activated);
            if (_animator != null)
                _animator.SetBool("Activated", activated);
        }

        public void PlayHitEffect()
        {
            if (_hitParticles != null)
                _hitParticles.Play();
        }

        public void PlayDestructionAnimation()
        {
            if (_animator != null)
                _animator.SetTrigger("Destroy");
            if (_destroyParticles != null)
                _destroyParticles.Play();
            _healthBar.gameObject.SetActive(false);
        }

        public void PlayActivationEffect()
        {
            _activationEffect.SetActive(true);
            if (_animator != null)
                _animator.SetTrigger("Activate");
        }

        public void OnSpawn() { }
        public void OnDespawn() { }
    }
}