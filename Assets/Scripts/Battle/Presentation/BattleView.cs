using System;
using System.Collections.Generic;
using UnityEngine;
using CRClone.Core;
using CRClone.Systems;
using CRClone.UI;
using CRClone.Battle.Simulation;

namespace CRClone.Battle.Presentation
{
    public class BattleView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _arenaRoot;
        [SerializeField] private Camera _battleCamera;
        [SerializeField] private Transform _p1Side;
        [SerializeField] private Transform _p2Side;
        [SerializeField] private RiverRenderer _riverRenderer;

        [Header("Prefabs")]
        [SerializeField] private GameObject _unitViewPrefab;
        [SerializeField] private GameObject _buildingViewPrefab;
        [SerializeField] private GameObject _projectileViewPrefab;
        [SerializeField] private GameObject _spellEffectViewPrefab;
        [SerializeField] private GameObject _towerViewPrefab;

        [Header("Settings")]
        [SerializeField] private float _interpolationFactor = 0.1f;
        [SerializeField] private float _cameraShakeIntensity = 0.2f;

        private BattleSimulation _simulation;
        private Dictionary<uint, UnitView> _unitViews = new();
        private Dictionary<uint, BuildingView> _buildingViews = new();
        private Dictionary<uint, ProjectileView> _projectileViews = new();
        private Dictionary<uint, SpellEffectView> _spellViews = new();
        private Dictionary<uint, TowerView> _towerViews = new();

        private Vector3 _cameraStartPos;
        private float _shakeTimer = 0f;

        public void Initialize(BattleSimulation simulation)
        {
            _simulation = simulation;
            _cameraStartPos = _battleCamera.transform.position;

            // Subscribe to events
            EventBus.OnUnitSpawned += OnUnitSpawned;
            EventBus.OnUnitDied += OnUnitDied;
            EventBus.OnBuildingPlaced += OnBuildingPlaced;
            EventBus.OnBuildingDestroyed += OnBuildingDestroyed;
            EventBus.OnSpellCast += OnSpellCast;
            EventBus.OnTowerDamaged += OnTowerDamaged;
            EventBus.OnTowerDestroyed += OnTowerDestroyed;
            EventBus.OnKingTowerActivated += OnKingTowerActivated;
            EventBus.OnBattleEnded += OnBattleEnded;

            // Create tower views
            CreateTowerViews();

            // Create initial entity views
            CreateInitialViews();

            Debug.Log("[BattleView] Initialized");
        }

        private void CreateTowerViews()
        {
            foreach (var tower in _simulation.Towers)
            {
                var view = Instantiate(_towerViewPrefab, _arenaRoot).GetComponent<TowerView>();
                view.Initialize(tower);
                _towerViews[tower.Id] = view;

                // Position on correct side
                if (tower.OwnerPlayerId == 1)
                    view.transform.SetParent(_p1Side);
                else
                    view.transform.SetParent(_p2Side);
            }
        }

        private void CreateInitialViews()
        {
            foreach (var unit in _simulation.Units)
            {
                CreateUnitView(unit);
            }
            foreach (var building in _simulation.Buildings)
            {
                CreateBuildingView(building);
            }
        }

        private void Update()
        {
            if (_simulation == null || _simulation.Status != BattleStatus.Playing) return;

            // Update entity positions (interpolated)
            UpdateUnitViews();
            UpdateBuildingViews();
            UpdateProjectileViews();
            UpdateSpellViews();
            UpdateTowerViews();

            // Camera shake
            UpdateCameraShake();
        }

        private void UpdateUnitViews()
        {
            // Update existing
            foreach (var kvp in _unitViews)
            {
                if (_simulation.GetEntity(kvp.Key) is Unit unit)
                {
                    kvp.Value.UpdatePosition(unit.Position, _interpolationFactor);
                    kvp.Value.UpdateHealth(unit.CurrentHP, unit.MaxHP);
                    kvp.Value.UpdateState(unit.State);
                }
                else
                {
                    // Unit died - will be cleaned up in OnUnitDied
                }
            }

            // Add new units
            foreach (var unit in _simulation.Units)
            {
                if (!_unitViews.ContainsKey(unit.Id))
                {
                    CreateUnitView(unit);
                }
            }
        }

