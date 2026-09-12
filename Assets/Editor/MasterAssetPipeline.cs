using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CRClone.Data;
using CRClone.Battle.Presentation;
using CRClone.Battle.Simulation;
using CRClone.Core;

namespace CRClone.Editor
{
    public static class MasterAssetPipeline
    {
        [MenuItem("CRClone/Asset Pipeline/Run Full Pipeline")]
        public static void RunFullPipeline()
        {
            Debug.Log("[MasterAssetPipeline] Starting full asset pipeline...");

            // Step 1: Create GameConfig
            CreateGameConfig();

            // Step 2: Generate CardData assets
            GenerateAllCards();

            // Step 3: Create default templates
            CreateDefaultTemplates();

            // Step 4: Generate Prefabs for all cards
            GenerateAllPrefabs();

            // Step 5: Generate Asset Manifest
            GenerateAssetManifest();

            Debug.Log("[MasterAssetPipeline] Full pipeline complete!");
            EditorUtility.DisplayDialog("Pipeline Complete", "All assets generated successfully!", "OK");
        }

        private static void CreateGameConfig()
        {
            var config = ScriptableObject.CreateInstance<GameConfig>();
            var path = "Assets/Resources/Configs/GameConfig.asset";
            
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (AssetDatabase.LoadAssetAtPath<GameConfig>(path) == null)
            {
                AssetDatabase.CreateAsset(config, path);
                Debug.Log($"[MasterAssetPipeline] Created GameConfig: {path}");
            }
        }

        private static void GenerateAllCards()
        {
            var databasePath = "docs/planning/phase1/CARDS_DATABASE.md";
            var outputPath = "Assets/Resources/Data/Cards";

            if (!File.Exists(databasePath))
            {
                Debug.LogError($"[MasterAssetPipeline] Database not found: {databasePath}");
                return;
            }

            var content = File.ReadAllText(databasePath);
            var cards = ParseCardsDatabase(content);

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            int created = 0, updated = 0;
            foreach (var def in cards)
            {
                var assetPath = $"{outputPath}/Card_{def.cardId:D3}_{SanitizeFileName(def.cardName)}.asset";
                
                CardData asset = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
                bool isNew = asset == null;
                
                if (isNew)
                {
                    asset = ScriptableObject.CreateInstance<CardData>();
                }

                ApplyCardData(asset, def);

                if (isNew)
                {
                    AssetDatabase.CreateAsset(asset, assetPath);
                    created++;
                }
                else
                {
                    EditorUtility.SetDirty(asset);
                    updated++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MasterAssetPipeline] Cards: {created} created, {updated} updated");
        }

        private static void CreateDefaultTemplates()
        {
            CreateUnitTemplate();
            CreateBuildingTemplate();
            CreateSpellTemplate();
            CreateProjectileTemplate();
            CreateTowerTemplate();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateUnitTemplate()
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

            var hbObj = new GameObject("HealthBar");
            hbObj.transform.SetParent(template.transform);
            hbObj.transform.localPosition = new Vector3(0, 2.5f, 0);
            hbObj.transform.localScale = Vector3.one * 0.5f;
            var healthBar = hbObj.AddComponent<HealthBar>();

            var ringObj = new GameObject("SelectionRing");
            ringObj.transform.SetParent(template.transform);
            ringObj.transform.localPosition = Vector3.zero;
            var selectionRing = ringObj.AddComponent<SelectionRing>();
            var lineRenderer = ringObj.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 32;
            lineRenderer.widthMultiplier = 0.05f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.material.color = Color.yellow;
            lineRenderer.loop = true;
            var positions = new Vector3[32];
            for (int i = 0; i < 32; i++)
            {
                float angle = i * Mathf.PI * 2f / 32;
                positions[i] = new Vector3(Mathf.Cos(angle) * 0.6f, Mathf.Sin(angle) * 0.6f, 0);
            }
            lineRenderer.SetPositions(positions);
            ringObj.SetActive(false);

            var points = new GameObject("ParticlePoints");
            points.transform.SetParent(template.transform);
            CreateParticlePoint(points.transform, "SpawnPoint", Vector3.zero);
            CreateParticlePoint(points.transform, "AttackPoint", Vector3.right * 0.8f);
            CreateParticlePoint(points.transform, "HitPoint", Vector3.zero);
            CreateParticlePoint(points.transform, "DeathPoint", Vector3.zero);

            SaveTemplate(template, "Assets/Prefabs/Templates/UnitTemplate.prefab");
        }

        private static void CreateBuildingTemplate()
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

            var hbObj = new GameObject("HealthBar");
            hbObj.transform.SetParent(template.transform);
            hbObj.transform.localPosition = new Vector3(0, 2.5f, 0);
            hbObj.transform.localScale = Vector3.one * 0.5f;
            var healthBar = hbObj.AddComponent<HealthBar>();

            var retractedObj = new GameObject("RetractedVisual");
            retractedObj.transform.SetParent(template.transform);
            retractedObj.transform.localPosition = new Vector3(0, -2f, 0);
            retractedObj.SetActive(false);

            SaveTemplate(template, "Assets/Prefabs/Templates/BuildingTemplate.prefab");
        }

