using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using CRClone.Data;
using CRClone.Core;

namespace CRClone.Editor
{
    public static class GenerateCardsScript
    {
        [MenuItem("CRClone/Asset Pipeline/Generate All Cards (Scriptable)")]
        public static void GenerateAllCards()
        {
            var databasePath = "docs/planning/phase1/CARDS_DATABASE.md";
            var outputPath = "Assets/Resources/Data/Cards";

            if (!File.Exists(databasePath))
            {
                Debug.LogError($"[GenerateCardsScript] Database not found: {databasePath}");
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
            Debug.Log($"[GenerateCardsScript] Complete: {created} created, {updated} updated");
            EditorUtility.DisplayDialog("Complete", $"Generated {created} new cards, updated {updated}", "OK");
        }

        private static List<CardDataDefinition> ParseCardsDatabase(string content)
        {
            var cards = new List<CardDataDefinition>();
            var lines = content.Split('\n');

            CardDataDefinition currentCard = null;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

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
                    continue;
                }

                if (currentCard == null) continue;

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
                    currentCard.elixirCost = int.Parse(Regex.Match(line.Substring(11).Trim(), @"(\d+)").Groups[1].Value);
                }
                else if (line.StartsWith("- **HP**:"))
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
                    currentCard.deployTime = int.Parse(Regex.Match(line.Substring(16).Trim(), @"(\d+)").Groups[1].Value);
                }
                else if (line.StartsWith("- **Count**:"))
                {
                    currentCard.count = int.Parse(Regex.Match(line.Substring(10).Trim(), @"(\d+)").Groups[1].Value);
                }
                else if (line.StartsWith("- **Mechanic**:") || line.StartsWith("- **Mechanics**:"))
                {
                    currentCard.mechanicsJson = ParseMechanics(line.Substring(line.IndexOf(':') + 1).Trim());
                }
                else if (line.StartsWith("- **") && line.Contains("**:"))
                {
                    var keyMatch = Regex.Match(line, @"-\s\*\*(.+?)\*\*:\s*(.+)");
                    if (keyMatch.Success)
                    {
                        currentCard.extraFields[keyMatch.Groups[1].Value] = keyMatch.Groups[2].Value;
                    }
                }
            }

            if (currentCard != null)
            {
                cards.Add(currentCard);
            }

            foreach (var card in cards)
            {
                ApplyDefaults(card);
            }

            return cards;
        }

        private static void ApplyCardData(CardData asset, CardDataDefinition def)
        {
            asset.cardId = def.cardId;
            asset.cardName = def.cardName;
            asset.nameKey = $"card_{def.cardName.ToLower().Replace(" ", "_").Replace("'", "").Replace(".", "").Replace("-", "_")}_name";
            asset.descriptionKey = $"card_{def.cardName.ToLower().Replace(" ", "_").Replace("'", "").Replace(".", "").Replace("-", "_")}_desc";
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
            var match = Regex.Match(input, @"(\d+)");
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }

        private static float ParseFloat(string input)
        {
            var match = Regex.Match(input, @"([\d.]+)");
            return match.Success ? float.Parse(match.Groups[1].Value) : 0f;
        }

        private static string ParseMechanics(string input)
        {
            var mechanics = new Dictionary<string, object>();

            if (input.Contains("splash") || input.Contains("area"))
            {
                var radiusMatch = Regex.Match(input, @"(\d+\.?\d*)\s*tile");
                mechanics["splashRadius"] = radiusMatch.Success ? float.Parse(radiusMatch.Groups[1].Value) : 1.5f;
            }

            if (input.Contains("charge"))
            {
                mechanics["charge"] = true;
                var rangeMatch = Regex.Match(input, @"(\d+\.?\d*)\s*tile");
                mechanics["chargeRange"] = rangeMatch.Success ? float.Parse(rangeMatch.Groups[1].Value) : 3.5f;
                mechanics["chargeMultiplier"] = 2f;
            }

            if (input.Contains("spawn"))
            {
                mechanics["spawns"] = true;
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
    }
}