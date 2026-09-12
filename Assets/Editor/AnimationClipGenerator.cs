using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CRClone.Editor
{
    public class AnimationClipGenerator : EditorWindow
    {
        private string _sourceFolder = "Assets/Art/Units/Common/Knight";
        private string _outputFolder = "Assets/Animations/Units";
        private int _frameRate = 12;
        private WrapMode _wrapMode = WrapMode.Loop;
        private bool _generateForAllStates = true;
        private string[] _animationStates = new[] { "Idle", "Walk", "Attack", "Hit", "Death", "Spawn" };
        private Dictionary<string, bool> _stateEnabled = new();
        private Vector2 _scrollPosition;

        [MenuItem("CRClone/Asset Pipeline/Animation Clip Generator")]
        public static void ShowWindow()
        {
            GetWindow<AnimationClipGenerator>("Animation Clip Generator");
        }

        private void OnEnable()
        {
            foreach (var state in _animationStates)
            {
                _stateEnabled[state] = true;
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Animation Clip Generator", EditorStyles.boldLabel);
            GUILayout.Label("Generates AnimationClips from frame sequences (PNG)", EditorStyles.helpBox);

            EditorGUILayout.Space();
            _sourceFolder = EditorGUILayout.TextField("Source Folder", _sourceFolder);
            if (GUILayout.Button("Browse"))
            {
                var path = EditorUtility.OpenFolderPanel("Select Source Folder", _sourceFolder, "");
                if (!string.IsNullOrEmpty(path))
                {
                    _sourceFolder = MakeRelativePath(path);
                }
            }

            _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);
            _frameRate = EditorGUILayout.IntField("Frame Rate (FPS)", _frameRate);
            _wrapMode = (WrapMode)EditorGUILayout.EnumPopup("Wrap Mode", _wrapMode);

            EditorGUILayout.Space();
            _generateForAllStates = EditorGUILayout.Toggle("Generate for All States", _generateForAllStates);

            if (_generateForAllStates)
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(150));
                foreach (var state in _animationStates)
                {
                    _stateEnabled[state] = EditorGUILayout.Toggle(state, _stateEnabled[state]);
                }
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate Animation Clips", GUILayout.Height(40)))
            {
                GenerateClips();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate for All Units in Folder", GUILayout.Height(30)))
            {
                GenerateForAllUnits();
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

        private void GenerateClips()
        {
            if (!Directory.Exists(_sourceFolder))
            {
                EditorUtility.DisplayDialog("Error", $"Source folder not found: {_sourceFolder}", "OK");
                return;
            }

            if (!Directory.Exists(_outputFolder))
            {
                Directory.CreateDirectory(_outputFolder);
            }

            var unitName = Path.GetFileName(_sourceFolder);
            var statesToGenerate = _generateForAllStates 
                ? _animationStates.Where(s => _stateEnabled[s]).ToArray()
                : _animationStates;

            int generated = 0;
            foreach (var state in statesToGenerate)
            {
                var stateFolder = Path.Combine(_sourceFolder, state).Replace("\\", "/");
                if (Directory.Exists(stateFolder))
                {
                    var clip = CreateAnimationClip(stateFolder, unitName, state);
                    if (clip != null)
                    {
                        generated++;
                    }
                }
                else
                {
                    Debug.LogWarning($"[AnimationClipGenerator] State folder not found: {stateFolder}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Complete", $"Generated {generated} animation clips for {unitName}", "OK");
        }

        private void GenerateForAllUnits()
        {
            var baseFolder = Path.GetDirectoryName(_sourceFolder).Replace("\\", "/");
            if (!Directory.Exists(baseFolder))
            {
                EditorUtility.DisplayDialog("Error", $"Base folder not found: {baseFolder}", "OK");
                return;
            }

            var unitFolders = Directory.GetDirectories(baseFolder);
            int totalClips = 0;

            foreach (var unitFolder in unitFolders)
            {
                var unitName = Path.GetFileName(unitFolder);
                _sourceFolder = unitFolder.Replace("\\", "/");
                
                var statesToGenerate = _animationStates.Where(s => _stateEnabled[s]).ToArray();
                foreach (var state in statesToGenerate)
                {
                    var stateFolder = Path.Combine(unitFolder, state).Replace("\\", "/");
                    if (Directory.Exists(stateFolder))
                    {
                        var clip = CreateAnimationClip(stateFolder, unitName, state);
                        if (clip != null) totalClips++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Complete", $"Generated {totalClips} animation clips for {unitFolders.Length} units", "OK");
        }

        private AnimationClip CreateAnimationClip(string frameFolder, string unitName, string stateName)
        {
            var frameFiles = Directory.GetFiles(frameFolder, "*.png")
                .OrderBy(f => f, new NaturalStringComparer())
                .ToArray();

            if (frameFiles.Length == 0)
            {
                Debug.LogWarning($"[AnimationClipGenerator] No frames found in: {frameFolder}");
                return null;
            }

            var clip = new AnimationClip();
            clip.frameRate = _frameRate;
            clip.wrapMode = _wrapMode;
            clip.name = $"{unitName}_{stateName}";

            var spriteBindings = new List<EditorCurveBinding>();
            var spriteKeyframes = new List<ObjectReferenceKeyframe>();

            for (int i = 0; i < frameFiles.Length; i++)
            {
                var framePath = frameFiles[i].Replace("\\", "/");
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(framePath);
                if (sprite == null)
                {
                    // Try to import as sprite
                    var importer = AssetImporter.GetAtPath(framePath) as TextureImporter;
                    if (importer != null)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.filterMode = FilterMode.Bilinear;
                        AssetDatabase.ImportAsset(framePath);
                        sprite = AssetDatabase.LoadAssetAtPath<Sprite>(framePath);
                    }
                }

                if (sprite != null)
                {
                    var time = (float)i / _frameRate;
                    spriteKeyframes.Add(new ObjectReferenceKeyframe { time = time, value = sprite });
                }
            }

            if (spriteKeyframes.Count == 0)
            {
                Debug.LogWarning($"[AnimationClipGenerator] No valid sprites for: {frameFolder}");
                return null;
            }

            var binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            AnimationUtility.SetObjectReferenceCurve(clip, binding, spriteKeyframes.ToArray());

            // Add events for attack/hit frames
            if (stateName == "Attack" || stateName == "Hit")
            {
                AddAnimationEvents(clip, stateName, frameFiles.Length);
            }

            var outputPath = Path.Combine(_outputFolder, unitName, $"{unitName}_{stateName}.anim").Replace("\\", "/");
            var dir = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            AssetDatabase.CreateAsset(clip, outputPath);
            Debug.Log($"[AnimationClipGenerator] Created: {outputPath} ({frameFiles.Length} frames)");

            return clip;
        }

        private void AddAnimationEvents(AnimationClip clip, string stateName, int frameCount)
        {
            var events = new List<AnimationEvent>();

            if (stateName == "Attack")
            {
                // Attack event at ~60% of animation
                var attackFrame = Mathf.RoundToInt(frameCount * 0.6f);
                var attackTime = attackFrame / (float)clip.frameRate;
                events.Add(new AnimationEvent
                {
                    time = attackTime,
                    functionName = "OnAttackFrame",
                    messageOptions = SendMessageOptions.DontRequireReceiver
                });
            }
            else if (stateName == "Hit")
            {
                // Hit event at ~30% of animation
                var hitFrame = Mathf.RoundToInt(frameCount * 0.3f);
                var hitTime = hitFrame / (float)clip.frameRate;
                events.Add(new AnimationEvent
                {
                    time = hitTime,
                    functionName = "OnHitFrame",
                    messageOptions = SendMessageOptions.DontRequireReceiver
                });
            }

            // Death event at end
            if (stateName == "Death")
            {
                var deathTime = frameCount / (float)clip.frameRate;
                events.Add(new AnimationEvent
                {
                    time = deathTime,
                    functionName = "OnDeathComplete",
                    messageOptions = SendMessageOptions.DontRequireReceiver
                });
            }

            clip.events = events.ToArray();
        }

        private class NaturalStringComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                // Extract numbers from filenames for natural sorting
                var rx = System.Text.RegularExpressions.Regex.Match(x, @"(\d+)");
                var ry = System.Text.RegularExpressions.Regex.Match(y, @"(\d+)");
                
                if (rx.Success && ry.Success)
                {
                    int nx = int.Parse(rx.Groups[1].Value);
                    int ny = int.Parse(ry.Groups[1].Value);
                    return nx.CompareTo(ny);
                }
                return string.Compare(x, y, StringComparison.Ordinal);
            }
        }
    }
}