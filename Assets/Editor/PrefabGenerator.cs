using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using CRClone.Data;
using CRClone.Battle.Presentation;
using CRClone.Battle.Simulation;
using CRClone.Core;

namespace CRClone.Editor
{
    public class PrefabGenerator : EditorWindow
    {
        private CardData _cardData;
        private GameObject _unitTemplate;
        private GameObject _buildingTemplate;
        private GameObject _spellTemplate;
        private GameObject _projectileTemplate;
        private GameObject _towerTemplate;
        private string _outputFolder = "Assets/Prefabs";
        private bool _overwriteExisting = true;

        [MenuItem("CRClone/Asset Pipeline/Prefab Generator")]
        public static void ShowWindow()
        {
            GetWindow<PrefabGenerator>("Prefab Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Prefab Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _cardData = (CardData)EditorGUILayout.ObjectField("Card Data", _cardData, typeof(CardData), false);

            EditorGUILayout.Space();
            GUILayout.Label("Templates (optional - will create defaults if empty)", EditorStyles.boldLabel);
            _unitTemplate = (GameObject)EditorGUILayout.ObjectField("Unit Template", _unitTemplate, typeof(GameObject), false);
            _buildingTemplate = (GameObject)EditorGUILayout.ObjectField("Building Template", _buildingTemplate, typeof(GameObject), false);
            _spellTemplate = (GameObject)EditorGUILayout.ObjectField("Spell Template", _spellTemplate, typeof(GameObject), false);
            _projectileTemplate = (GameObject)EditorGUILayout.ObjectField("Projectile Template", _projectileTemplate, typeof(GameObject), false);
            _towerTemplate = (GameObject)EditorGUILayout.ObjectField("Tower Template", _towerTemplate, typeof(GameObject), false);

            EditorGUILayout.Space();
            _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);
            _overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", _overwriteExisting);

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate Prefab", GUILayout.Height(40)))
            {
                GeneratePrefab();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate All Prefabs from Database", GUILayout.Height(30)))
            {
                GenerateAllPrefabs();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Create Default Templates", GUILayout.Height(30)))
            {
                CreateDefaultTemplates();
            }
        }

        private void GeneratePrefab()
        {
            if (_cardData == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a CardData asset", "OK");
                return;
            }

            GameObject prefab = null;
            string subfolder = "";

            switch (_cardData.type)
            {
                case CardType.Troop:
                case CardType.Champion:
                    prefab = GenerateUnitPrefab(_cardData);
                    subfolder = "Units";
                    break;
                case CardType.Building:
                    prefab = GenerateBuildingPrefab(_cardData);
                    subfolder = "Buildings";
                    break;
                case CardType.Spell:
                    prefab = GenerateSpellPrefab(_cardData);
                    subfolder = "Spells";
                    break;
            }

            if (prefab != null)
            {
                var outputPath = Path.Combine(_outputFolder, subfolder, $"{prefab.name}.prefab").Replace("\\", "/");
                var dir = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                if (File.Exists(outputPath) && !_overwriteExisting)
                {
                    Debug.LogWarning($"[PrefabGenerator] Prefab exists: {outputPath}");
                    return;
                }

                var savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefab, outputPath);
                Debug.Log($"[PrefabGenerator] Generated: {outputPath}");
                DestroyImmediate(prefab);
                EditorUtility.DisplayDialog("Success", $"Prefab generated at: {outputPath}", "OK");
            }
        }

        private void GenerateAllPrefabs()
        {
            var cards = Resources.LoadAll<CardData>("Data/Cards");
            int count = 0;

            foreach (var card in cards)
            {
                if (!card.isEnabled) continue;

                GameObject prefab = null;
                string subfolder = "";

                switch (card.type)
                {
                    case CardType.Troop:
                    case CardType.Champion:
                        prefab = GenerateUnitPrefab(card);
                        subfolder = "Units";
                        break;
                    case CardType.Building:
                        prefab = GenerateBuildingPrefab(card);
                        subfolder = "Buildings";
                        break;
                    case CardType.Spell:
                        prefab = GenerateSpellPrefab(card);
                        subfolder = "Spells";
                        break;
                }

                if (prefab != null)
                {
                    var outputPath = Path.Combine(_outputFolder, subfolder, $"{prefab.name}.prefab").Replace("\\", "/");
                    var dir = Path.GetDirectoryName(outputPath);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    PrefabUtility.SaveAsPrefabAsset(prefab, outputPath);
                    DestroyImmediate(prefab);
                    count++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Complete", $"Generated {count} prefabs", "OK");
        }

        private GameObject GenerateUnitPrefab(CardData card)
        {
            var prefab = new GameObject($"Unit_{card.cardName.Replace(" ", "")}");
            prefab.tag = "Unit";
            prefab.layer = LayerMask.NameToLayer("Unit");

            // Add UnitView component
            var unitView = prefab.AddComponent<UnitView>();
            unitView.entity = prefab.AddComponent<Unit>();
            unitView.cardData = card;

            // Add Spine animation (placeholder - will be replaced by SpineExporter)
            var animator = prefab.AddComponent<Animator>();
            var controller = CreateUnitAnimatorController(card);
            animator.runtimeAnimatorController = controller;

            // Add SpriteRenderer for fallback
            var spriteRenderer = prefab.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 10;

            // Add collider
            var collider = prefab.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
            collider.isTrigger = true;

            // Add rigidbody for physics
            var rb = prefab.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;

            // Add HealthBar
            var healthBar = CreateHealthBar(prefab.transform);

            // Add SelectionRing
            var selectionRing = CreateSelectionRing(prefab.transform);

            // Add particle points
            CreateParticlePoints(prefab.transform);

            // Apply card stats to Unit
            var unit = prefab.GetComponent<Unit>();
            unit.cardData = card;
            unit.maxHP = card.baseHitpoints;
            unit.currentHP = card.baseHitpoints;
            unit.damage = card.baseDamage;
            unit.hitSpeed = card.baseHitSpeed;
            unit.range = card.baseRange;
            unit.moveSpeed = GetSpeedValue(card.speed);
            unit.targetType = card.targetType;
            unit.deployTime = card.deployTime;
            unit.entityType = EntityType.Unit;

            // Add IPoolable
            var poolable = prefab.AddComponent<UnitPoolable>();

            return prefab;
        }

        private GameObject GenerateBuildingPrefab(CardData card)
        {
            var prefab = new GameObject($"Building_{card.cardName.Replace(" ", "")}");
            prefab.tag = "Building";
            prefab.layer = LayerMask.NameToLayer("Building");

            var buildingView = prefab.AddComponent<BuildingView>();
            buildingView.entity = prefab.AddComponent<Building>();
            buildingView.cardData = card;

            var animator = prefab.AddComponent<Animator>();
            var controller = CreateBuildingAnimatorController(card);
            animator.runtimeAnimatorController = controller;

            var spriteRenderer = prefab.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 5;

            var collider = prefab.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(2f, 2f);
            collider.isTrigger = true;

            var rb = prefab.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            var healthBar = CreateHealthBar(prefab.transform);

            // Special handling for Tesla (retraction)
            if (card.cardName.Contains("Tesla", StringComparison.OrdinalIgnoreCase))
            {
                var teslaRetract = prefab.AddComponent<TeslaRetraction>();
                teslaRetract.retractedPosition = Vector3.down * 2f;
            }

            // Spawn points for spawners
            if (card.mechanicsJson.Contains("spawn") || card.cardName.Contains("Hut") || card.cardName.Contains("Furnace") || card.cardName.Contains("Tombstone") || card.cardName.Contains("Cage") || card.cardName.Contains("Drill"))
            {
                var spawnPoint = new GameObject("SpawnPoint").transform;
                spawnPoint.SetParent(prefab.transform);
                spawnPoint.localPosition = Vector3.up * 1f;
                buildingView.spawnPoint = spawnPoint;
            }

            var building = prefab.GetComponent<Building>();
            building.cardData = card;
            building.maxHP = card.baseHitpoints;
            building.currentHP = card.baseHitpoints;
            building.damage = card.baseDamage;
            building.hitSpeed = card.baseHitSpeed;
            building.range = card.baseRange;
            building.targetType = card.targetType;
            building.lifetime = GetBuildingLifetime(card);
            building.entityType = EntityType.Building;

            var poolable = prefab.AddComponent<BuildingPoolable>();

            return prefab;
        }

        private GameObject GenerateSpellPrefab(CardData card)
        {
            var prefab = new GameObject($"Spell_{card.cardName.Replace(" ", "")}");
            prefab.tag = "Spell";
            prefab.layer = LayerMask.NameToLayer("Spell");

            var spellView = prefab.AddComponent<SpellEffectView>();
            spellView.entity = prefab.AddComponent<SpellEffect>();
            spellView.cardData = card;

            // Add ParticleSystem for spell effects
            var ps = prefab.AddComponent<ParticleSystem>();
            ConfigureSpellParticles(ps, card);

            var spell = prefab.GetComponent<SpellEffect>();
            spell.cardData = card;
            spell.damage = card.baseDamage;
            spell.radius = GetSpellRadius(card);
            spell.duration = GetSpellDuration(card);
            spell.entityType = EntityType.SpellEffect;

            var poolable = prefab.AddComponent<SpellPoolable>();

            return prefab;
        }

        private GameObject GenerateProjectilePrefab(CardData card)
        {
            var prefab = new GameObject($"Projectile_{card.cardName.Replace(" ", "")}");
            prefab.tag = "Projectile";
            prefab.layer = LayerMask.NameToLayer("Projectile");

            var projectileView = prefab.AddComponent<ProjectileView>();
            projectileView.entity = prefab.AddComponent<Projectile>();
            projectileView.cardData = card;

            var spriteRenderer = prefab.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 15;

            var trailRenderer = prefab.AddComponent<TrailRenderer>();
            trailRenderer.time = 0.3f;
            trailRenderer.startWidth = 0.2f;
            trailRenderer.endWidth = 0f;
            trailRenderer.material = new Material(Shader.Find("Sprites/Default"));

            var collider = prefab.AddComponent<CircleCollider2D>();
            collider.radius = 0.2f;
            collider.isTrigger = true;

            var rb = prefab.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            var projectile = prefab.GetComponent<Projectile>();
            projectile.cardData = card;
            projectile.damage = card.baseDamage;
            projectile.speed = 20f;
            projectile.entityType = EntityType.Projectile;

            var poolable = prefab.AddComponent<ProjectilePoolable>();

            return prefab;
        }

        private GameObject GenerateTowerPrefab(CardData card)
        {
            var prefab = new GameObject($"Tower_{card.cardName.Replace(" ", "")}");
            prefab.tag = "Tower";
            prefab.layer = LayerMask.NameToLayer("Tower");

            var towerView = prefab.AddComponent<TowerView>();
            towerView.entity = prefab.AddComponent<Tower>();
            towerView.cardData = card;

            var animator = prefab.AddComponent<Animator>();
            var controller = CreateTowerAnimatorController(card);
            animator.runtimeAnimatorController = controller;

            var spriteRenderer = prefab.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 5;

            var collider = prefab.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(3f, 4f);
            collider.isTrigger = true;

            var rb = prefab.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            var healthBar = CreateHealthBar(prefab.transform);

            var tower = prefab.GetComponent<Tower>();
            tower.cardData = card;
            tower.maxHP = card.baseHitpoints;
            tower.currentHP = card.baseHitpoints;
            tower.damage = card.baseDamage;
            tower.hitSpeed = card.baseHitSpeed;
            tower.range = card.baseRange;
            tower.targetType = card.targetType;
            tower.entityType = EntityType.Tower;

            return prefab;
        }

        private void CreateDefaultTemplates()
        {
            // Create default template prefabs
            CreateUnitTemplate();
            CreateBuildingTemplate();
            CreateSpellTemplate();
            CreateProjectileTemplate();
            CreateTowerTemplate();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Complete", "Default templates created in Assets/Prefabs/Templates/", "OK");
        }

        private void CreateUnitTemplate()
        {
            var template = new GameObject("UnitTemplate");
            template.tag = "Unit";
            template.layer = LayerMask.NameToLayer("Unit");

            var unitView = template.AddComponent<UnitView>();
            var animator = template.AddComponent<Animator>();
            var spriteRenderer = template.AddComponent<SpriteRenderer>();
            var collider = template.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
            collider.isTrigger = true;
            var rb = template.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            CreateHealthBar(template.transform);
            CreateSelectionRing(template.transform);
            CreateParticlePoints(template.transform);

            SaveTemplate(template, "Templates/UnitTemplate.prefab");
        }

        private void CreateBuildingTemplate()
        {
            var template = new GameObject("BuildingTemplate");
            template.tag = "Building";
            template.layer = LayerMask.NameToLayer("Building");

            var buildingView = template.AddComponent<BuildingView>();
            var animator = template.AddComponent<Animator>();
            var spriteRenderer = template.AddComponent<SpriteRenderer>();
            var collider = template.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(2f, 2f);
            collider.isTrigger = true;
            var rb = template.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            CreateHealthBar(template.transform);

            SaveTemplate(template, "Templates/BuildingTemplate.prefab");
        }

        private void CreateSpellTemplate()
        {
            var template = new GameObject("SpellTemplate");
            template.tag = "Spell";
            template.layer = LayerMask.NameToLayer("Spell");

            var spellView = template.AddComponent<SpellEffectView>();
            var ps = template.AddComponent<ParticleSystem>();

            SaveTemplate(template, "Templates/SpellTemplate.prefab");
        }

        private void CreateProjectileTemplate()
        {
            var template = new GameObject("ProjectileTemplate");
            template.tag = "Projectile";
            template.layer = LayerMask.NameToLayer("Projectile");

            var projectileView = template.AddComponent<ProjectileView>();
            var spriteRenderer = template.AddComponent<SpriteRenderer>();
            var trailRenderer = template.AddComponent<TrailRenderer>();
            var collider = template.AddComponent<CircleCollider2D>();
            collider.radius = 0.2f;
            collider.isTrigger = true;
            var rb = template.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            SaveTemplate(template, "Templates/ProjectileTemplate.prefab");
        }

        private void CreateTowerTemplate()
        {
            var template = new GameObject("TowerTemplate");
            template.tag = "Tower";
            template.layer = LayerMask.NameToLayer("Tower");

            var towerView = template.AddComponent<TowerView>();
            var animator = template.AddComponent<Animator>();
            var spriteRenderer = template.AddComponent<SpriteRenderer>();
            var collider = template.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(3f, 4f);
            collider.isTrigger = true;
            var rb = template.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            CreateHealthBar(template.transform);

            SaveTemplate(template, "Templates/TowerTemplate.prefab");
        }

        private void SaveTemplate(GameObject template, string path)
        {
            var fullPath = $"Assets/Prefabs/{path}";
            var dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            PrefabUtility.SaveAsPrefabAsset(template, fullPath);
            DestroyImmediate(template);
        }

        private RuntimeAnimatorController CreateUnitAnimatorController(CardData card)
        {
            var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath($"Assets/Animations/Units/{card.cardName}_Controller.controller");
            
            // Add parameters
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Spawn", AnimatorControllerParameterType.Trigger);

            // Create state machine
            var rootStateMachine = controller.layers[0].stateMachine;
            
            var idleState = rootStateMachine.AddState("Idle");
            var walkState = rootStateMachine.AddState("Walk");
            var attackState = rootStateMachine.AddState("Attack");
            var hitState = rootStateMachine.AddState("Hit");
            var deathState = rootStateMachine.AddState("Death");
            var spawnState = rootStateMachine.AddState("Spawn");

            // Set default state
            rootStateMachine.defaultState = idleState;

            // Transitions
            AddTransition(idleState, walkState, "Speed", 0.1f, true);
            AddTransition(walkState, idleState, "Speed", 0.1f, false);
            AddTransition(idleState, attackState, "Attack");
            AddTransition(attackState, idleState, exitTime: 0.9f);
            AddTransition(anyState: rootStateMachine, hitState, "Hit");
            AddTransition(hitState, idleState, exitTime: 0.9f);
            AddTransition(anyState: rootStateMachine, deathState, "Death");
            AddTransition(spawnState, idleState, exitTime: 0.9f);

            return controller;
        }

        private RuntimeAnimatorController CreateBuildingAnimatorController(CardData card)
        {
            var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath($"Assets/Animations/Buildings/{card.cardName}_Controller.controller");
            
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Damaged", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Destroyed", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Spawn", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;
            
            var idleState = rootStateMachine.AddState("Idle");
            var attackState = rootStateMachine.AddState("Attack");
            var damagedState = rootStateMachine.AddState("Damaged");
            var destroyedState = rootStateMachine.AddState("Destroyed");
            var spawnState = rootStateMachine.AddState("Spawn");

            rootStateMachine.defaultState = idleState;

            AddTransition(idleState, attackState, "Attack");
            AddTransition(attackState, idleState, exitTime: 0.9f);
            AddTransition(anyState: rootStateMachine, damagedState, "Damaged");
            AddTransition(damagedState, idleState, exitTime: 0.9f);
            AddTransition(anyState: rootStateMachine, destroyedState, "Destroyed");
            AddTransition(spawnState, idleState, exitTime: 0.9f);

            return controller;
        }

        private RuntimeAnimatorController CreateSpellAnimatorController(CardData card)
        {
            var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath($"Assets/Animations/Spells/{card.cardName}_Controller.controller");
            
            controller.AddParameter("Cast", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Impact", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;
            
            var idleState = rootStateMachine.AddState("Idle");
            var castState = rootStateMachine.AddState("Cast");
            var impactState = rootStateMachine.AddState("Impact");

            rootStateMachine.defaultState = idleState;

            AddTransition(idleState, castState, "Cast");
            AddTransition(castState, impactState, exitTime: 0.9f);
            AddTransition(impactState, idleState, exitTime: 0.9f);

            return controller;
        }

        private RuntimeAnimatorController CreateTowerAnimatorController(CardData card)
        {
            var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath($"Assets/Animations/Towers/{card.cardName}_Controller.controller");
            
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Activate", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Destroyed", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;
            
            var idleState = rootStateMachine.AddState("Idle");
            var attackState = rootStateMachine.AddState("Attack");
            var activateState = rootStateMachine.AddState("Activate");
            var destroyedState = rootStateMachine.AddState("Destroyed");

            rootStateMachine.defaultState = idleState;

            AddTransition(idleState, attackState, "Attack");
            AddTransition(attackState, idleState, exitTime: 0.9f);
            AddTransition(anyState: rootStateMachine, activateState, "Activate");
            AddTransition(activateState, idleState, exitTime: 0.9f);
            AddTransition(anyState: rootStateMachine, destroyedState, "Destroyed");

            return controller;
        }

        private void AddTransition(AnimatorState from, AnimatorState to, string param, float threshold = 0, bool greater = true, float exitTime = 0)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = exitTime > 0;
            transition.exitTime = exitTime;
            transition.duration = 0.1f;
            
            if (!string.IsNullOrEmpty(param))
            {
                transition.AddCondition(AnimatorConditionMode.If, threshold, param);
            }
        }

        private void AddTransition(AnimatorStateMachine anyState, AnimatorState to, string trigger)
        {
            var transition = anyState.AddAnyStateTransition(to);
            transition.AddCondition(AnimatorConditionMode.If, 0, trigger);
            transition.duration = 0.05f;
        }

        private HealthBar CreateHealthBar(Transform parent)
        {
            var hbObj = new GameObject("HealthBar");
            hbObj.transform.SetParent(parent);
            hbObj.transform.localPosition = new Vector3(0, 2.5f, 0);
            hbObj.transform.localScale = Vector3.one * 0.5f;
            return hbObj.AddComponent<HealthBar>();
        }

        private SelectionRing CreateSelectionRing(Transform parent)
        {
            var ringObj = new GameObject("SelectionRing");
            ringObj.transform.SetParent(parent);
            ringObj.transform.localPosition = Vector3.zero;
            
            var ring = ringObj.AddComponent<SelectionRing>();
            var lineRenderer = ringObj.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 32;
            lineRenderer.widthMultiplier = 0.05f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.material.color = Color.yellow;
            lineRenderer.loop = true;
            
            // Create circle
            var positions = new Vector3[32];
            for (int i = 0; i < 32; i++)
            {
                float angle = i * Mathf.PI * 2f / 32;
                positions[i] = new Vector3(Mathf.Cos(angle) * 0.6f, Mathf.Sin(angle) * 0.6f, 0);
            }
            lineRenderer.SetPositions(positions);
            
            ringObj.SetActive(false);
            return ring;
        }

        private void CreateParticlePoints(Transform parent)
        {
            var points = new GameObject("ParticlePoints");
            points.transform.SetParent(parent);
            
            var spawnPoint = new GameObject("SpawnPoint").transform;
            spawnPoint.SetParent(points.transform);
            spawnPoint.localPosition = Vector3.zero;
            
            var attackPoint = new GameObject("AttackPoint").transform;
            attackPoint.SetParent(points.transform);
            attackPoint.localPosition = Vector3.right * 0.8f;
            
            var hitPoint = new GameObject("HitPoint").transform;
            hitPoint.SetParent(points.transform);
            hitPoint.localPosition = Vector3.zero;
            
            var deathPoint = new GameObject("DeathPoint").transform;
            deathPoint.SetParent(points.transform);
            deathPoint.localPosition = Vector3.zero;
        }

        private void ConfigureSpellParticles(ParticleSystem ps, CardData card)
        {
            var main = ps.main;
            main.duration = 1f;
            main.loop = false;
            main.startLifetime = 1f;
            main.startSpeed = 5f;
            main.startSize = 0.5f;
            main.startColor = GetSpellColor(card);
            main.gravityModifier = 0f;

            var emission = ps.emission;
            emission.rateOverTime = 50;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            // Add specific modules based on spell type
            if (card.cardName.Contains("Fire", StringComparison.OrdinalIgnoreCase))
            {
                var colorOverLifetime = ps.colorOverLifetime;
                colorOverLifetime.enabled = true;
                var gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.red, 0), new GradientColorKey(Color.yellow, 0.5f), new GradientColorKey(Color.white, 1) },
                    new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) }
                );
                colorOverLifetime.color = gradient;
            }
            else if (card.cardName.Contains("Ice", StringComparison.OrdinalIgnoreCase) || card.cardName.Contains("Freeze", StringComparison.OrdinalIgnoreCase))
            {
                var colorOverLifetime = ps.colorOverLifetime;
                colorOverLifetime.enabled = true;
                var gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.cyan, 0), new GradientColorKey(Color.blue, 1) },
                    new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) }
                );
                colorOverLifetime.color = gradient;
            }
            else if (card.cardName.Contains("Lightning", StringComparison.OrdinalIgnoreCase) || card.cardName.Contains("Zap", StringComparison.OrdinalIgnoreCase))
            {
                var colorOverLifetime = ps.colorOverLifetime;
                colorOverLifetime.enabled = true;
                var gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.yellow, 0), new GradientColorKey(Color.white, 1) },
                    new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) }
                );
                colorOverLifetime.color = gradient;
            }
            else if (card.cardName.Contains("Poison", StringComparison.OrdinalIgnoreCase))
            {
                var colorOverLifetime = ps.colorOverLifetime;
                colorOverLifetime.enabled = true;
                var gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.green, 0), new GradientColorKey(new Color(0.5f, 0.8f, 0.2f), 1) },
                    new GradientAlphaKey[] { new GradientAlphaKey(0.5f, 0), new GradientAlphaKey(0, 1) }
                );
                colorOverLifetime.color = gradient;
            }
        }

        private Color GetSpellColor(CardData card)
        {
            if (card.cardName.Contains("Fire")) return Color.red;
            if (card.cardName.Contains("Ice") || card.cardName.Contains("Freeze")) return Color.cyan;
            if (card.cardName.Contains("Lightning") || card.cardName.Contains("Zap")) return Color.yellow;
            if (card.cardName.Contains("Poison")) return Color.green;
            if (card.cardName.Contains("Tornado")) return Color.gray;
            if (card.cardName.Contains("Log")) return new Color(0.6f, 0.4f, 0.2f);
            return Color.white;
        }

        private float GetSpeedValue(SpeedType speed)
        {
            return speed switch
            {
                SpeedType.VerySlow => 20f,
                SpeedType.Slow => 35f,
                SpeedType.Medium => 50f,
                SpeedType.Fast => 70f,
                SpeedType.VeryFast => 100f,
                _ => 50f
            };
        }

        private float GetBuildingLifetime(CardData card)
        {
            // Default lifetimes based on building type
            if (card.cardName.Contains("Tesla")) return 25f;
            if (card.cardName.Contains("Inferno Tower")) return 25f;
            if (card.cardName.Contains("Tombstone")) return 20f;
            if (card.cardName.Contains("Furnace")) return 40f;
            if (card.cardName.Contains("Elixir Collector")) return 70f;
            if (card.cardName.Contains("Goblin Drill")) return 30f;
            if (card.cardName.Contains("Cannon Cart")) return 30f;
            return 30f;
        }

        private float GetSpellRadius(CardData card)
        {
            // Parse from mechanics or use defaults
            if (card.mechanicsJson.Contains("radius"))
            {
                // Would parse from JSON
            }
            return 2.5f;
        }

        private float GetSpellDuration(CardData card)
        {
            if (card.cardName.Contains("Freeze")) return 4f;
            if (card.cardName.Contains("Poison")) return 8f;
            if (card.cardName.Contains("Rage")) return 6f;
            if (card.cardName.Contains("Tornado")) return 1.5f;
            if (card.cardName.Contains("Graveyard")) return 3f;
            return 1f;
        }
    }

    // Poolable components for different entity types
    public class UnitPoolable : MonoBehaviour, CRClone.Systems.IPoolable
    {
        public void OnSpawn() { }
        public void OnDespawn() { }
    }

    public class BuildingPoolable : MonoBehaviour, CRClone.Systems.IPoolable
    {
        public void OnSpawn() { }
        public void OnDespawn() { }
    }

    public class SpellPoolable : MonoBehaviour, CRClone.Systems.IPoolable
    {
        public void OnSpawn() { }
        public void OnDespawn() { }
    }

    public class ProjectilePoolable : MonoBehaviour, CRClone.Systems.IPoolable
    {
        public void OnSpawn() { }
        public void OnDespawn() { }
    }

    // Missing components
    public class TeslaRetraction : MonoBehaviour
    {
        public Vector3 retractedPosition;
        public float retractionSpeed = 5f;
        private bool _isRetracted = false;
        private Vector3 _originalPosition;
        
        private void Awake() { _originalPosition = transform.localPosition; }
        
        public void SetRetracted(bool retracted)
        {
            _isRetracted = retracted;
        }
        
        private void Update()
        {
            var target = _isRetracted ? retractedPosition : _originalPosition;
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, retractionSpeed * Time.deltaTime);
        }
    }

    public class SelectionRing : MonoBehaviour { }
}