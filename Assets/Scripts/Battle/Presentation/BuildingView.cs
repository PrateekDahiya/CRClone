using UnityEngine;
using UnityEngine.UI;
using CRClone.Battle.Simulation;

namespace CRClone.Battle.Presentation
{
    public class BuildingView : MonoBehaviour, CRClone.Systems.IPoolable
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Animator _animator;
        [SerializeField] private HealthBar _healthBar;
        [SerializeField] private GameObject _retractedVisual;

        private Building _building;

        public void Initialize(Building building)
        {
            _building = building;
            transform.position = new Vector3(building.Position.x, building.Position.y, -building.Position.y * 0.01f);
            
            SetupVisuals(building);
            _healthBar.SetHealth(building.CurrentHP, building.MaxHP);
            _retractedVisual.SetActive(building.IsRetracted);
        }

        private void SetupVisuals(Building building)
        {
            // _spriteRenderer.sprite = Services.Get<AssetManager>().LoadSprite(building.CardData.spriteId);
            _spriteRenderer.sortingOrder = Mathf.RoundToInt(-building.Position.y * 100) + 500;
            _spriteRenderer.color = building.OwnerPlayerId == 1 ? Color.white : new Color(0.8f, 0.8f, 1f);
        }

        public void UpdateHealth(int current, int max)
        {
            _healthBar.SetHealth(current, max);
        }

        public void UpdateState(bool isRetracted)
        {
            _retractedVisual.SetActive(isRetracted);
            if (_animator != null)
                _animator.SetBool("Retracted", isRetracted);
        }

        public void PlayDestructionAnimation()
        {
            if (_animator != null)
                _animator.SetTrigger("Destroy");
            _healthBar.gameObject.SetActive(false);
        }

        public void OnSpawn()
        {
            gameObject.SetActive(true);
            _healthBar.gameObject.SetActive(true);
            _animator?.Rebind();
        }

        public void OnDespawn() { }
    }
}