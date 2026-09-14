using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CRClone.Data;

namespace CRClone.Editor
{
    public class AssetManifestGenerator : EditorWindow
    {
        private string _outputPath = "Assets/AssetManifest.csv";

        [MenuItem("CRClone/Asset Pipeline/Generate Asset Manifest")]
        public static void ShowWindow()
        {
            GetWindow<AssetManifestGenerator>("Asset Manifest Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Asset Manifest Generator", EditorStyles.boldLabel);
            GUILayout.Label("Creates CSV manifest of all game assets", EditorStyles.helpBox);

            EditorGUILayout.Space();
            _outputPath = EditorGUILayout.TextField("Output Path", _outputPath);

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate Manifest", GUILayout.Height(40)))
            {
                GenerateManifest();
            }
        }

        private void GenerateManifest()
        {
            var manifest = new List<AssetManifestEntry>();

            // Card Data
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
                    memoryEstimateKB = EstimateScriptableObjectSize(card),
                    dependencies = $"{card.spineAssetName},{card.spriteId},{card.portraitId}"
                });
            }

            // Prefabs
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var renderers = prefab.GetComponentsInChildren<Renderer>();
                int triCount = 0;
                int texCount = 0;
                long texMemory = 0;

                foreach (var r in renderers)
                {
                    if (r is SkinnedMeshRenderer smr)
                    {
                        if (smr.sharedMesh != null) triCount += smr.sharedMesh.triangles.Length / 3;
                    }
                    else if (r is MeshRenderer mr && mr.GetComponent<MeshFilter>() != null)
                    {
                        var mf = mr.GetComponent<MeshFilter>();
                        if (mf.sharedMesh != null) triCount += mf.sharedMesh.triangles.Length / 3;
                    }

                    if (r.sharedMaterial != null && r.sharedMaterial.mainTexture != null)
                    {
                        texCount++;
                        var tex = r.sharedMaterial.mainTexture as Texture2D;
                        if (tex != null)
                        {
                            texMemory += GetTextureMemory(tex);
                        }
                    }
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
                    dimensions = $"{triCount} tris, {texCount} textures",
                    compression = "N/A",
                    memoryEstimateKB = texMemory / 1024 + triCount * 4,
                    dependencies = GetPrefabDependencies(prefab)
                });
            }

            // Textures
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
                    var settings = importer.GetPlatformTextureSettings("Standalone");
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

            // Audio
            var audioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" });
            foreach (var guid in audioGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) continue;

                var importer = AssetImporter.GetAtPath(path) as UnityEditor.AudioImporter;
                string compression = "Unknown";
                if (importer != null)
                {
                    var settings = new AudioImporterSampleSettings();
                    importer.GetOverrideSampleSettings("Standalone", out settings);
                    compression = settings.compressionFormat.ToString();
                }

                manifest.Add(new AssetManifestEntry
                {
                    id = guid,
                    name = clip.name,
                    type = "AudioClip",
                    rarity = GetFolderName(path, 2),
                    path = path,
                    dimensions = $"{clip.length:F1}s, {clip.channels}ch, {clip.frequency}Hz",
                    compression = compression,
                    memoryEstimateKB = EstimateAudioMemory(clip) / 1024,
                    dependencies = "N/A"
                });
            }

            // Animations
            var animGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/Animations" });
            foreach (var guid in animGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip == null) continue;

                manifest.Add(new AssetManifestEntry
                {
                    id = guid,
                    name = clip.name,
                    type = "AnimationClip",
                    rarity = GetFolderName(path, 2),
                    path = path,
                    dimensions = $"{clip.length:F2}s, {clip.frameRate} fps",
                    compression = "N/A",
                    memoryEstimateKB = EstimateAnimationMemory(clip) / 1024,
                    dependencies = "N/A"
                });
            }

            // Shaders
            var shaderGuids = AssetDatabase.FindAssets("t:Shader", new[] { "Assets/Shaders" });
            foreach (var guid in shaderGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (shader == null) continue;

                manifest.Add(new AssetManifestEntry
                {
                    id = guid,
                    name = shader.name,
                    type = "Shader",
                    rarity = "N/A",
                    path = path,
                    dimensions = "N/A",
                    compression = "N/A",
                    memoryEstimateKB = 0,
                    dependencies = "N/A"
                });
            }

            // Write CSV
            using (var writer = new StreamWriter(_outputPath))
            {
                writer.WriteLine("ID,Name,Type,Rarity,Path,Dimensions,Compression,MemoryEstimateKB,Dependencies");
                foreach (var entry in manifest.OrderBy(e => e.type).ThenBy(e => e.name))
                {
                    writer.WriteLine($"\"{entry.id}\",\"{entry.name}\",\"{entry.type}\",\"{entry.rarity}\",\"{entry.path}\",\"{entry.dimensions}\",\"{entry.compression}\",{entry.memoryEstimateKB},\"{entry.dependencies}\"");
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[AssetManifestGenerator] Generated manifest with {manifest.Count} entries: {_outputPath}");
            EditorUtility.DisplayDialog("Complete", $"Manifest generated with {manifest.Count} entries", "OK");
        }

        private string GetFolderName(string path, int levelsUp)
        {
            var dir = Path.GetDirectoryName(path);
            for (int i = 0; i < levelsUp && dir != null; i++)
            {
                dir = Path.GetDirectoryName(dir);
            }
            return dir != null ? Path.GetFileName(dir) : "Unknown";
        }

        private string GetPrefabDependencies(GameObject prefab)
        {
            var deps = new List<string>();
            var renderers = prefab.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (r.sharedMaterial != null && r.sharedMaterial.shader != null)
                {
                    deps.Add(r.sharedMaterial.shader.name);
                }
            }
            var scripts = prefab.GetComponentsInChildren<MonoBehaviour>();
            foreach (var s in scripts)
            {
                if (s != null) deps.Add(s.GetType().Name);
            }
            return string.Join(";", deps.Distinct());
        }

        private int EstimateScriptableObjectSize(ScriptableObject obj)
        {
            // Rough estimate
            return 1;
        }

        private long GetTextureMemory(Texture2D tex)
        {
            if (tex == null) return 0;
            float bytesPerPixel = 4; // Default RGBA32
            switch (tex.format)
            {
                case TextureFormat.ASTC_4x4: bytesPerPixel = 1; break;
                case TextureFormat.ASTC_5x5: bytesPerPixel = 1; break;
                case TextureFormat.ASTC_6x6: bytesPerPixel = 1; break;
                case TextureFormat.ASTC_8x8: bytesPerPixel = 1; break;
                case TextureFormat.ASTC_10x10: bytesPerPixel = 1; break;
                case TextureFormat.ASTC_12x12: bytesPerPixel = 1; break;
                case TextureFormat.DXT1: bytesPerPixel = 0.5f; break;
                case TextureFormat.DXT5: bytesPerPixel = 1; break;
                case TextureFormat.BC7: bytesPerPixel = 1; break;
                case TextureFormat.RGBA32: bytesPerPixel = 4; break;
                case TextureFormat.RGB24: bytesPerPixel = 3; break;
                case TextureFormat.Alpha8: bytesPerPixel = 1; break;
            }
            return (long)(tex.width * tex.height * bytesPerPixel * (tex.mipmapCount > 1 ? 1.33f : 1.0f));
        }

        private long EstimateAudioMemory(AudioClip clip)
        {
            if (clip == null) return 0;
            return (long)(clip.samples * clip.channels * 2); // 16-bit
        }

        private long EstimateAnimationMemory(AnimationClip clip)
        {
            if (clip == null) return 0;
            // Rough estimate based on curve count and length
            return (long)(clip.length * clip.frameRate * 100);
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