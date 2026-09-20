using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace CRClone.Editor
{
    public class AudioImporter : EditorWindow
    {
        private string _sourceFolder = "Assets/Audio";
        private float _targetLUFS = -18f;
        private float _targetPeak = -1f;
        private bool _forceMonoForSFX = true;
        private bool _normalizeOnImport = true;
        private AudioCompressionFormat _compressionFormat = AudioCompressionFormat.Vorbis;
        private int _quality = 70;
        private Vector2 _scrollPosition;
        private List<string> _importResults = new();

        [MenuItem("CRClone/Asset Pipeline/Audio Importer")]
        public static void ShowWindow()
        {
            GetWindow<AudioImporter>("Audio Importer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Audio Importer & Normalizer", EditorStyles.boldLabel);
            GUILayout.Label("Batch processes audio files for optimal game settings", EditorStyles.helpBox);

            EditorGUILayout.Space();
            _sourceFolder = EditorGUILayout.TextField("Source Folder", _sourceFolder);
            if (GUILayout.Button("Browse"))
            {
                var path = EditorUtility.OpenFolderPanel("Select Audio Folder", _sourceFolder, "");
                if (!string.IsNullOrEmpty(path))
                {
                    _sourceFolder = MakeRelativePath(path);
                }
            }

            EditorGUILayout.Space();
            GUILayout.Label("Normalization Settings:", EditorStyles.boldLabel);
            _targetLUFS = EditorGUILayout.Slider("Target LUFS", _targetLUFS, -24f, -12f);
            _targetPeak = EditorGUILayout.Slider("Target Peak (dB)", _targetPeak, -3f, 0f);
            _forceMonoForSFX = EditorGUILayout.Toggle("Force Mono for SFX", _forceMonoForSFX);
            _normalizeOnImport = EditorGUILayout.Toggle("Auto-Normalize on Import", _normalizeOnImport);

            EditorGUILayout.Space();
            GUILayout.Label("Compression Settings:", EditorStyles.boldLabel);
            _compressionFormat = (AudioCompressionFormat)EditorGUILayout.EnumPopup("Format", _compressionFormat);
            _quality = EditorGUILayout.IntSlider("Quality", _quality, 1, 100);

            EditorGUILayout.Space();
            if (GUILayout.Button("Process All Audio", GUILayout.Height(40)))
            {
                ProcessAllAudio();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Register Clips in AudioManager", GUILayout.Height(30)))
            {
                RegisterClipsInAudioManager();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Create AudioMixer Asset", GUILayout.Height(30)))
            {
                CreateAudioMixer();
            }

            if (_importResults.Count > 0)
            {
                EditorGUILayout.Space();
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200));
                foreach (var result in _importResults)
                {
                    EditorGUILayout.LabelField(result);
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

        private void ProcessAllAudio()
        {
            _importResults.Clear();

            if (!Directory.Exists(_sourceFolder))
            {
                EditorUtility.DisplayDialog("Error", $"Source folder not found: {_sourceFolder}", "OK");
                return;
            }

            var audioFiles = Directory.GetFiles(_sourceFolder, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (var file in audioFiles)
            {
                ProcessAudioFile(file);
            }

            AssetDatabase.Refresh();
            Repaint();
            EditorUtility.DisplayDialog("Complete", $"Processed {audioFiles.Length} audio files", "OK");
        }

        private void ProcessAudioFile(string filePath)
        {
            var relativePath = MakeRelativePath(filePath);
            var importer = AssetImporter.GetAtPath(relativePath) as UnityEditor.AudioImporter;
            
            if (importer == null)
            {
                _importResults.Add($"SKIP: {relativePath} (no importer)");
                return;
            }

            bool isMusic = relativePath.Contains("/Music/");
            bool isSFX = relativePath.Contains("/SFX/") || relativePath.Contains("/Voice/") || relativePath.Contains("/Announcer/");
            bool isVoice = relativePath.Contains("/Voice/");

            var settings = new AudioImporterSampleSettings
            {
                loadType = isMusic ? AudioClipLoadType.Streaming : AudioClipLoadType.CompressedInMemory,
                compressionFormat = _compressionFormat,
                quality = _quality,
                sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate
            };

            importer.forceToMono = _forceMonoForSFX && isSFX && !isVoice;
            settings.preloadAudioData = !isMusic;
            importer.SetOverrideSampleSettings("Standalone", settings);
            importer.SetOverrideSampleSettings("iPhone", settings);
            importer.SetOverrideSampleSettings("Android", settings);

            AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);

            // Note: Actual loudness normalization would require external tools (ffmpeg, sox)
            // Unity doesn't have built-in LUFS normalization
            _importResults.Add($"OK: {relativePath} ({ (isMusic ? "Music" : isVoice ? "Voice" : "SFX") })");
        }

        private void RegisterClipsInAudioManager()
        {
            // Generate a script that registers all audio clips
            var script = @"
using UnityEngine;
using CRClone.Systems;

public static class AudioClipRegistry
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegisterAllClips()
    {
        var manager = AudioManager.Instance;
        if (manager == null) return;

        // Music
";

            var musicFiles = Directory.GetFiles("Assets/Audio/Music", "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".wav") || f.EndsWith(".ogg") || f.EndsWith(".mp3"))
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .ToArray();

            foreach (var clip in musicFiles)
            {
                script += $@"
        manager.RegisterClip(""{clip}"", Resources.Load<AudioClip>(""Audio/Music/{clip}""));";
            }

            script += @"
        // SFX
";

            var sfxFolders = new[] { "Units", "Spells", "Buildings", "Towers", "UI", "Announcer" };
            foreach (var folder in sfxFolders)
            {
                var folderPath = $"Assets/Audio/SFX/{folder}";
                if (Directory.Exists(folderPath))
                {
                    var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                        .Where(f => f.EndsWith(".wav") || f.EndsWith(".ogg") || f.EndsWith(".mp3"))
                        .Select(f => Path.GetFileNameWithoutExtension(f))
                        .ToArray();

                    foreach (var clip in files)
                    {
                        script += $@"
        manager.RegisterClip(""{clip}"", Resources.Load<AudioClip>(""Audio/SFX/{folder}/{clip}""));";
                    }
                }
            }

            script += @"
        // Voice
";

            var voiceFiles = Directory.GetFiles("Assets/Audio/Voice", "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".wav") || f.EndsWith(".ogg") || f.EndsWith(".mp3"))
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .ToArray();

            foreach (var clip in voiceFiles)
            {
                script += $@"
        manager.RegisterClip(""{clip}"", Resources.Load<AudioClip>(""Audio/Voice/{clip}""));";
            }

            script += @"
    }
}
";

            var outputPath = "Assets/Scripts/Systems/AudioClipRegistry.cs";
            File.WriteAllText(outputPath, script);
            AssetDatabase.Refresh();
            _importResults.Add($"Registry script created: {outputPath}");
        }

        private void CreateAudioMixer()
        {
            // Unity provides no public API to create AudioMixer assets in code
            // (AudioMixer.CreateAudioMixer / AddGroup / ExposeParameter do not exist).
            // Author 'Assets/Audio/GameAudioMixer.mixer' manually with Master/Music/SFX/Voice
            // groups and MasterVolume/MusicVolume/SFXVolume/VoiceVolume exposed parameters.
            UnityEngine.Debug.LogWarning("[AudioImporter] CreateAudioMixer skipped: no public Unity API to create AudioMixer assets. Create 'Assets/Audio/GameAudioMixer.mixer' manually.");
            _importResults.Add("SKIP: CreateAudioMixer not supported by Unity API - create mixer manually (see log).");
        }

        private void CreateDuckingSnapshot(AudioMixer mixer)
        {
            // No public API (AudioMixer.AddSnapshot does not exist). Graceful skip.
            UnityEngine.Debug.LogWarning("[AudioImporter] CreateDuckingSnapshot skipped: no public Unity API.");
        }
    }
}