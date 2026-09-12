using System;
using System.Collections.Generic;
using System.Globalization;
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
        private string _cardsDocsPath = "docs/planning/phase1/cards";
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

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                // Non-card subsections (e.g. "### Crown Towers") terminate the
                // current card so tower stats can't leak into the last card.
                if (line.StartsWith("### ") && !Regex.IsMatch(line, @"^###\s+\d+\."))
                {
                    if (currentCard != null)
                    {
                        cards.Add(currentCard);
                        currentCard = null;
                    }
                    continue;
                }

                // Detect card sections (### Number. Name)
                var cardMatch = Regex.Match(line, @"^###\s+(\d+)\.\s+(.+)$");
                if (cardMatch.Success)
                {
                    if (currentCard != null)
                    {
                        cards.Add(currentCard);
                    }

                    // Strip alias suffixes like "(listed above as Rare)" or "(Champion)"
                    // so asset names stay clean and duplicates resolve to the same card.
                    var rawName = cardMatch.Groups[2].Value.Trim();
                    currentCard = new CardDataDefinition
                    {
                        cardId = int.Parse(cardMatch.Groups[1].Value),
                        cardName = CleanCardName(rawName),
                        mechanicsJson = "{}"
                    };
                    continue;
                }

                if (currentCard == null) continue;

                // Parse key-value pairs. Values are taken as the text after the
                // first ':' and then number-extracted, so suffixed values like
                // "1 sec", "2.5 tiles" or "Previous card +1" cannot throw.
                if (line.StartsWith("- **Type**:"))
                {
                    currentCard.type = ParseCardType(ValueAfterColon(line));
                    currentCard.parsedFields.Add("type");
                }
                else if (line.StartsWith("- **Rarity**:"))
                {
                    currentCard.rarity = ParseRarity(ValueAfterColon(line));
                    currentCard.parsedFields.Add("rarity");
                }
                else if (line.StartsWith("- **Elixir**:"))
                {
                    currentCard.elixirCost = ParseLeadingInt(ValueAfterColon(line), currentCard.elixirCost);
                    currentCard.parsedFields.Add("elixir");
                }
                else if (line.StartsWith("- **HP**:"))
                {
                    currentCard.baseHitpoints = ParseNumber(ValueAfterColon(line));
                    currentCard.parsedFields.Add("hp");
                }
                else if (line.StartsWith("- **Damage**:"))
                {
                    currentCard.baseDamage = ParseNumber(ValueAfterColon(line));
                    currentCard.parsedFields.Add("damage");
                }
                else if (line.StartsWith("- **Hit Speed**:"))
                {
                    currentCard.baseHitSpeed = ParseFloat(ValueAfterColon(line));
                    currentCard.parsedFields.Add("hitspeed");
                }
                else if (line.StartsWith("- **Range**:"))
                {
                    currentCard.baseRange = ParseFloat(ValueAfterColon(line));
                    currentCard.parsedFields.Add("range");
                }
                else if (line.StartsWith("- **Target**:"))
                {
                    currentCard.targetType = ParseTargetType(ValueAfterColon(line));
                    currentCard.parsedFields.Add("target");
                }
                else if (line.StartsWith("- **Speed**:"))
                {
                    currentCard.speed = ParseSpeedType(ValueAfterColon(line));
                    currentCard.parsedFields.Add("speed");
                }
                else if (line.StartsWith("- **Deploy Time**:"))
                {
                    currentCard.deployTime = ParseLeadingInt(ValueAfterColon(line), currentCard.deployTime);
                    currentCard.parsedFields.Add("deploy");
                }
                else if (line.StartsWith("- **Count**:"))
                {
                    currentCard.count = ParseLeadingInt(ValueAfterColon(line), currentCard.count);
                    currentCard.parsedFields.Add("count");
                }
                else if (line.StartsWith("- **Mechanic**:") || line.StartsWith("- **Mechanics**:"))
                {
                    var mechanics = ParseMechanics(ValueAfterColon(line));
                    currentCard.mechanicsJson = mechanics;
                    if (mechanics != "{}") currentCard.parsedFields.Add("mechanics");
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

            // Placeholder entries such as "Princess (listed above as Legendary)"
            // carry no primary stats (parsedFields is empty); inherit them from
            // the referenced full entry so every emitted asset has real,
            // non-default data. Runs before the doc merge so stub docs (e.g.
            // champion specs with "Elixir Cost: ?") can't mask a placeholder.
            ResolvePlaceholderCards(cards);

            // Supplement fields still missing from the main database with the
            // per-card specification docs (table + Basic Info format).
            MergePerCardDocs(cards);

            // Apply defaults for missing values
            foreach (var card in cards)
            {
                ApplyDefaults(card);
            }

            return cards;
        }

        private void ResolvePlaceholderCards(List<CardDataDefinition> cards)
        {
            foreach (var card in cards)
            {
                if (card.parsedFields.Count > 0) continue;

                CardDataDefinition donor = null;
                foreach (var other in cards)
                {
                    if (other == card || other.parsedFields.Count == 0) continue;
                    if (string.Equals(other.cardName, card.cardName, StringComparison.OrdinalIgnoreCase))
                    {
                        donor = other;
                        break;
                    }
                }

                if (donor == null)
                {
                    Debug.LogWarning($"[CardDatabaseBuilder] No stat source for placeholder card {card.cardId}: {card.cardName}");
                    continue;
                }

                card.type = donor.type;
                card.rarity = donor.rarity;
                card.elixirCost = donor.elixirCost;
                card.baseHitpoints = donor.baseHitpoints;
                card.baseDamage = donor.baseDamage;
                card.baseHitSpeed = donor.baseHitSpeed;
                card.baseRange = donor.baseRange;
                card.speed = donor.speed;
                card.deployTime = donor.deployTime;
                card.targetType = donor.targetType;
                card.count = donor.count;
                card.mechanicsJson = donor.mechanicsJson;
                foreach (var kvp in donor.extraFields) card.extraFields[kvp.Key] = kvp.Value;
                foreach (var field in donor.parsedFields) card.parsedFields.Add(field);
            }
        }

        private void MergePerCardDocs(List<CardDataDefinition> cards)
        {
            if (!Directory.Exists(_cardsDocsPath)) return;

            var docByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in Directory.GetFiles(_cardsDocsPath, "*.md", SearchOption.AllDirectories))
            {
                var key = SanitizeId(Path.GetFileNameWithoutExtension(file));
                if (!docByName.ContainsKey(key)) docByName[key] = file;
            }

            foreach (var card in cards)
            {
                if (!docByName.TryGetValue(SanitizeId(card.cardName), out var docPath)) continue;
                MergeSingleDoc(card, File.ReadAllText(docPath));
            }
        }

        private void MergeSingleDoc(CardDataDefinition card, string docContent)
        {
            foreach (var rawLine in docContent.Split('\n'))
            {
                var line = rawLine.Trim();
                string key = null;
                string value = null;

                // Table rows: | Stat | Value |
                if (line.StartsWith("|"))
                {
                    var cells = line.Split('|');
                    if (cells.Length < 3) continue;
                    key = cells[1].Trim();
                    value = cells[2].Trim();
                    if (key.StartsWith("---") || key.Equals("Stat", StringComparison.OrdinalIgnoreCase)) continue;
                }
                // Basic Info bullets: - **Elixir Cost**: 3
                else if (line.StartsWith("- **"))
                {
                    var m = Regex.Match(line, @"-\s\*\*(.+?)\*\*:\s*(.+)");
                    if (!m.Success) continue;
                    key = m.Groups[1].Value.Trim();
                    value = m.Groups[2].Value.Trim();
                }
                else
                {
                    continue;
                }

                var norm = key.ToLowerInvariant();
                if ((norm == "elixir cost" || norm == "elixir") && !card.parsedFields.Contains("elixir"))
                {
                    int v = ParseLeadingInt(value, 0);
                    if (v > 0) { card.elixirCost = v; card.parsedFields.Add("elixir"); }
                }
                else if (norm == "rarity" && !card.parsedFields.Contains("rarity"))
                {
                    card.rarity = ParseRarity(value);
                    card.parsedFields.Add("rarity");
                }
                else if ((norm == "type") && !card.parsedFields.Contains("type"))
                {
                    card.type = ParseCardType(value);
                    card.parsedFields.Add("type");
                }
                else if ((norm == "hitpoints" || norm == "hp") && !card.parsedFields.Contains("hp"))
                {
                    int v = ParseNumber(value);
                    if (v > 0) { card.baseHitpoints = v; card.parsedFields.Add("hp"); }
                }
                else if ((norm == "damage") && !card.parsedFields.Contains("damage"))
                {
                    int v = ParseNumber(value);
                    if (v > 0) { card.baseDamage = v; card.parsedFields.Add("damage"); }
                }
                else if ((norm == "hit speed") && !card.parsedFields.Contains("hitspeed"))
                {
                    float v = ParseFloat(value);
                    if (v > 0) { card.baseHitSpeed = v; card.parsedFields.Add("hitspeed"); }
                }
                else if ((norm == "range") && !card.parsedFields.Contains("range"))
                {
                    float v = ParseFloat(value);
                    if (v > 0) { card.baseRange = v; card.parsedFields.Add("range"); }
                }
                else if ((norm == "speed") && !card.parsedFields.Contains("speed"))
                {
                    card.speed = ParseSpeedType(value);
                    card.parsedFields.Add("speed");
                }
                else if ((norm == "deploy time") && !card.parsedFields.Contains("deploy"))
                {
                    int v = ParseLeadingInt(value, 0);
                    if (v > 0) { card.deployTime = v; card.parsedFields.Add("deploy"); }
                }
                else if ((norm == "target") && !card.parsedFields.Contains("target"))
                {
                    card.targetType = ParseTargetType(value);
                    card.parsedFields.Add("target");
                }
                else if ((norm == "count") && !card.parsedFields.Contains("count"))
                {
                    int v = ParseLeadingInt(value, 0);
                    if (v > 0) { card.count = v; card.parsedFields.Add("count"); }
                }
                else if ((norm == "mechanic" || norm == "mechanics") && !card.parsedFields.Contains("mechanics"))
                {
                    var mechanics = ParseMechanics(value);
                    if (mechanics != "{}") { card.mechanicsJson = mechanics; card.parsedFields.Add("mechanics"); }
                }
            }
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
            // Rarity changes are written "Epic → Rare (changed)": the
            // effective rarity is the segment after the arrow.
            string effective = input ?? string.Empty;
            int arrow = effective.LastIndexOf('→');
            if (arrow < 0) arrow = effective.LastIndexOf("->", StringComparison.Ordinal);
            if (arrow >= 0) effective = effective.Substring(arrow + 1);

            effective = effective.ToLower();
            if (effective.Contains("champion")) return CardRarity.Champion;
            if (effective.Contains("legendary")) return CardRarity.Legendary;
            if (effective.Contains("epic")) return CardRarity.Epic;
            if (effective.Contains("rare")) return CardRarity.Rare;
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
            // Match the leading speed token so parentheticals like
            // "Medium (Fast when charging)" don't mis-resolve to Fast.
            var s = (input ?? string.Empty).Trim().ToLowerInvariant();
            if (s.StartsWith("very fast")) return SpeedType.VeryFast;
            if (s.StartsWith("very slow")) return SpeedType.VerySlow;
            if (s.StartsWith("fast")) return SpeedType.Fast;
            if (s.StartsWith("medium")) return SpeedType.Medium;
            if (s.StartsWith("slow")) return SpeedType.Slow;
            return SpeedType.Medium;
        }

        private int ParseNumber(string input)
        {
            // Handle "123 (×6 = 738 total)" format
            var match = Regex.Match(input ?? string.Empty, @"(\d+)");
            return match.Success ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
        }

        private int ParseLeadingInt(string input, int fallback)
        {
            // Only a leading number counts, so "Previous card +1" keeps the default.
            var match = Regex.Match(input ?? string.Empty, @"^\s*(\d+)");
            return match.Success ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : fallback;
        }

        private float ParseFloat(string input)
        {
            // Handle "1.2 sec" format
            var match = Regex.Match(input ?? string.Empty, @"([\d.]+)");
            return match.Success ? float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : 0f;
        }

        private static string ValueAfterColon(string line)
        {
            int idx = line.IndexOf(':');
            return idx >= 0 ? line.Substring(idx + 1).Trim() : string.Empty;
        }

        private static string CleanCardName(string rawName)
        {
            // Drop trailing alias suffixes: "(listed above as Rare)", "(Champion)".
            return Regex.Replace(rawName ?? string.Empty, @"\s*\(.*?\)\s*$", "").Trim();
        }

        private static string SanitizeId(string name)
        {
            return (name ?? string.Empty).ToLowerInvariant()
                .Replace(" ", "_")
                .Replace(".", "")
                .Replace("'", "")
                .Replace("-", "_");
        }

        private static string SanitizeFileName(string name)
        {
            var s = (name ?? string.Empty).Replace(" ", "_").Replace("-", "_").Replace(".", "").Replace("'", "");
            return Regex.Replace(s, @"[^A-Za-z0-9_]", "");
        }

        private static string F(float v)
        {
            return v.ToString(CultureInfo.InvariantCulture);
        }

        private string ParseMechanics(string input)
        {
            // Convert mechanic description to JSON. Built manually because
            // Unity's JsonUtility cannot serialize dictionaries (it would
            // emit "{}" for every card).
            var parts = new List<string>();
            var text = input ?? string.Empty;

            if (text.Contains("splash") || text.Contains("area"))
            {
                var radiusMatch = Regex.Match(text, @"(\d+\.?\d*)\s*tile");
                float radius = radiusMatch.Success
                    ? float.Parse(radiusMatch.Groups[1].Value, CultureInfo.InvariantCulture)
                    : 1.5f;
                parts.Add($"\"splashRadius\":{F(radius)}");
            }

            if (text.Contains("charge"))
            {
                parts.Add("\"charge\":true");
                var rangeMatch = Regex.Match(text, @"(\d+\.?\d*)\s*tile");
                if (rangeMatch.Success)
                {
                    parts.Add($"\"chargeRange\":{F(float.Parse(rangeMatch.Groups[1].Value, CultureInfo.InvariantCulture))}");
                }
                parts.Add("\"chargeMultiplier\":2");
            }

            if (text.Contains("spawn"))
            {
                parts.Add("\"spawns\":true");
            }

            if (text.Contains("slow"))
            {
                parts.Add("\"slowPercent\":0.35");
                parts.Add("\"slowDuration\":1.5");
            }

            if (text.Contains("stun"))
            {
                parts.Add("\"stunDuration\":0.5");
            }

            if (text.Contains("knockback"))
            {
                parts.Add("\"knockback\":0.5");
            }

            if (text.Contains("invisible"))
            {
                parts.Add("\"invisible\":true");
            }

            if (text.Contains("ramp") || text.Contains("ramps"))
            {
                parts.Add("\"damageRamp\":true");
            }

            if (text.Contains("pierce"))
            {
                parts.Add("\"pierce\":true");
            }

            if (text.Contains("heal"))
            {
                parts.Add($"\"healAmount\":{ParseNumber(text)}");
                parts.Add("\"healRadius\":2.5");
            }

            if (text.Contains("chain"))
            {
                parts.Add("\"chainTargets\":3");
            }

            if (text.Contains("death"))
            {
                parts.Add("\"deathEffect\":true");
            }

            if (parts.Count == 0) return "{}";
            return "{" + string.Join(",", parts) + "}";
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
            var nameId = SanitizeId(card.cardName);
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
            var assetPath = $"{_outputPath}/Card_{def.cardId:D3}_{SanitizeFileName(def.cardName)}.asset";

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
            asset.nameKey = $"card_{SanitizeId(def.cardName)}_name";
            asset.descriptionKey = $"card_{SanitizeId(def.cardName)}_desc";
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
            public HashSet<string> parsedFields = new HashSet<string>();
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

    }
}