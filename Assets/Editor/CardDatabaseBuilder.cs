using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using CRClone.Data;

namespace CRClone.Editor
{
    public class CardDatabaseBuilder : EditorWindow
    {
        private string _cardsDatabasePath = "docs/planning/phase1/CARDS_DATABASE.md";
        private string _outputPath = "Assets/Resources/Data/Cards";
        private bool _overwriteExisting = true;
        private Vector2 _scrollPosition;
        private List<CardParseResult> _parseResults = new();
        private bool _parsingComplete = false;

        [MenuItem("CRClone/Asset Pipeline/Card Database Builder")]
        public static void ShowWindow()
        {
            GetWindow<CardDatabaseBuilder>("Card Database Builder");
        }

        private void OnGUI()
        {
            GUILayout.Label("Card Database Builder", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Source File:", EditorStyles.boldLabel);
            _cardsDatabasePath = EditorGUILayout.TextField("Path", _cardsDatabasePath);
            if (GUILayout.Button("Browse"))
            {
                var path = EditorUtility.OpenFilePanel("Select Cards Database", "", "md");
                if (!string.IsNullOrEmpty(path))
                {
                    _cardsDatabasePath = MakeRelativePath(path);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Output Folder:", EditorStyles.boldLabel);
            _outputPath = EditorGUILayout.TextField("Path", _outputPath);
            if (GUILayout.Button("Browse Output"))
            {
                var path = EditorUtility.OpenFolderPanel("Select Output Folder", _outputPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    _outputPath = MakeRelativePath(path);
                }
            }

            _overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", _overwriteExisting);

            EditorGUILayout.Space();
            if (GUILayout.Button("Parse & Generate Cards", GUILayout.Height(40)))
            {
                ParseAndGenerate();
            }

            EditorGUILayout.Space();
            if (_parsingComplete)
            {
                GUILayout.Label($"Results: {_parseResults.Count} cards parsed", EditorStyles.boldLabel);
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300));
                foreach (var result in _parseResults)
                {
                    var style = result.success ? EditorStyles.label : EditorStyles.boldLabel;
                    EditorGUILayout.LabelField($"{result.cardId}: {result.cardName} ({result.rarity}) - {(result.success ? "OK" : result.error)}", style);
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private string MakeRelativePath(string absolutePath)
        {
            var projectPath = Application.dataPath.Replace("/Assets", "");
            if (absolutePath.StartsWith(projectPath))
            {
                return "Assets" + absolutePath.Substring(projectPath.Length).Replace("\\", "/");
            }
            return absolutePath;
        }

        private void ParseAndGenerate()
        {
            _parseResults.Clear();
            _parsingComplete = false;

            if (!File.Exists(_cardsDatabasePath))
            {
                Debug.LogError($"[CardDatabaseBuilder] File not found: {_cardsDatabasePath}");
                EditorUtility.DisplayDialog("Error", $"File not found: {_cardsDatabasePath}", "OK");
                return;
            }

            var content = File.ReadAllText(_cardsDatabasePath);
            var cards = ParseCardsDatabase(content);

            if (!Directory.Exists(_outputPath))
            {
                Directory.CreateDirectory(_outputPath);
            }

            int created = 0, updated = 0, skipped = 0;
            foreach (var card in cards)
            {
                var result = CreateCardAsset(card);
                _parseResults.Add(result);
                if (result.success)
                {
                    if (result.isNew) created++;
                    else updated++;
                }
                else
                {
                    skipped++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _parsingComplete = true;
            Debug.Log($"[CardDatabaseBuilder] Complete: {created} created, {updated} updated, {skipped} skipped");
            EditorUtility.DisplayDialog("Complete", $"Generated {created} new cards, updated {updated}, skipped {skipped}", "OK");
            Repaint();
        }

        private List<CardDataDefinition> ParseCardsDatabase(string content)
        {
            var cards = new List<CardDataDefinition>();
            var lines = content.Split('\n');

            CardDataDefinition currentCard = null;
            string currentSection = "";

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                // Detect card sections (### Number. Name)
                var cardMatch = Regex.Match(line, @"^###\s+(\d+)\.\s+(.+)$");
                if (cardMatch.Success)
                {
                    if (currentCard != null)
                    {
                        cards.Add(currentCard);
                    }

                    currentCard = new CardDataDefinition
                    {
                        cardId = int.Parse(cardMatch.Groups[1].Value),
                        cardName = cardMatch.Groups[2].Value.Trim(),
                        mechanicsJson = "{}"
                    };
                    currentSection = "";
                    continue;
                }

                if (currentCard == null) continue;

                // Parse key-value pairs
                if (line.StartsWith("- **Type**:"))
                {
                    currentCard.type = ParseCardType(line.Substring(10).Trim());
                }
                else if (line.StartsWith("- **Rarity**:"))
                {
                    currentCard.rarity = ParseRarity(line.Substring(12).Trim());
                }
                else if (line.StartsWith("- **Elixir**:"))
                {
                    currentCard.elixirCost = int.Parse(line.Substring(11).Trim());
                }
                else if (line.StartsWith("- **HP**:") || line.StartsWith("- **HP**:"))
                {
                    currentCard.baseHitpoints = ParseNumber(line.Substring(6).Trim());
                }
                else if (line.StartsWith("- **Damage**:"))
                {
                    currentCard.baseDamage = ParseNumber(line.Substring(11).Trim());
                }
                else if (line.StartsWith("- **Hit Speed**:"))
                {
                    currentCard.baseHitSpeed = ParseFloat(line.Substring(13).Trim());
                }
                else if (line.StartsWith("- **Range**:"))
                {
                    currentCard.baseRange = ParseFloat(line.Substring(9).Trim());
                }
                else if (line.StartsWith("- **Target**:"))
                {
                    currentCard.targetType = ParseTargetType(line.Substring(10).Trim());
                }
                else if (line.StartsWith("- **Speed**:"))
                {
                    currentCard.speed = ParseSpeedType(line.Substring(10).Trim());
                }
                else if (line.StartsWith("- **Deploy Time**:"))
                {
                    currentCard.deployTime = int.Parse(line.Substring(16).Trim());
                }
                else if (line.StartsWith("- **Count**:"))
                {
                    currentCard.count = int.Parse(line.Substring(10).Trim());
                }
                else if (line.StartsWith("- **Mechanic**:") || line.StartsWith("- **Mechanics**:"))
                {
                    currentCard.mechanicsJson = ParseMechanics(line.Substring(line.IndexOf(':') + 1).Trim());
                }
                else if (line.StartsWith("- **") && line.Contains("**:"))
                {
                    // Handle other fields like Knockback, Width, etc.
                    var keyMatch = Regex.Match(line, @"-\s\*\*(.+?)\*\*:\s*(.+)");
                    if (keyMatch.Success)
                    {
                        var key = keyMatch.Groups[1].Value;
                        var value = keyMatch.Groups[2].Value;
                        currentCard.extraFields[key] = value;
                    }
                }
            }

            if (currentCard != null)
            {
                cards.Add(currentCard);
            }

            // Apply defaults for missing values
            foreach (var card in cards)
            {
                ApplyDefaults(card);
            }

            return cards;
        }

        private CardType ParseCardType(string input)
        {
            input = input.ToLower();
            if (input.Contains("spell") && input.Contains("spawn")) return CardType.Spell;
            if (input.Contains("spell")) return CardType.Spell;
            if (input.Contains("building")) return CardType.Building;
            if (input.Contains("champion")) return CardType.Champion;
            if (input.Contains("troop")) return CardType.Troop;
            return CardType.Troop;
        }

        private CardRarity ParseRarity(string input)
        {
            input = input.ToLower();
            if (input.Contains("champion")) return CardRarity.Champion;
            if (input.Contains("legendary")) return CardRarity.Legendary;
            if (input.Contains("epic")) return CardRarity.Epic;
            if (input.Contains("rare")) return CardRarity.Rare;
            return CardRarity.Common;
        }

        private TargetType ParseTargetType(string input)
        {
            input = input.ToLower();
            if (input.Contains("air & ground") || input.Contains("air and ground") || input.Contains("both")) return TargetType.Both;
            if (input.Contains("air")) return TargetType.Air;
            if (input.Contains("building")) return TargetType.Buildings;
            if (input.Contains("ground")) return TargetType.Ground;
            return TargetType.Any;
        }

        private SpeedType ParseSpeedType(string input)
        {
            input = input.ToLower();
            if (input.Contains("very fast")) return SpeedType.VeryFast;
            if (input.Contains("fast")) return SpeedType.Fast;
            if (input.Contains("medium")) return SpeedType.Medium;
            if (input.Contains("slow")) return SpeedType.Slow;
            if (input.Contains("very slow")) return SpeedType.VerySlow;
            return SpeedType.Medium;
        }

        private int ParseNumber(string input)
        {
            // Handle "123 (×6 = 738 total)" format
            var match = Regex.Match(input, @"(\d+)");
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }

        private float ParseFloat(string input)
        {
            // Handle "1.2 sec" format
            var match = Regex.Match(input, @"([\d.]+)");
            return match.Success ? float.Parse(match.Groups[1].Value) : 0f;
        }

        private string ParseMechanics(string input)
        {
            // Convert mechanic description to JSON
            var mechanics = new Dictionary<string, object>();

            if (input.Contains("splash") || input.Contains("area"))
            {
                var radiusMatch = Regex.Match(input, @"(\d+\.?\d*)\s*tile");
                if (radiusMatch.Success)
                {
                    mechanics["splashRadius"] = float.Parse(radiusMatch.Groups[1].Value);
                }
                else
                {
                    mechanics["splashRadius"] = 1.5f;
                }
            }

            if (input.Contains("charge"))
            {
                mechanics["charge"] = true;
                var rangeMatch = Regex.Match(input, @"(\d+\.?\d*)\s*tile");
                if (rangeMatch.Success)
                {
                    mechanics["chargeRange"] = float.Parse(rangeMatch.Groups[1].Value);
                }
                mechanics["chargeMultiplier"] = 2f;
            }

            if (input.Contains("spawn"))
            {
                mechanics["spawns"] = true;
                // Extract spawn details from description
            }

            if (input.Contains("slow"))
            {
                mechanics["slowPercent"] = 0.35f;
                mechanics["slowDuration"] = 1.5f;
            }

            if (input.Contains("stun"))
            {
                mechanics["stunDuration"] = 0.5f;
            }

            if (input.Contains("knockback"))
            {
                mechanics["knockback"] = 0.5f;
            }

            if (input.Contains("invisible"))
            {
                mechanics["invisible"] = true;
            }

            if (input.Contains("ramp") || input.Contains("ramps"))
            {
                mechanics["damageRamp"] = true;
            }

            if (input.Contains("pierce"))
            {
                mechanics["pierce"] = true;
            }

            if (input.Contains("heal"))
            {
                mechanics["healAmount"] = ParseNumber(input);
                mechanics["healRadius"] = 2.5f;
            }

            if (input.Contains("chain"))
            {
                mechanics["chainTargets"] = 3;
            }

            if (input.Contains("death"))
            {
                mechanics["deathEffect"] = true;
            }

            return JsonUtility.ToJson(new MechanicsData { data = mechanics });
        }

        private void ApplyDefaults(CardDataDefinition card)
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

            // Generate sprite/portrait IDs
            var nameId = card.cardName.ToLower()
                .Replace(" ", "_")
                .Replace(".", "")
                .Replace("'", "")
                .Replace("-", "_");
            card.spriteId = $"sprite_{nameId}";
            card.portraitId = $"portrait_{nameId}";
            card.spineAssetName = $"spine_{nameId}";

            // Audio clips
            card.deploySound = $"sfx_unit_{nameId}_deploy";
            card.attackSound = $"sfx_unit_{nameId}_attack";
            card.hitSound = $"sfx_unit_{nameId}_hit";
            card.deathSound = $"sfx_unit_{nameId}_death";
        }

        private CardParseResult CreateCardAsset(CardDataDefinition def)
        {
            var assetPath = $"{_outputPath}/Card_{def.cardId:D3}_{def.cardName.Replace(" ", "_")}.asset";
            assetPath = assetPath.Replace("'", "").Replace(".", "").Replace("-", "_");

            CardData asset = null;
            bool isNew = false;

            if (File.Exists(assetPath) && !_overwriteExisting)
            {
                return new CardParseResult
                {
                    cardId = def.cardId,
                    cardName = def.cardName,
                    rarity = def.rarity,
                    success = false,
                    error = "Already exists (overwrite disabled)",
                    isNew = false
                };
            }

            if (File.Exists(assetPath))
            {
                asset = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
            }

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CardData>();
                isNew = true;
            }

            asset.cardId = def.cardId;
            asset.cardName = def.cardName;
            asset.nameKey = $"card_{def.cardName.ToLower().Replace(" ", "_")}_name";
            asset.descriptionKey = $"card_{def.cardName.ToLower().Replace(" ", "_")}_desc";
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

            if (isNew)
            {
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            else
            {
                EditorUtility.SetDirty(asset);
            }

            return new CardParseResult
            {
                cardId = def.cardId,
                cardName = def.cardName,
                rarity = def.rarity,
                success = true,
                error = "",
                isNew = isNew
            };
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

        private class CardParseResult
        {
            public int cardId;
            public string cardName;
            public CardRarity rarity;
            public bool success;
            public string error;
            public bool isNew;
        }

        [Serializable]
        private class MechanicsData
        {
            public Dictionary<string, object> data;
        }
    }
}