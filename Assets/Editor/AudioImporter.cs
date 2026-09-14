using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

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
            var importer = AssetImporter.GetAtPath(relativePath) as AudioImporter;
            
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

            if (_forceMonoForSFX && isSFX && !isVoice)
            {
                settings.conversionMode = AudioConversionMode.Mono;
            }
            else if (isMusic)
            {
                settings.conversionMode = AudioConversionMode.Stereo;
            }

            importer.forceToMono = _forceMonoForSFX && isSFX && !isVoice;
            importer.preloadAudioData = !isMusic;
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
            var mixerPath = "Assets/Audio/GameAudioMixer.mixer";
            
            if (File.Exists(mixerPath))
            {
                if (!EditorUtility.DisplayDialog("Exists", "AudioMixer already exists. Overwrite?", "Yes", "No"))
                    return;
            }

            var mixer = AudioMixer.CreateAudioMixer("GameAudioMixer");
            
            // Create groups
            var masterGroup = mixer.AddGroup("Master");
            var musicGroup = mixer.AddGroup("Music");
            var sfxGroup = mixer.AddGroup("SFX");
            var voiceGroup = mixer.AddGroup("Voice");

            // Set parent groups
            musicGroup.outputAudioMixerGroup = masterGroup;
            sfxGroup.outputAudioMixerGroup = masterGroup;
            voiceGroup.outputAudioMixerGroup = masterGroup;

            // Expose parameters
            mixer.ExposeParameter("MasterVolume");
            mixer.ExposeParameter("MusicVolume");
            mixer.ExposeParameter("SFXVolume");
            mixer.ExposeParameter("VoiceVolume");

            // Set default values
            masterGroup.audioMixer.SetFloat("MasterVolume", 0f);
            musicGroup.audioMixer.SetFloat("MusicVolume", -1.58f); // ~0.8
            sfxGroup.audioMixer.SetFloat("SFXVolume", 0f);
            voiceGroup.audioMixer.SetFloat("VoiceVolume", 0f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            _importResults.Add($"AudioMixer created: {mixerPath}");
            
            // Create snapshot for ducking
            CreateDuckingSnapshot(mixer);
        }

        private void CreateDuckingSnapshot(AudioMixer mixer)
        {
            var snapshot = mixer.AddSnapshot("Ducked");
            snapshot.TransitionTo(0.5f);
            
            // Lower music and SFX when voice plays
            mixer.SetFloat("MusicVolume", -20f); // -20dB
            mixer.SetFloat("SFXVolume", -10f);   // -10dB
            mixer.SetFloat("VoiceVolume", 0f);   // Full volume
        }
    }
}