        private void CreateUnitView(Unit unit)
        {
            var view = Instantiate(_unitViewPrefab, _arenaRoot).GetComponent<UnitView>();
            view.Initialize(unit);
            _unitViews[unit.Id] = view;

            // Set side parent
            if (unit.OwnerPlayerId == 1)
                view.transform.SetParent(_p1Side);
            else
                view.transform.SetParent(_p2Side);
        }

        private void OnUnitSpawned(EventBus.UnitSpawnedEvent evt)
        {
            var unit = _simulation.GetEntity(evt.entityId) as Unit;
            if (unit != null)
            {
                CreateUnitView(unit);
            }
        }

        private void OnUnitDied(EventBus.UnitDiedEvent evt)
        {
            if (_unitViews.TryGetValue(evt.entityId, out var view))
            {
                view.PlayDeathAnimation((DeathCause)evt.cause);
                _unitViews.Remove(evt.entityId);
                // Return to pool after animation
                StartCoroutine(ReturnToPoolAfter(view.gameObject, 1f));
            }
        }

        private System.Collections.IEnumerator ReturnToPoolAfter(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            Services.Get<PoolManager>().Despawn(obj.name, obj);
        }

        private void UpdateBuildingViews()
        {
            foreach (var building in _simulation.Buildings)
            {
                if (!_buildingViews.ContainsKey(building.Id))
                {
                    CreateBuildingView(building);
                }
                else
                {
                    _buildingViews[building.Id].UpdateHealth(building.CurrentHP, building.MaxHP);
                    _buildingViews[building.Id].UpdateState(building.IsRetracted);
                }
            }
        }

        private void CreateBuildingView(Building building)
        {
            var view = Instantiate(_buildingViewPrefab, _arenaRoot).GetComponent<BuildingView>();
            view.Initialize(building);
            _buildingViews[building.Id] = view;

            if (building.OwnerPlayerId == 1)
                view.transform.SetParent(_p1Side);
            else
                view.transform.SetParent(_p2Side);
        }

        private void OnBuildingPlaced(EventBus.BuildingPlacedEvent evt)
        {
            var building = _simulation.GetEntity(evt.entityId) as Building;
            if (building != null) CreateBuildingView(building);
        }

        private void OnBuildingDestroyed(EventBus.BuildingDestroyedEvent evt)
        {
            if (_buildingViews.TryGetValue(evt.entityId, out var view))
            {
                view.PlayDestructionAnimation();
                _buildingViews.Remove(evt.entityId);
                StartCoroutine(ReturnToPoolAfter(view.gameObject, 2f));
            }
        }

        private void UpdateProjectileViews()
        {
            // Update existing
            var toRemove = new List<uint>();
            foreach (var kvp in _projectileViews)
            {
                var proj = _simulation.GetEntity(kvp.Key) as Projectile;
                if (proj != null)
                {
                    kvp.Value.UpdatePosition(proj.Position);
                }
                else
                {
                    toRemove.Add(kvp.Key);
                }
            }

            // Add new
            foreach (var proj in _simulation.Projectiles)
            {
                if (!_projectileViews.ContainsKey(proj.Id))
                {
                    CreateProjectileView(proj);
                }
            }

            // Remove dead
            foreach (var id in toRemove)
            {
                if (_projectileViews.TryGetValue(id, out var view))
                {
                    _projectileViews.Remove(id);
                    Services.Get<PoolManager>().Despawn(view.gameObject.name, view.gameObject);
                }
            }
        }

        private void CreateProjectileView(Projectile projectile)
        {
            var view = Instantiate(_projectileViewPrefab, _arenaRoot).GetComponent<ProjectileView>();
            view.Initialize(projectile);
            _projectileViews[projectile.Id] = view;
        }