        private static void CreateSpellTemplate()
        {
            var template = new GameObject("SpellTemplate");
            template.tag = "Spell";
            template.layer = LayerMask.NameToLayer("Spell");

            var spellView = template.AddComponent<SpellEffectView>();
            var ps = template.AddComponent<ParticleSystem>();
            
            var areaIndicator = new GameObject("AreaIndicator");
            areaIndicator.transform.SetParent(template.transform);
            var areaSprite = areaIndicator.AddComponent<SpriteRenderer>();
            areaSprite.sprite = CreateCircleSprite(1f, new Color(1, 1, 1, 0.2f));
            areaSprite.sortingOrder = 1;

            SaveTemplate(template, "Assets/Prefabs/Templates/SpellTemplate.prefab");
        }

        private static void CreateProjectileTemplate()
        {
            var template = new GameObject("ProjectileTemplate");
            template.tag = "Projectile";
            template.layer = LayerMask.NameToLayer("Projectile");

            var projectileView = template.AddComponent<ProjectileView>();
            var spriteRenderer = template.AddComponent<SpriteRenderer>();
            var trailRenderer = template.AddComponent<TrailRenderer>();
            trailRenderer.time = 0.3f;
            trailRenderer.startWidth = 0.2f;
            trailRenderer.endWidth = 0f;
            trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
            
            var collider = template.AddComponent<CircleCollider2D>();
            collider.radius = 0.2f;
            collider.isTrigger = true;
            
            var rb = template.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            var impactPS = new GameObject("ImpactParticles");
            impactPS.transform.SetParent(template.transform);
            impactPS.AddComponent<ParticleSystem>();

            SaveTemplate(template, "Assets/Prefabs/Templates/ProjectileTemplate.prefab");
        }

        private static void CreateTowerTemplate()
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

            var hbObj = new GameObject("HealthBar");
            hbObj.transform.SetParent(template.transform);
            hbObj.transform.localPosition = new Vector3(0, 3f, 0);
            hbObj.transform.localScale = Vector3.one * 0.5f;
            var healthBar = hbObj.AddComponent<HealthBar>();

            var activationObj = new GameObject("ActivationEffect");
            activationObj.transform.SetParent(template.transform);
            activationObj.SetActive(false);
            var activationPS = activationObj.AddComponent<ParticleSystem>();

            var hitPSObj = new GameObject("HitParticles");
            hitPSObj.transform.SetParent(template.transform);
            hitPSObj.AddComponent<ParticleSystem>();

            var destroyPSObj = new GameObject("DestroyParticles");
            destroyPSObj.transform.SetParent(template.transform);
            destroyPSObj.AddComponent<ParticleSystem>();

            SaveTemplate(template, "Assets/Prefabs/Templates/TowerTemplate.prefab");
        }

