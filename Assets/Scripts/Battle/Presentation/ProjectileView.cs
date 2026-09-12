using UnityEngine;
using CRClone.Battle.Simulation;

namespace CRClone.Battle.Presentation
{
    public class ProjectileView : MonoBehaviour, CRClone.Systems.IPoolable
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private TrailRenderer _trailRenderer;
        [SerializeField] private ParticleSystem _impactParticles;

        private Projectile _projectile;

        public void Initialize(Projectile projectile)
        {
            _projectile = projectile;
            transform.position = new Vector3(projectile.Position.x, projectile.Position.y, -projectile.Position.y * 0.01f);
            
            // Setup visual based on projectile type
            // _spriteRenderer.sprite = Services.Get<AssetManager>().LoadSprite(GetProjectileSprite(projectile));
        }

        public void UpdatePosition(Vector2 position)
        {
            transform.position = new Vector3(position.x, position.y, -position.y * 0.01f);
        }

        public void PlayImpact()
        {
            if (_impactParticles != null)
            {
                _impactParticles.Play();
            }
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
}