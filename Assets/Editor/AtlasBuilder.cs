using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace CRClone.Editor
{
    public class AtlasBuilder : EditorWindow
    {
        private string _texturePackerPath = "";
        private string _sourceFolder = "Assets/Art/UI/Icons";
        private string _outputFolder = "Assets/Art/UI";
        private string _atlasName = "UIAtlas";
        private int _maxSize = 2048;
        private bool _allowRotation = true;
        private bool _trimMode = true;
        private int _padding = 2;
        private bool _powerOfTwo = true;

        [MenuItem("CRClone/Asset Pipeline/Atlas Builder")]
        public static void ShowWindow()
        {
            GetWindow<AtlasBuilder>("Atlas Builder");
        }

        private void OnGUI()
        {
            GUILayout.Label("Texture Atlas Builder", EditorStyles.boldLabel);
            GUILayout.Label("Requires TexturePacker CLI installed", EditorStyles.helpBox);

            EditorGUILayout.Space();
            _texturePackerPath = EditorGUILayout.TextField("TexturePacker Path", _texturePackerPath);
            if (GUILayout.Button("Auto-Detect"))
            {
                AutoDetectTexturePacker();
            }

            EditorGUILayout.Space();
            _sourceFolder = EditorGUILayout.TextField("Source Folder", _sourceFolder);
            _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);
            _atlasName = EditorGUILayout.TextField("Atlas Name", _atlasName);

            EditorGUILayout.Space();
            _maxSize = EditorGUILayout.IntField("Max Size", _maxSize);
            _padding = EditorGUILayout.IntField("Padding", _padding);
            _allowRotation = EditorGUILayout.Toggle("Allow Rotation", _allowRotation);
            _trimMode = EditorGUILayout.Toggle("Trim Transparent", _trimMode);
            _powerOfTwo = EditorGUILayout.Toggle("Power of Two", _powerOfTwo);

            EditorGUILayout.Space();
            if (GUILayout.Button("Build Atlas", GUILayout.Height(40)))
            {
                BuildAtlas();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Build All Atlases (UI + Units)", GUILayout.Height(30)))
            {
                BuildAllAtlases();
            }
        }

        private void AutoDetectTexturePacker()
        {
            var paths = new[]
            {
                @"C:\Program Files\TexturePacker\TexturePacker.exe",
                @"C:\Program Files (x86)\TexturePacker\TexturePacker.exe",
                "/Applications/TexturePacker.app/Contents/MacOS/TexturePacker",
                "/usr/bin/TexturePacker"
            };

            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    _texturePackerPath = path;
                    UnityEngine.Debug.Log($"[AtlasBuilder] Found TexturePacker at: {path}");
                    return;
                }
            }

            EditorUtility.DisplayDialog("Not Found", "TexturePacker not found in standard locations. Please install or set path manually.", "OK");
        }

        private void BuildAtlas()
        {
            if (string.IsNullOrEmpty(_texturePackerPath) || !File.Exists(_texturePackerPath))
            {
                EditorUtility.DisplayDialog("Error", "TexturePacker not found. Please set path.", "OK");
                return;
            }

            if (!Directory.Exists(_sourceFolder))
            {
                EditorUtility.DisplayDialog("Error", $"Source folder not found: {_sourceFolder}", "OK");
                return;
            }

            if (!Directory.Exists(_outputFolder))
            {
                Directory.CreateDirectory(_outputFolder);
            }

            var args = BuildArguments();
            RunTexturePacker(args);

            AssetDatabase.Refresh();
            ConfigureImportSettings();

            EditorUtility.DisplayDialog("Complete", $"Atlas built: {_outputFolder}/{_atlasName}.png", "OK");
        }

        private void BuildAllAtlases()
        {
            // UI Atlas
            _sourceFolder = "Assets/Art/UI/Icons";
            _outputFolder = "Assets/Art/UI";
            _atlasName = "UIAtlas";
            BuildAtlas();

            // Unit Atlases by rarity
            var rarities = new[] { "Common", "Rare", "Epic", "Legendary", "Champion" };
            foreach (var rarity in rarities)
            {
                _sourceFolder = $"Assets/Art/Units/{rarity}";
                _outputFolder = $"Assets/Art/Units/UnitAtlases";
                _atlasName = $"UnitAtlas_{rarity}";
                if (Directory.Exists(_sourceFolder))
                {
                    BuildAtlas();
                }
            }

            // Building Atlas
            _sourceFolder = "Assets/Art/Buildings";
            _outputFolder = "Assets/Art/Buildings";
            _atlasName = "BuildingAtlas";
            if (Directory.Exists(_sourceFolder))
            {
                BuildAtlas();
            }

            // Spell Atlas
            _sourceFolder = "Assets/Art/Spells";
            _outputFolder = "Assets/Art/Spells";
            _atlasName = "SpellAtlas";
            if (Directory.Exists(_sourceFolder))
            {
                BuildAtlas();
            }

            AssetDatabase.Refresh();
        }

        private string BuildArguments()
        {
            var args = $"--format unity-texture2d ";
            args += $"--data \"{_outputFolder}/{_atlasName}.json\" ";
            args += $"--sheet \"{_outputFolder}/{_atlasName}.png\" ";
            args += $"--max-width {_maxSize} --max-height {_maxSize} ";
            args += $"--padding {_padding} ";
            args += $"--trim-mode {(_trimMode ? "Trim" : "None")} ";
            args += $"--allow-rotation {(_allowRotation ? "true" : "false")} ";
            args += $"--power-of-two {(_powerOfTwo ? "true" : "false")} ";
            args += $"--opt RGBA8888 ";
            args += $"\"{_sourceFolder}\"";

            return args;
        }

        private void RunTexturePacker(string args)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _texturePackerPath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                UnityEngine.Debug.LogError($"[AtlasBuilder] TexturePacker failed: {error}");
                throw new Exception($"TexturePacker failed: {error}");
            }

            UnityEngine.Debug.Log($"[AtlasBuilder] {output}");
        }

        private void ConfigureImportSettings()
        {
            var pngPath = $"{_outputFolder}/{_atlasName}.png";
            var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = _maxSize;
                importer.textureCompression = TextureImporterCompression.Compressed;
                
#if UNITY_IOS || UNITY_ANDROID
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    name = "Mobile",
                    overridden = true,
                    format = TextureImporterFormat.ASTC_4x4,
                    maxTextureSize = _maxSize,
                    compressionQuality = 50
                });
#else
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    name = "Standalone",
                    overridden = true,
                    format = TextureImporterFormat.DXT5,
                    maxTextureSize = _maxSize,
                    compressionQuality = 50
                });
#endif

                AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);
            }

            var jsonPath = $"{_outputFolder}/{_atlasName}.json";
            if (File.Exists(jsonPath))
            {
                var jsonImporter = AssetImporter.GetAtPath(jsonPath) as AssetImporter;
                // JSON doesn't need special import settings
            }
        }
    }
}