        private static void CreateParticlePoint(Transform parent, string name, Vector3 localPos)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.localPosition = localPos;
        }

        private static Sprite CreateCircleSprite(float radius, Color color)
        {
            int size = 64;
            var tex = new Texture2D(size, size);
            var center = size / 2;
            var r = center * radius;
            
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= r)
                    {
                        tex.SetPixel(x, y, color);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        }

        private static void SaveTemplate(GameObject template, string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            PrefabUtility.SaveAsPrefabAsset(template, path);
            DestroyImmediate(template);
        }

        private static void GenerateAllPrefabs()
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
                    var outputPath = $"Assets/Prefabs/{subfolder}/{prefab.name}.prefab";
                    var dir = Path.GetDirectoryName(outputPath);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    PrefabUtility.SaveAsPrefabAsset(prefab, outputPath);
                    DestroyImmediate(prefab);
                    count++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MasterAssetPipeline] Generated {count} prefabs");
        }

        private static GameObject GenerateUnitPrefab(CardData card)
        {
            var template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Templates/UnitTemplate.prefab");
            if (template == null) return null;

            var prefab = PrefabUtility.InstantiatePrefab(template) as GameObject;
            prefab.name = $"Unit_{SanitizeFileName(card.cardName)}";

            var unitView = prefab.GetComponent<UnitView>();
            var unit = prefab.GetComponent<Unit>();
            if (unit == null) unit = prefab.AddComponent<Unit>();

            unitView.cardData = card;
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

            var animator = prefab.GetComponent<Animator>();
            if (animator != null)
            {
                var controller = CreateUnitAnimatorController(card);
                animator.runtimeAnimatorController = controller;
            }

            var poolable = prefab.GetComponent<UnitPoolable>();
            if (poolable == null) poolable = prefab.AddComponent<UnitPoolable>();

            return prefab;
        }

        private static GameObject GenerateBuildingPrefab(CardData card)
        {
            var template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Templates/BuildingTemplate.prefab");
            if (template == null) return null;

            var prefab = PrefabUtility.InstantiatePrefab(template) as GameObject;
            prefab.name = $"Building_{SanitizeFileName(card.cardName)}";

            var buildingView = prefab.GetComponent<BuildingView>();
            var building = prefab.GetComponent<Building>();
            if (building == null) building = prefab.AddComponent<Building>();

            buildingView.cardData = card;
            building.cardData = card;
            building.maxHP = card.baseHitpoints;
            building.currentHP = card.baseHitpoints;
            building.damage = card.baseDamage;
            building.hitSpeed = card.baseHitSpeed;
            building.range = card.baseRange;
            building.targetType = card.targetType;
            building.lifetime = GetBuildingLifetime(card);
            building.entityType = EntityType.Building;

            if (card.cardName.Contains("Tesla", StringComparison.OrdinalIgnoreCase))
            {
                var tesla = prefab.AddComponent<TeslaRetraction>();
            }

            if (card.mechanicsJson.Contains("spawn") || IsSpawnerBuilding(card))
            {
                var spawnPoint = new GameObject("SpawnPoint").transform;
                spawnPoint.SetParent(prefab.transform);
                spawnPoint.localPosition = Vector3.up * 1f;
                buildingView.spawnPoint = spawnPoint;
            }

            var animator = prefab.GetComponent<Animator>();
            if (animator != null)
            {
                var controller = CreateBuildingAnimatorController(card);
                animator.runtimeAnimatorController = controller;
            }

            var poolable = prefab.GetComponent<BuildingPoolable>();
            if (poolable == null) poolable = prefab.AddComponent<BuildingPoolable>();

            return prefab;
        }

        private static GameObject GenerateSpellPrefab(CardData card)
        {
            var template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Templates/SpellTemplate.prefab");
            if (template == null) return null;

            var prefab = PrefabUtility.InstantiatePrefab(template) as GameObject;
            prefab.name = $"Spell_{SanitizeFileName(card.cardName)}";

            var spellView = prefab.GetComponent<SpellEffectView>();
            var spell = prefab.GetComponent<SpellEffect>();
            if (spell == null) spell = prefab.AddComponent<SpellEffect>();

            spellView.cardData = card;
            spell.cardData = card;
            spell.damage = card.baseDamage;
            spell.radius = GetSpellRadius(card);
            spell.duration = GetSpellDuration(card);
            spell.entityType = EntityType.SpellEffect;

            var ps = prefab.GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                ConfigureSpellParticles(ps, card);
            }

            var poolable = prefab.GetComponent<SpellPoolable>();
            if (poolable == null) poolable = prefab.AddComponent<SpellPoolable>();

            return prefab;
        }

        private static void ConfigureSpellParticles(ParticleSystem ps, CardData card)
        {
            var main = ps.main;
            main.duration = GetSpellDuration(card);
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
        }

        private static Color GetSpellColor(CardData card)
        {
            if (card.cardName.Contains("Fire")) return Color.red;
            if (card.cardName.Contains("Ice") || card.cardName.Contains("Freeze")) return Color.cyan;
            if (card.cardName.Contains("Lightning") || card.cardName.Contains("Zap")) return Color.yellow;
            if (card.cardName.Contains("Poison")) return Color.green;
            if (card.cardName.Contains("Tornado")) return Color.gray;
            if (card.cardName.Contains("Log")) return new Color(0.6f, 0.4f, 0.2f);
            return Color.white;
        }

        private static float GetSpellRadius(CardData card)
        {
            if (card.cardName.Contains("Fireball")) return 2.5f;
            if (card.cardName.Contains("Rocket")) return 2f;
            if (card.cardName.Contains("Lightning")) return 3.5f;
            if (card.cardName.Contains("Poison")) return 3.5f;
            if (card.cardName.Contains("Freeze")) return 3f;
            if (card.cardName.Contains("Tornado")) return 5.5f;
            if (card.cardName.Contains("Graveyard")) return 4f;
            if (card.cardName.Contains("Log")) return 1.5f;
            if (card.cardName.Contains("Zap")) return 2.5f;
            if (card.cardName.Contains("Arrows")) return 4f;
            if (card.cardName.Contains("Rage")) return 3.5f;
            if (card.cardName.Contains("Clone")) return 3f;
            if (card.cardName.Contains("Mirror")) return 3f;
            if (card.cardName.Contains("Earthquake")) return 3.5f;
            if (card.cardName.Contains("Giant Snowball")) return 2.5f;
            if (card.cardName.Contains("Royal Delivery")) return 2.5f;
            if (card.cardName.Contains("Barbarian Barrel")) return 1.5f;
            if (card.cardName.Contains("Goblin Barrel")) return 1.5f;
            if (card.cardName.Contains("Skeleton Barrel")) return 2f;
            return 2.5f;
        }

        private static float GetSpellDuration(CardData card)
        {
            if (card.cardName.Contains("Freeze")) return 4f;
            if (card.cardName.Contains("Poison")) return 8f;
            if (card.cardName.Contains("Rage")) return 6f;
            if (card.cardName.Contains("Tornado")) return 1.5f;
            if (card.cardName.Contains("Graveyard")) return 3f;
            if (card.cardName.Contains("Clone")) return 3f;
            return 1f;
        }

        private static bool IsSpawnerBuilding(CardData card)
        {
            return card.cardName.Contains("Hut") || 
                   card.cardName.Contains("Furnace") || 
                   card.cardName.Contains("Tombstone") || 
                   card.cardName.Contains("Cage") || 
                   card.cardName.Contains("Drill") ||
                   card.cardName.Contains("Collector");
        }

        private static float GetBuildingLifetime(CardData card)
        {
            if (card.cardName.Contains("Tesla")) return 25f;
            if (card.cardName.Contains("Inferno Tower")) return 25f;
            if (card.cardName.Contains("Tombstone")) return 20f;
            if (card.cardName.Contains("Furnace")) return 40f;
            if (card.cardName.Contains("Elixir Collector")) return 70f;
            if (card.cardName.Contains("Goblin Drill")) return 30f;
            if (card.cardName.Contains("Cannon Cart")) return 30f;
            if (card.cardName.Contains("Goblin Hut")) return 30f;
            if (card.cardName.Contains("Goblin Cage")) return 30f;
            if (card.cardName.Contains("Bomb Tower")) return 30f;
            if (card.cardName.Contains("Cannon")) return 30f;
            if (card.cardName.Contains("Mortar")) return 30f;
            if (card.cardName.Contains("X-Bow")) return 30f;
            return 30f;
        }

        private static float GetSpeedValue(SpeedType speed)
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

        private static RuntimeAnimatorController CreateUnitAnimatorController(CardData card)
        {
            var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath($"Assets/Animations/Units/{SanitizeFileName(card.cardName)}_Controller.controller");
            
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Spawn", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;
            
            var idleState = rootStateMachine.AddState("Idle");
            var walkState = rootStateMachine.AddState("Walk");
            var attackState = rootStateMachine.AddState("Attack");
            var hitState = rootStateMachine.AddState("Hit");
            var deathState = rootStateMachine.AddState("Death");
            var spawnState = rootStateMachine.AddState("Spawn");

            rootStateMachine.defaultState = idleState;

            AddTransition(idleState, walkState, "Speed", 0.1f, true);
            AddTransition(walkState, idleState, "Speed", 0.1f, false);
            AddTransition(idleState, attackState, "Attack");
            AddTransition(attackState, idleState, exitTime: 0.9f);
            AddTransition(rootStateMachine, hitState, "Hit");
            AddTransition(hitState, idleState, exitTime: 0.9f);
            AddTransition(rootStateMachine, deathState, "Death");
            AddTransition(spawnState, idleState, exitTime: 0.9f);

            return controller;
        }

        private static RuntimeAnimatorController CreateBuildingAnimatorController(CardData card)
        {
            var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath($"Assets/Animations/Buildings/{SanitizeFileName(card.cardName)}_Controller.controller");
            
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Damaged", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Destroyed", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Retracted", AnimatorControllerParameterType.Bool);
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
            AddTransition(rootStateMachine, damagedState, "Damaged");
            AddTransition(damagedState, idleState, exitTime: 0.9f);
            AddTransition(rootStateMachine, destroyedState, "Destroyed");
            AddTransition(spawnState, idleState, exitTime: 0.9f);

            return controller;
        }

        private static void AddTransition(AnimatorState from, AnimatorState to, string param, float threshold = 0, bool greater = true, float exitTime = 0)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = exitTime > 0;
            transition.exitTime = exitTime;
            transition.duration = 0.1f;
            
            if (!string.IsNullOrEmpty(param))
            {
                transition.AddCondition(greater ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, threshold, param);
            }
        }

        private static void AddTransition(AnimatorStateMachine anyState, AnimatorState to, string trigger)
        {
            var transition = anyState.AddAnyStateTransition(to);
            transition.AddCondition(AnimatorConditionMode.If, 0, trigger);
            transition.duration = 0.05f;
        }

        private static void GenerateAssetManifest()
        {
            var manifest = new List<AssetManifestEntry>();

            var cards = Resources.LoadAll<CardData>("Data/Cards");
            foreach (var card in cards)
            {
                manifest.Add(new AssetManifestEntry
                {
                    id = card.cardId.ToString(),
                    name = card.cardName,
                    type = "CardData",
                    rarity = card.rarity.ToString(),
                    path = AssetDatabase.GetAssetPath(card),
                    dimensions = "N/A",
                    compression = "N/A",
                    memoryEstimateKB = 1,
                    dependencies = $"{card.spineAssetName},{card.spriteId},{card.portraitId}"
                });
            }

            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var renderers = prefab.GetComponentsInChildren<Renderer>();
                int triCount = 0;
                long texMemory = 0;

                foreach (var r in renderers)
                {
                    if (r is SkinnedMeshRenderer smr && smr.sharedMesh != null) 
                        triCount += smr.sharedMesh.triangles.Length / 3;
                    else if (r is MeshRenderer mr && mr.GetComponent<MeshFilter>()?.sharedMesh != null) 
                        triCount += mr.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3;

                    if (r.sharedMaterial?.mainTexture is Texture2D tex)
                        texMemory += GetTextureMemory(tex);
                }

                string category = "Unknown";
                if (path.Contains("/Units/")) category = "Unit";
                else if (path.Contains("/Buildings/")) category = "Building";
                else if (path.Contains("/Spells/")) category = "Spell";
                else if (path.Contains("/Projectiles/")) category = "Projectile";
                else if (path.Contains("/UI/")) category = "UI";

                manifest.Add(new AssetManifestEntry
                {
                    id = guid,
                    name = prefab.name,
                    type = "Prefab",
                    rarity = category,
                    path = path,
                    dimensions = $"{triCount} tris",
                    compression = "N/A",
                    memoryEstimateKB = texMemory / 1024 + triCount * 4,
                    dependencies = GetPrefabDependencies(prefab)
                });
            }

            var textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" });
            foreach (var guid in textureGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex == null) continue;

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                string compression = "Unknown";
                if (importer != null)
                {
                    var settings = new TextureImporterPlatformSettings();
                    importer.GetPlatformTextureSettings(settings);
                    compression = settings.format.ToString();
                }

                manifest.Add(new AssetManifestEntry
                {
                    id = guid,
                    name = tex.name,
                    type = "Texture",
                    rarity = GetFolderName(path, 2),
                    path = path,
                    dimensions = $"{tex.width}x{tex.height}",
                    compression = compression,
                    memoryEstimateKB = GetTextureMemory(tex) / 1024,
                    dependencies = "N/A"
                });
            }

            var outputPath = "Assets/AssetManifest.csv";
            using (var writer = new StreamWriter(outputPath))
            {
                writer.WriteLine("ID,Name,Type,Rarity,Path,Dimensions,Compression,MemoryEstimateKB,Dependencies");
                foreach (var entry in manifest.OrderBy(e => e.type).ThenBy(e => e.name))
                {
                    writer.WriteLine($"\"{entry.id}\",\"{entry.name}\",\"{entry.type}\",\"{entry.rarity}\",\"{entry.path}\",\"{entry.dimensions}\",\"{entry.compression}\",{entry.memoryEstimateKB},\"{entry.dependencies}\"");
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[MasterAssetPipeline] Manifest generated with {manifest.Count} entries");
        }

        private static string GetFolderName(string path, int levelsUp)
        {
            var dir = Path.GetDirectoryName(path);
            for (int i = 0; i < levelsUp && dir != null; i++)
                dir = Path.GetDirectoryName(dir);
            return dir != null ? Path.GetFileName(dir) : "Unknown";
        }

        private static string GetPrefabDependencies(GameObject prefab)
        {
            var deps = new List<string>();
            var renderers = prefab.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (r.sharedMaterial?.shader != null)
                    deps.Add(r.sharedMaterial.shader.name);
            }
            var scripts = prefab.GetComponentsInChildren<MonoBehaviour>();
            foreach (var s in scripts)
            {
                if (s != null) deps.Add(s.GetType().Name);
            }
            return string.Join(";", deps.Distinct());
        }

        private static long GetTextureMemory(Texture2D tex)
        {
            if (tex == null) return 0;
            int bytesPerPixel = 4;
            switch (tex.format)
            {
                case TextureFormat.ASTC_4x4: bytesPerPixel = 1; break;
                case TextureFormat.DXT1: bytesPerPixel = 1; break;
                case TextureFormat.DXT5: bytesPerPixel = 1; break;
                case TextureFormat.BC7: bytesPerPixel = 1; break;
                case TextureFormat.RGBA32: bytesPerPixel = 4; break;
                case TextureFormat.RGB24: bytesPerPixel = 3; break;
            }
            return (long)(tex.width * tex.height * bytesPerPixel * (tex.mipmapCount > 1 ? 1.33f : 1.0f));
        }

        private static List<CardDataDefinition> ParseCardsDatabase(string content)
        {
            var cards = new List<CardDataDefinition>();
            var lines = content.Split('\n');

            CardDataDefinition currentCard = null;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                var cardMatch = System.Text.RegularExpressions.Regex.Match(line, @"^###\s+(\d+)\.\s+(.+)$");
                if (cardMatch.Success)
                {
                    if (currentCard != null)
                        cards.Add(currentCard);

                    currentCard = new CardDataDefinition
                    {
                        cardId = int.Parse(cardMatch.Groups[1].Value),
                        cardName = cardMatch.Groups[2].Value.Trim(),
                        mechanicsJson = "{}"
                    };
                    continue;
                }

                if (currentCard == null) continue;

                if (line.StartsWith("- **Type**:"))
                    currentCard.type = ParseCardType(line.Substring(10).Trim());
                else if (line.StartsWith("- **Rarity**:"))
                    currentCard.rarity = ParseRarity(line.Substring(12).Trim());
                else if (line.StartsWith("- **Elixir**:"))
                    currentCard.elixirCost = int.Parse(System.Text.RegularExpressions.Regex.Match(line.Substring(11).Trim(), @"(\d+)").Groups[1].Value);
                else if (line.StartsWith("- **HP**:"))
                    currentCard.baseHitpoints = ParseNumber(line.Substring(6).Trim());
                else if (line.StartsWith("- **Damage**:"))
                    currentCard.baseDamage = ParseNumber(line.Substring(11).Trim());
                else if (line.StartsWith("- **Hit Speed**:"))
                    currentCard.baseHitSpeed = ParseFloat(line.Substring(13).Trim());
                else if (line.StartsWith("- **Range**:"))
                    currentCard.baseRange = ParseFloat(line.Substring(9).Trim());
                else if (line.StartsWith("- **Target**:"))
                    currentCard.targetType = ParseTargetType(line.Substring(10).Trim());
                else if (line.StartsWith("- **Speed**:"))
                    currentCard.speed = ParseSpeedType(line.Substring(10).Trim());
                else if (line.StartsWith("- **Deploy Time**:"))
                    currentCard.deployTime = int.Parse(System.Text.RegularExpressions.Regex.Match(line.Substring(16).Trim(), @"(\d+)").Groups[1].Value);
                else if (line.StartsWith("- **Count**:"))
                    currentCard.count = int.Parse(System.Text.RegularExpressions.Regex.Match(line.Substring(10).Trim(), @"(\d+)").Groups[1].Value);
                else if (line.StartsWith("- **Mechanic**:") || line.StartsWith("- **Mechanics**:"))
                    currentCard.mechanicsJson = ParseMechanics(line.Substring(line.IndexOf(':') + 1).Trim());
            }

            if (currentCard != null)
                cards.Add(currentCard);

            foreach (var card in cards)
                ApplyDefaults(card);

            return cards;
        }

        private static void ApplyCardData(CardData asset, CardDataDefinition def)
        {
            asset.cardId = def.cardId;
            asset.cardName = def.cardName;
            asset.nameKey = $"card_{SanitizeFileName(def.cardName).ToLower()}_name";
            asset.descriptionKey = $"card_{SanitizeFileName(def.cardName).ToLower()}_desc";
            asset.rarity = def.rarity;
            asset.type = def.type;
            asset.unlockArena = 1;
            asset.elixirCost = def.elixirCost;
            asset.baseHitpoints = def.baseHitpoints;
            asset.baseDamage = def.baseDamage;
            asset.baseHitSpeed = def.baseHitSpeed;
            asset.baseRange = def.baseRange;
            asset.speed = def.speed;
            asset.deployTime = def.deployTime;
            asset.targetType = def.targetType;
            asset.count = def.count;
            asset.mechanicsJson = def.mechanicsJson;
            asset.spriteId = def.spriteId;
            asset.portraitId = def.portraitId;
            asset.spineAssetName = def.spineAssetName;
            asset.deploySound = def.deploySound;
            asset.attackSound = def.attackSound;
            asset.hitSound = def.hitSound;
            asset.deathSound = def.deathSound;
            asset.isEnabled = true;
            asset.releaseVersion = "1.0";
        }

        private static CardType ParseCardType(string input)
        {
            input = input.ToLower();
            if (input.Contains("spell")) return CardType.Spell;
            if (input.Contains("building")) return CardType.Building;
            if (input.Contains("champion")) return CardType.Champion;
            return CardType.Troop;
        }

        private static CardRarity ParseRarity(string input)
        {
            input = input.ToLower();
            if (input.Contains("champion")) return CardRarity.Champion;
            if (input.Contains("legendary")) return CardRarity.Legendary;
            if (input.Contains("epic")) return CardRarity.Epic;
            if (input.Contains("rare")) return CardRarity.Rare;
            return CardRarity.Common;
        }

        private static TargetType ParseTargetType(string input)
        {
            input = input.ToLower();
            if (input.Contains("air & ground") || input.Contains("air and ground") || input.Contains("both")) return TargetType.Both;
            if (input.Contains("air")) return TargetType.Air;
            if (input.Contains("building")) return TargetType.Buildings;
            if (input.Contains("ground")) return TargetType.Ground;
            return TargetType.Any;
        }

        private static SpeedType ParseSpeedType(string input)
        {
            input = input.ToLower();
            if (input.Contains("very fast")) return SpeedType.VeryFast;
            if (input.Contains("fast")) return SpeedType.Fast;
            if (input.Contains("medium")) return SpeedType.Medium;
            if (input.Contains("slow")) return SpeedType.Slow;
            if (input.Contains("very slow")) return SpeedType.VerySlow;
            return SpeedType.Medium;
        }

        private static int ParseNumber(string input)
        {
            var match = System.Text.RegularExpressions.Regex.Match(input, @"(\d+)");
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }

        private static float ParseFloat(string input)
        {
            var match = System.Text.RegularExpressions.Regex.Match(input, @"([\d.]+)");
            return match.Success ? float.Parse(match.Groups[1].Value) : 0f;
        }

        private static string ParseMechanics(string input)
        {
            var mechanics = new Dictionary<string, object>();

            if (input.Contains("splash") || input.Contains("area"))
            {
                var radiusMatch = System.Text.RegularExpressions.Regex.Match(input, @"(\d+\.?\d*)\s*tile");
                mechanics["splashRadius"] = radiusMatch.Success ? float.Parse(radiusMatch.Groups[1].Value) : 1.5f;
            }

            if (input.Contains("charge"))
            {
                mechanics["charge"] = true;
                var rangeMatch = System.Text.RegularExpressions.Regex.Match(input, @"(\d+\.?\d*)\s*tile");
                mechanics["chargeRange"] = rangeMatch.Success ? float.Parse(rangeMatch.Groups[1].Value) : 3.5f;
                mechanics["chargeMultiplier"] = 2f;
            }

            if (input.Contains("spawn"))
                mechanics["spawns"] = true;

            if (input.Contains("slow"))
            {
                mechanics["slowPercent"] = 0.35f;
                mechanics["slowDuration"] = 1.5f;
            }

            if (input.Contains("stun"))
                mechanics["stunDuration"] = 0.5f;

            if (input.Contains("knockback"))
                mechanics["knockback"] = 0.5f;

            if (input.Contains("invisible"))
                mechanics["invisible"] = true;

            if (input.Contains("ramp") || input.Contains("ramps"))
                mechanics["damageRamp"] = true;

            if (input.Contains("pierce"))
                mechanics["pierce"] = true;

            if (input.Contains("heal"))
            {
                mechanics["healAmount"] = ParseNumber(input);
                mechanics["healRadius"] = 2.5f;
            }

            if (input.Contains("chain"))
                mechanics["chainTargets"] = 3;

            if (input.Contains("death"))
                mechanics["deathEffect"] = true;

            return JsonUtility.ToJson(new MechanicsData { data = mechanics });
        }

        private static void ApplyDefaults(CardDataDefinition card)
        {
            if (card.baseHitpoints == 0) card.baseHitpoints = 100;
            if (card.baseDamage == 0) card.baseDamage = 10;
            if (card.baseHitSpeed == 0) card.baseHitSpeed = 1f;
            if (card.baseRange == 0) card.baseRange = 1.2f;
            if (card.speed == 0) card.speed = SpeedType.Medium;
            if (card.targetType == 0) card.targetType = TargetType.Ground;
            if (card.deployTime == 0) card.deployTime = 1;
            if (card.count == 0) card.count = 1;
            if (string.IsNullOrEmpty(card.mechanicsJson)) card.mechanicsJson = "{}";

            var nameId = SanitizeFileName(card.cardName).ToLower();
            card.spriteId = $"sprite_{nameId}";
            card.portraitId = $"portrait_{nameId}";
            card.spineAssetName = $"spine_{nameId}";
            card.deploySound = $"sfx_unit_{nameId}_deploy";
            card.attackSound = $"sfx_unit_{nameId}_attack";
            card.hitSound = $"sfx_unit_{nameId}_hit";
            card.deathSound = $"sfx_unit_{nameId}_death";
        }

        private static string SanitizeFileName(string name)
        {
            return name.Replace(" ", "_")
                .Replace("'", "")
                .Replace(".", "")
                .Replace("-", "_")
                .Replace("(", "")
                .Replace(")", "");
        }

        private class CardDataDefinition
        {
            public int cardId;
            public string cardName;
            public CardType type = CardType.Troop;
            public CardRarity rarity = CardRarity.Common;
            public int elixirCost = 3;
            public int baseHitpoints = 100;
            public int baseDamage = 10;
            public float baseHitSpeed = 1f;
            public float baseRange = 1.2f;
            public SpeedType speed = SpeedType.Medium;
            public int deployTime = 1;
            public TargetType targetType = TargetType.Ground;
            public int count = 1;
            public string mechanicsJson = "{}";
            public string spriteId;
            public string portraitId;
            public string spineAssetName;
            public string deploySound;
            public string attackSound;
            public string hitSound;
            public string deathSound;
            public Dictionary<string, string> extraFields = new();
        }

        [Serializable]
        private class MechanicsData
        {
            public Dictionary<string, object> data;
        }

        private class AssetManifestEntry
        {
            public string id;
            public string name;
            public string type;
            public string rarity;
            public string path;
            public string dimensions;
            public string compression;
            public long memoryEstimateKB;
            public string dependencies;
        }
    }
}