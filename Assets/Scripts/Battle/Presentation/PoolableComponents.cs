using UnityEngine;
using CRClone.Systems;
using CRClone.Core;
using CRClone.Battle.Simulation;

namespace CRClone.Battle.Presentation
{
    public class UnitPoolable : MonoBehaviour, IPoolable
    {
        private UnitView _unitView;
        private Unit _unit;
        private Animator _animator;
        private HealthBar _healthBar;
        private SelectionRing _selectionRing;
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _unitView = GetComponent<UnitView>();
            _unit = GetComponent<Unit>();
            _animator = GetComponent<Animator>();
            _healthBar = GetComponentInChildren<HealthBar>();
            _selectionRing = GetComponentInChildren<SelectionRing>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void OnSpawn()
        {
            gameObject.SetActive(true);
            if (_spriteRenderer != null) _spriteRenderer.enabled = true;
            if (_healthBar != null) _healthBar.gameObject.SetActive(true);
            if (_selectionRing != null) _selectionRing.gameObject.SetActive(false);
            if (_animator != null) _animator.Rebind();
        }

        public void OnDespawn()
        {
            if (_unitView != null) _unitView.SetSelected(false);
        }
    }

    public class BuildingPoolable : MonoBehaviour, IPoolable
    {
        private BuildingView _buildingView;
        private Building _building;
        private Animator _animator;
        private HealthBar _healthBar;

        private void Awake()
        {
            _buildingView = GetComponent<BuildingView>();
            _building = GetComponent<Building>();
            _animator = GetComponent<Animator>();
            _healthBar = GetComponentInChildren<HealthBar>();
        }

        public void OnSpawn()
        {
            gameObject.SetActive(true);
            if (_healthBar != null) _healthBar.gameObject.SetActive(true);
            if (_animator != null) _animator.Rebind();
        }

        public void OnDespawn()
        {
        }
    }

    public class SpellPoolable : MonoBehaviour, IPoolable
    {
        private SpellEffectView _spellView;
        private SpellEffect _spellEffect;
        private ParticleSystem _particleSystem;

        private void Awake()
        {
            _spellView = GetComponent<SpellEffectView>();
            _spellEffect = GetComponent<SpellEffect>();
            _particleSystem = GetComponentInChildren<ParticleSystem>();
        }

        public void OnSpawn()
        {
            gameObject.SetActive(true);
            if (_particleSystem != null)
            {
                _particleSystem.Clear();
                _particleSystem.Play();
            }
        }

        public void OnDespawn()
        {
            if (_particleSystem != null)
                _particleSystem.Stop();
        }
    }

    public class ProjectilePoolable : MonoBehaviour, IPoolable
    {
        private ProjectileView _projectileView;
        private Projectile _projectile;
        private TrailRenderer _trailRenderer;
        private ParticleSystem _impactParticles;

        private void Awake()
        {
            _projectileView = GetComponent<ProjectileView>();
            _projectile = GetComponent<Projectile>();
            _trailRenderer = GetComponent<TrailRenderer>();
            _impactParticles = GetComponentInChildren<ParticleSystem>();
        }

        public void OnSpawn()
        {
            gameObject.SetActive(true);
            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
                _trailRenderer.emitting = true;
            }
        }

        public void OnDespawn()
        {
            if (_trailRenderer != null)
                _trailRenderer.emitting = false;
        }
    }

    public class TowerPoolable : MonoBehaviour, IPoolable
    {
        private TowerView _towerView;
        private Tower _tower;
        private Animator _animator;
        private HealthBar _healthBar;

        private void Awake()
        {
            _towerView = GetComponent<TowerView>();
            _tower = GetComponent<Tower>();
            _animator = GetComponent<Animator>();
            _healthBar = GetComponentInChildren<HealthBar>();
        }

        public void OnSpawn()
        {
            gameObject.SetActive(true);
            if (_healthBar != null) _healthBar.gameObject.SetActive(true);
            if (_animator != null) _animator.Rebind();
        }

        public void OnDespawn()
        {
        }
    }
}