        private void UpdateSpellViews()
        {
            var toRemove = new List<uint>();
            foreach (var kvp in _spellViews)
            {
                var spell = _simulation.GetEntity(kvp.Key) as SpellEffect;
                if (spell != null && !spell.IsFinished)
                {
                    kvp.Value.Update(spell);
                }
                else
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var spell in _simulation.ActiveSpells)
            {
                if (!_spellViews.ContainsKey(spell.Id))
                {
                    CreateSpellView(spell);
                }
            }

            foreach (var id in toRemove)
            {
                if (_spellViews.TryGetValue(id, out var view))
                {
                    _spellViews.Remove(id);
                    Services.Get<PoolManager>().Despawn(view.gameObject.name, view.gameObject);
                }
            }
        }

        private void CreateSpellView(SpellEffect spell)
        {
            var view = Instantiate(_spellEffectViewPrefab, _arenaRoot).GetComponent<SpellEffectView>();
            view.Initialize(spell);
            _spellViews[spell.Id] = view;
        }

        private void OnSpellCast(EventBus.SpellCastEvent evt)
        {
            // Spell view created in UpdateSpellViews
            // Play cast sound/effect at position
            Services.Get<AudioManager>().PlaySFX("spell_cast", evt.position);
        }

        private void UpdateTowerViews()
        {
            foreach (var kvp in _towerViews)
            {
                var tower = _simulation.GetEntity(kvp.Key) as Tower;
                if (tower != null)
                {
                    kvp.Value.UpdateHealth(tower.CurrentHP, tower.MaxHP);
                    kvp.Value.UpdateActivation(tower.IsActivated);
                }
            }
        }

        private void OnTowerDamaged(EventBus.TowerDamagedEvent evt)
        {
            // Find tower view
            foreach (var kvp in _towerViews)
            {
                var tower = kvp.Value.Tower;
                if (tower.OwnerPlayerId == evt.playerId && tower.Type == (TowerType)evt.towerType)
                {
                    kvp.Value.PlayHitEffect();
                    TriggerCameraShake(0.1f);
                    break;
                }
            }
        }

        private void OnTowerDestroyed(EventBus.TowerDestroyedEvent evt)
        {
            foreach (var kvp in _towerViews)
            {
                var tower = kvp.Value.Tower;
                if (tower.OwnerPlayerId == evt.playerId && tower.Type == (TowerType)evt.towerType)
                {
                    kvp.Value.PlayDestructionAnimation();
                    TriggerCameraShake(0.5f);
                    break;
                }
            }
        }

        private void OnKingTowerActivated(EventBus.KingTowerActivatedEvent evt)
        {
            // Visual: King Tower activation effect
            foreach (var kvp in _towerViews)
            {
                var tower = kvp.Value.Tower;
                if (tower.OwnerPlayerId == evt.playerId && tower.Type == TowerType.King)
                {
                    kvp.Value.PlayActivationEffect();
                    break;
                }
            }
        }

        private void OnBattleEnded(EventBus.BattleEndedEvent evt)
        {
            // Show result UI
            Services.Get<UIManager>().ShowBattleResult(evt);
        }

        private void TriggerCameraShake(float intensity)
        {
            _shakeTimer = intensity * 0.5f;
        }

        private void UpdateCameraShake()
        {
            if (_shakeTimer > 0)
            {
                _shakeTimer -= Time.deltaTime;
                float intensity = Mathf.Lerp(0, _cameraShakeIntensity, _shakeTimer * 2f);
                _battleCamera.transform.position = _cameraStartPos + (Vector3)UnityEngine.Random.insideUnitCircle * intensity;
            }
            else
            {
                _battleCamera.transform.position = Vector3.Lerp(_battleCamera.transform.position, _cameraStartPos, Time.deltaTime * 5f);
            }
        }

        private void OnDestroy()
        {
            EventBus.OnUnitSpawned -= OnUnitSpawned;
            EventBus.OnUnitDied -= OnUnitDied;
            EventBus.OnBuildingPlaced -= OnBuildingPlaced;
            EventBus.OnBuildingDestroyed -= OnBuildingDestroyed;
            EventBus.OnSpellCast -= OnSpellCast;
            EventBus.OnTowerDamaged -= OnTowerDamaged;
            EventBus.OnTowerDestroyed -= OnTowerDestroyed;
            EventBus.OnKingTowerActivated -= OnKingTowerActivated;
            EventBus.OnBattleEnded -= OnBattleEnded;
        }
    }
}