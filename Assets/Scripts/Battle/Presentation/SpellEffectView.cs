using UnityEngine;
using CRClone.Battle.Simulation;

namespace CRClone.Battle.Presentation
{
    public class SpellEffectView : MonoBehaviour, CRClone.Systems.IPoolable
    {
        [SerializeField] private ParticleSystem _particleSystem;
        [SerializeField] private SpriteRenderer _areaIndicator;
        [SerializeField] private LineRenderer _tornadoLine;

        private SpellEffect _spellEffect;

        public void Initialize(SpellEffect spellEffect)
        {
            _spellEffect = spellEffect;
            transform.position = new Vector3(spellEffect.CenterPosition.x, spellEffect.CenterPosition.y, -spellEffect.CenterPosition.y * 0.01f);

            SetupVisuals(spellEffect);
        }

        private void SetupVisuals(SpellEffect spell)
        {
            if (_areaIndicator != null)
            {
                // Draw circle for radius
                _areaIndicator.enabled = spell.Type != SpellType.Utility || spell.SpellData.cardName == "Tornado";
            }

            if (_particleSystem != null)
            {
                // Configure particles based on spell
                var main = _particleSystem.main;
                main.duration = spell.Duration;
                _particleSystem.Play();
            }

            if (spell.SpellData.cardName == "Tornado" && _tornadoLine != null)
            {
                _tornadoLine.enabled = true;
            }
        }

        public void Update(SpellEffect spell)
        {
            // Update visual effects
            if (spell.SpellData.cardName == "Tornado" && _tornadoLine != null)
            {
                // Draw swirling line
                int segments = 20;
                _tornadoLine.positionCount = segments;
                for (int i = 0; i < segments; i++)
                {
                    float t = (float)i / segments;
                    float angle = t * Mathf.PI * 4 + Time.time * 5f;
                    float radius = spell.Radius * (1f - t * 0.5f);
                    Vector3 pos = new Vector3(
                        spell.CenterPosition.x + Mathf.Cos(angle) * radius,
                        spell.CenterPosition.y + Mathf.Sin(angle) * radius,
                        -spell.CenterPosition.y * 0.01f
                    );
                    _tornadoLine.SetPosition(i, pos);
                }
            }

            // Update particle system
            if (_particleSystem != null && !_particleSystem.isPlaying)
            {
                _particleSystem.Play();
            }
        }

        public void OnSpawn()
        {
            gameObject.SetActive(true);
        }

        public void OnDespawn()
        {
            if (_particleSystem != null)
                _particleSystem.Stop();
        }
    }
}