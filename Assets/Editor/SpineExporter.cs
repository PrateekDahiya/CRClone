using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CRClone.Editor
{
    public class SpineExporter : EditorWindow
    {
        private string _spineProjectPath = "Assets/Spine";
        private string _exportPath = "Assets/Spine/Exported";
        private string _spineExecutable = "";
        private bool _createSkeletonDataAssets = true;
        private bool _createAnimationClips = true;
        private float _mixDuration = 0.1f;
        private Vector2 _scrollPosition;
        private List<string> _exportResults = new();

        [MenuItem("CRClone/Asset Pipeline/Spine Exporter")]
        public static void ShowWindow()
        {
            GetWindow<SpineExporter>("Spine Exporter");
        }

        private void OnGUI()
        {
            GUILayout.Label("Spine Exporter", EditorStyles.boldLabel);
            GUILayout.Label("Exports Spine projects to Unity runtime format", EditorStyles.helpBox);

            EditorGUILayout.Space();
            _spineProjectPath = EditorGUILayout.TextField("Spine Project Folder", _spineProjectPath);
            _exportPath = EditorGUILayout.TextField("Export Output Folder", _exportPath);
            _spineExecutable = EditorGUILayout.TextField("Spine CLI Path", _spineExecutable);
            if (GUILayout.Button("Auto-Detect Spine"))
            {
                AutoDetectSpine();
            }

            EditorGUILayout.Space();
            _createSkeletonDataAssets = EditorGUILayout.Toggle("Create SkeletonDataAsset", _createSkeletonDataAssets);
            _createAnimationClips = EditorGUILayout.Toggle("Create Animation Clips", _createAnimationClips);
            _mixDuration = EditorGUILayout.FloatField("Default Mix Duration", _mixDuration);

            EditorGUILayout.Space();
            if (GUILayout.Button("Export All Spine Projects", GUILayout.Height(40)))
            {
                ExportAll();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Setup Spine Runtimes in Unity", GUILayout.Height(30)))
            {
                SetupSpineRuntimes();
            }

            if (_exportResults.Count > 0)
            {
                EditorGUILayout.Space();
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200));
                foreach (var result in _exportResults)
                {
                    EditorGUILayout.LabelField(result);
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void AutoDetectSpine()
        {
            var paths = new[]
            {
                @"C:\Program Files\Spine\Spine.exe",
                @"C:\Program Files (x86)\Spine\Spine.exe",
                "/Applications/Spine.app/Contents/MacOS/Spine",
                "/usr/bin/spine"
            };

            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    _spineExecutable = path;
                    Debug.Log($"[SpineExporter] Found Spine at: {path}");
                    return;
                }
            }

            EditorUtility.DisplayDialog("Not Found", "Spine not found in standard locations.", "OK");
        }

        private void ExportAll()
        {
            _exportResults.Clear();

            if (!Directory.Exists(_spineProjectPath))
            {
                EditorUtility.DisplayDialog("Error", $"Spine project folder not found: {_spineProjectPath}", "OK");
                return;
            }

            if (!Directory.Exists(_exportPath))
            {
                Directory.CreateDirectory(_exportPath);
            }

            // Find all .spine files
            var spineFiles = Directory.GetFiles(_spineProjectPath, "*.spine", SearchOption.AllDirectories);
            
            if (spineFiles.Length == 0)
            {
                _exportResults.Add("No .spine files found. Checking for .json/.skel exports...");
                ExportFromJson();
                return;
            }

            foreach (var spineFile in spineFiles)
            {
                ExportSpineFile(spineFile);
            }

            if (_createSkeletonDataAssets)
            {
                CreateSkeletonDataAssets();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Repaint();
        }

        private void ExportSpineFile(string spineFile)
        {
            var fileName = Path.GetFileNameWithoutExtension(spineFile);
            var outputDir = Path.Combine(_exportPath, fileName).Replace("\\", "/");
            
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            _exportResults.Add($"Exporting: {fileName}");

            if (!string.IsNullOrEmpty(_spineExecutable) && File.Exists(_spineExecutable))
            {
                var args = $"--input \"{spineFile}\" --output \"{outputDir}\" --format json";
                RunProcess(_spineExecutable, args);
            }
            else
            {
                _exportResults.Add($"  WARNING: Spine CLI not found, skipping export for {fileName}");
            }
        }

        private void ExportFromJson()
        {
            // Look for existing .json + .atlas + .png files
            var jsonFiles = Directory.GetFiles(_spineProjectPath, "*.json", SearchOption.AllDirectories);
            
            foreach (var jsonFile in jsonFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(jsonFile);
                var dir = Path.GetDirectoryName(jsonFile);
                
                var atlasFile = Path.Combine(dir, fileName + ".atlas").Replace("\\", "/");
                var pngFile = Path.Combine(dir, fileName + ".png").Replace("\\", "/");
                
                if (File.Exists(atlasFile) && File.Exists(pngFile))
                {
                    _exportResults.Add($"Found Spine export: {fileName}");
                    CopyToExportFolder(jsonFile, atlasFile, pngFile, fileName);
                }
            }
        }

        private void CopyToExportFolder(string jsonFile, string atlasFile, string pngFile, string fileName)
        {
            var outputDir = Path.Combine(_exportPath, fileName).Replace("\\", "/");
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            File.Copy(jsonFile, Path.Combine(outputDir, fileName + ".json"), true);
            File.Copy(atlasFile, Path.Combine(outputDir, fileName + ".atlas"), true);
            File.Copy(pngFile, Path.Combine(outputDir, fileName + ".png"), true);

            _exportResults.Add($"  Copied to: {outputDir}");
        }

        private void CreateSkeletonDataAssets()
        {
            // This requires Spine-Unity runtime package
            // We'll create placeholder setup instructions
            _exportResults.Add("");
            _exportResults.Add("=== Spine-Unity Setup Required ===");
            _exportResults.Add("1. Install Spine-Unity package from Esoteric Software");
            _exportResults.Add("2. Use SkeletonDataAsset.CreateFromFiles() API");
            _exportResults.Add("3. Or use SpineEditorUtilities.ImportSpineProject()");
            
            var setupScript = @"
// Spine Setup Script (run after installing Spine-Unity)
using Spine.Unity;
using Spine.Unity.Editor;
using UnityEditor;
using UnityEngine;

public class SpineSetup
{
    [MenuItem(""CRClone/Spine/Create SkeletonDataAssets"")]
    public static void CreateAssets()
    {
        var exportPath = ""Assets/Spine/Exported"";
        var dirs = Directory.GetDirectories(exportPath);
        
        foreach (var dir in dirs)
        {
            var name = Path.GetFileName(dir);
            var jsonFile = Path.Combine(dir, name + "".json"");
            var atlasFile = Path.Combine(dir, name + "".atlas"");
            var pngFile = Path.Combine(dir, name + "".png"");
            
            if (File.Exists(jsonFile) && File.Exists(atlasFile) && File.Exists(pngFile))
            {
                var atlasAsset = new AtlasAsset();
                atlasAsset.atlasFile = new TextAsset(File.ReadAllText(atlasFile));
                atlasAsset.materials = new[] { new Material(Shader.Find(""Spine/Skeleton"")) };
                
                var skeletonDataAsset = SkeletonDataAsset.CreateFromFiles(
                    jsonFile.Replace(""Assets/"", """"),
                    new[] { atlasAsset },
                    true
                );
                
                var assetPath = $""Assets/Spine/SkeletonData/{name}_SkeletonData.asset"";
                Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
                AssetDatabase.CreateAsset(skeletonDataAsset, assetPath);
                
                // Set mix durations
                var data = skeletonDataAsset.GetSkeletonData(true);
                SetMixDurations(data);
                
                AssetDatabase.SaveAssets();
            }
        }
    }

    static void SetMixDurations(Spine.SkeletonData data)
    {
        var states = new[] { ""idle"", ""walk"", ""attack"", ""hit"", ""death"", ""spawn"" };
        foreach (var from in states)
        {
            foreach (var to in states)
            {
                if (from != to && data.FindAnimation(from) != null && data.FindAnimation(to) != null)
                {
                    data.SetMix(from, to, 0.1f);
                }
            }
        }
    }
}
";

            var setupPath = "Assets/Editor/SpineSetup.cs";
            File.WriteAllText(setupPath, setupScript);
            _exportResults.Add($"Setup script written to: {setupPath}");
        }

        private void SetupSpineRuntimes()
        {
            var instructions = @"
# Spine-Unity Runtime Setup

## Installation
1. Download Spine-Unity runtime from: https://github.com/EsotericSoftware/spine-runtimes
2. Copy `spine-unity` folder to `Assets/Plugins/Spine/`
3. Or install via UPM: `com.esotericsoftware.spine-unity`

## Required Components per Unit Prefab:
- SkeletonAnimation (or SkeletonGraphic for UI)
- MeshRenderer / SkinnedMeshRenderer
- AnimationReferenceAsset (for each animation)

## Animation State Machine Setup:
1. Create AnimationState for each unit
2. Set mix durations (0.1s idle<->walk, 0.05s attack->idle)
3. Add events: attack_frame, hit_frame, death_complete

## Skin System for Team Colors:
1. Create base skin in Spine
2. Use shader-based tinting (UnitShader) instead of duplicate skins
3. Set material property _TeamColor (blue/red)

## Spine Events Mapping:
- ""attack"" -> UnitView.OnAttackFrame()
- ""hit"" -> UnitView.OnHitFrame()
- ""death_end"" -> UnitView.OnDeathComplete()
- ""spawn_end"" -> UnitView.OnSpawnComplete()
- ""footstep"" -> Play footstep sound
";

            var path = "Assets/Spine/SPINE_SETUP.md";
            File.WriteAllText(path, instructions);
            _exportResults.Add($"Setup guide written to: {path}");
            
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Spine Setup", "Setup guide created. Install Spine-Unity runtime first.", "OK");
        }

        private void RunProcess(string exe, string args)
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exe,
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
                _exportResults.Add($"  ERROR: {error}");
            }
            else
            {
                _exportResults.Add($"  Success: {output}");
            }
        }
    }
}