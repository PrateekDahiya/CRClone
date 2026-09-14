using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CRClone.Editor
{
    public class AssetValidator : EditorWindow
    {
        private string _scanFolder = "Assets";
        private bool _checkNaming = true;
        private bool _checkDimensions = true;
        private bool _checkCompression = true;
        private bool _checkMissingRefs = true;
        private bool _checkMaxSize = true;
        private int _maxTextureSize = 2048;
        private bool _autoFix = false;
        private Vector2 _scrollPosition;
        private List<ValidationIssue> _issues = new();
        private bool _validationComplete = false;

        [MenuItem("CRClone/Asset Pipeline/Asset Validator")]
        public static void ShowWindow()
        {
            GetWindow<AssetValidator>("Asset Validator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Asset Validator", EditorStyles.boldLabel);
            GUILayout.Label("Validates assets on import and batch", EditorStyles.helpBox);

            EditorGUILayout.Space();
            _scanFolder = EditorGUILayout.TextField("Scan Folder", _scanFolder);
            if (GUILayout.Button("Browse"))
            {
                var path = EditorUtility.OpenFolderPanel("Select Folder", _scanFolder, "");
                if (!string.IsNullOrEmpty(path))
                {
                    _scanFolder = MakeRelativePath(path);
                }
            }

            EditorGUILayout.Space();
            GUILayout.Label("Validation Rules:", EditorStyles.boldLabel);
            _checkNaming = EditorGUILayout.Toggle("Naming Convention", _checkNaming);
            _checkDimensions = EditorGUILayout.Toggle("Power-of-2 Dimensions", _checkDimensions);
            _checkCompression = EditorGUILayout.Toggle("Compression Format", _checkCompression);
            _checkMissingRefs = EditorGUILayout.Toggle("Missing References", _checkMissingRefs);
            _checkMaxSize = EditorGUILayout.Toggle("Max Texture Size", _checkMaxSize);
            _maxTextureSize = EditorGUILayout.IntField("Max Size", _maxTextureSize);

            EditorGUILayout.Space();
            _autoFix = EditorGUILayout.Toggle("Auto-Fix (where possible)", _autoFix);

            EditorGUILayout.Space();
            if (GUILayout.Button("Run Validation", GUILayout.Height(40)))
            {
                RunValidation();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Validate Single Asset (Selection)", GUILayout.Height(30)))
            {
                ValidateSelection();
            }

            if (_validationComplete)
            {
                EditorGUILayout.Space();
                GUILayout.Label($"Issues Found: {_issues.Count}", EditorStyles.boldLabel);
                
                var errors = _issues.Count(i => i.severity == ValidationSeverity.Error);
                var warnings = _issues.Count(i => i.severity == ValidationSeverity.Warning);
                var info = _issues.Count(i => i.severity == ValidationSeverity.Info);
                
                EditorGUILayout.LabelField($"Errors: {errors} | Warnings: {warnings} | Info: {info}");

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300));
                foreach (var issue in _issues.OrderByDescending(i => i.severity))
                {
                    var color = issue.severity == ValidationSeverity.Error ? Color.red : 
                               issue.severity == ValidationSeverity.Warning ? Color.yellow : Color.white;
                    var style = new GUIStyle(EditorStyles.label) { normal = { textColor = color } };
                    EditorGUILayout.LabelField($"[{issue.severity}] {issue.assetPath}: {issue.message}", style);
                }
                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("Export Report"))
                {
                    ExportReport();
                }
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

        private void RunValidation()
        {
            _issues.Clear();
            _validationComplete = false;

            var assets = AssetDatabase.FindAssets("", new[] { _scanFolder });
            
            foreach (var guid in assets)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                ValidateAsset(path);
            }

            _validationComplete = true;
            Debug.Log($"[AssetValidator] Validation complete: {_issues.Count} issues found");
            Repaint();
        }

        private void ValidateSelection()
        {
            _issues.Clear();
            _validationComplete = false;

            foreach (var obj in Selection.objects)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                ValidateAsset(path);
            }

            _validationComplete = true;
            Repaint();
        }

        private void ValidateAsset(string path)
        {
            if (string.IsNullOrEmpty(path) || path.EndsWith(".cs") || path.EndsWith(".shader") || path.EndsWith(".json") || path.EndsWith(".md"))
                return;

            var importer = AssetImporter.GetAtPath(path);
            if (importer == null) return;

            // Check naming convention
            if (_checkNaming)
            {
                ValidateNaming(path);
            }

            // Check texture settings
            if (importer is TextureImporter textureImporter)
            {
                ValidateTexture(textureImporter, path);
            }

            // Check audio settings
            if (importer is UnityEditor.AudioImporter audioImporter)
            {
                ValidateAudio(audioImporter, path);
            }

            // Check model settings
            if (importer is ModelImporter modelImporter)
            {
                ValidateModel(modelImporter, path);
            }

            // Check for missing references in prefabs
            if (_checkMissingRefs && path.EndsWith(".prefab"))
            {
                ValidatePrefabReferences(path);
            }
        }

        private void ValidateNaming(string path)
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            
            // Check for spaces
            if (fileName.Contains(" "))
            {
                _issues.Add(new ValidationIssue
                {
                    assetPath = path,
                    message = "Filename contains spaces",
                    severity = ValidationSeverity.Warning,
                    autoFixable = true
                });
            }

            // Check for special characters
            if (Regex.IsMatch(fileName, @"[^a-zA-Z0-9_\-]"))
            {
                _issues.Add(new ValidationIssue
                {
                    assetPath = path,
                    message = "Filename contains special characters",
                    severity = ValidationSeverity.Warning,
                    autoFixable = true
                });
            }

            // Check prefix conventions
            var lowerName = fileName.ToLower();
            if (path.Contains("/Units/") && !lowerName.StartsWith("unit_"))
            {
                _issues.Add(new ValidationIssue
                {
                    assetPath = path,
                    message = "Unit assets should be prefixed with 'unit_'",
                    severity = ValidationSeverity.Info,
                    autoFixable = false
                });
            }
            if (path.Contains("/Buildings/") && !lowerName.StartsWith("building_"))
            {
                _issues.Add(new ValidationIssue
                {
                    assetPath = path,
                    message = "Building assets should be prefixed with 'building_'",
                    severity = ValidationSeverity.Info,
                    autoFixable = false
                });
            }
            if (path.Contains("/Spells/") && !lowerName.StartsWith("spell_"))
            {
                _issues.Add(new ValidationIssue
                {
                    assetPath = path,
                    message = "Spell assets should be prefixed with 'spell_'",
                    severity = ValidationSeverity.Info,
                    autoFixable = false
                });
            }
            if (path.Contains("/UI/") && !lowerName.StartsWith("ui_"))
            {
                _issues.Add(new ValidationIssue
                {
                    assetPath = path,
                    message = "UI assets should be prefixed with 'ui_'",
                    severity = ValidationSeverity.Info,
                    autoFixable = false
                });
            }
        }

        private void ValidateTexture(TextureImporter importer, string path)
        {
            // Check dimensions
            if (_checkDimensions)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null)
                {
                    if (!IsPowerOfTwo(texture.width) || !IsPowerOfTwo(texture.height))
                    {
                        _issues.Add(new ValidationIssue
                        {
                            assetPath = path,
                            message = $"Texture dimensions not power of 2: {texture.width}x{texture.height}",
                            severity = ValidationSeverity.Warning,
                            autoFixable = false
                        });
                    }

                    if (_checkMaxSize && (texture.width > _maxTextureSize || texture.height > _maxTextureSize))
                    {
                        _issues.Add(new ValidationIssue
                        {
                            assetPath = path,
                            message = $"Texture exceeds max size {_maxTextureSize}: {texture.width}x{texture.height}",
                            severity = ValidationSeverity.Error,
                            autoFixable = _autoFix
                        });
                    }
                }
            }

            // Check compression
            if (_checkCompression)
            {
                var settings = importer.GetPlatformTextureSettings("Standalone");
                
                var isMobile = path.Contains("/UI/") || path.Contains("/Units/");
                var expectedFormat = isMobile ? TextureImporterFormat.ASTC_4x4 : TextureImporterFormat.DXT5;
                
                if (settings.format != expectedFormat)
                {
                    _issues.Add(new ValidationIssue
                    {
                        assetPath = path,
                        message = $"Texture compression not optimal: {settings.format} (expected {expectedFormat})",
                        severity = ValidationSeverity.Warning,
                        autoFixable = _autoFix
                    });
                }
            }

            // Check mipmaps for UI
            if (path.Contains("/UI/") && importer.mipmapEnabled)
            {
                _issues.Add(new ValidationIssue
                {
                    assetPath = path,
                    message = "UI textures should have mipmaps disabled",
                    severity = ValidationSeverity.Warning,
                    autoFixable = _autoFix
                });
            }

            // Check filter mode for pixel art
            if (path.Contains("/UI/Icons/") && importer.filterMode != FilterMode.Point)
            {
                _issues.Add(new ValidationIssue
                {
                    assetPath = path,
                    message = "Icon textures should use Point filter mode",
                    severity = ValidationSeverity.Info,
                    autoFixable = _autoFix
                });
            }
        }

        private void ValidateAudio(UnityEditor.AudioImporter importer, string path)
        {
            var settings = new AudioImporterSampleSettings();
            importer.GetOverrideSampleSettings("Standalone", out settings);
            
            if (path.Contains("/Music/") && settings.loadType != AudioClipLoadType.Streaming)
            {
                _issues.Add(new ValidationIssue
                {
                    assetPath = path,
                    message = "Music should use Streaming load type",
                    severity = ValidationSeverity.Warning,
                    autoFixable = _autoFix
                });
            }

            if (path.Contains("/SFX/") && settings.loadType != AudioClipLoadType.CompressedInMemory)
            {
                _issues.Add(new ValidationIssue
                {
                    assetPath = path,
                    message = "SFX should use Compressed In Memory load type",
                    severity = ValidationSeverity.Info,
                    autoFixable = _autoFix
                });
            }

            if (path.Contains("/SFX/") && !settings.forceToMono)
            {
                _issues.Add(new ValidationIssue
                {
                    assetPath = path,
                    message = "SFX should be forced to mono",
                    severity = ValidationSeverity.Info,
                    autoFixable = _autoFix
                });
            }
        }

        private void ValidateModel(ModelImporter importer, string path)
        {
            if (importer.animationType == ModelImporterAnimationType.None)
            {
                _issues.Add(new ValidationIssue
                {
                    assetPath = path,
                    message = "Model has no animation type set",
                    severity = ValidationSeverity.Info,
                    autoFixable = false
                });
            }
        }

        private void ValidatePrefabReferences(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;

            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterial == null)
                {
                    _issues.Add(new ValidationIssue
                    {
                        assetPath = path,
                        message = $"Missing material on renderer: {renderer.name}",
                        severity = ValidationSeverity.Error,
                        autoFixable = false
                    });
                }
                else if (renderer.sharedMaterial.shader == null)
                {
                    _issues.Add(new ValidationIssue
                    {
                        assetPath = path,
                        message = $"Missing shader on material: {renderer.sharedMaterial.name}",
                        severity = ValidationSeverity.Error,
                        autoFixable = false
                    });
                }
            }

            var scripts = prefab.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var script in scripts)
            {
                if (script == null)
                {
                    _issues.Add(new ValidationIssue
                    {
                        assetPath = path,
                        message = "Missing script reference (deleted script)",
                        severity = ValidationSeverity.Error,
                        autoFixable = false
                    });
                }
            }
        }

        private bool IsPowerOfTwo(int value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }

        private void ExportReport()
        {
            var reportPath = "Assets/AssetValidationReport.txt";
            using (var writer = new StreamWriter(reportPath))
            {
                writer.WriteLine($"Asset Validation Report - {DateTime.Now}");
                writer.WriteLine($"Scan Folder: {_scanFolder}");
                writer.WriteLine($"Total Issues: {_issues.Count}");
                writer.WriteLine();

                foreach (var issue in _issues.OrderByDescending(i => i.severity))
                {
                    writer.WriteLine($"[{issue.severity}] {issue.assetPath}: {issue.message}");
                }
            }

            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(reportPath);
            Debug.Log($"[AssetValidator] Report exported to: {reportPath}");
        }

        public class ValidationIssue
        {
            public string assetPath;
            public string message;
            public ValidationSeverity severity;
            public bool autoFixable;
        }

        public enum ValidationSeverity
        {
            Info,
            Warning,
            Error
        }
    }

    // AssetPostprocessor for automatic validation on import
    public class AssetValidationPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var path in importedAssets)
            {
                ValidateOnImport(path);
            }
        }

        private static void ValidateOnImport(string path)
        {
            var importer = AssetImporter.GetAtPath(path);
            if (importer == null) return;

            var issues = new List<string>();

            if (importer is TextureImporter textureImporter)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null)
                {
                    if (!IsPowerOfTwo(texture.width) || !IsPowerOfTwo(texture.height))
                    {
                        issues.Add($"Non-power-of-2 texture: {texture.width}x{texture.height}");
                    }
                }
            }

            if (issues.Count > 0)
            {
                Debug.LogWarning($"[AssetValidator] Import validation for {path}: {string.Join(", ", issues)}");
            }
        }

        private static bool IsPowerOfTwo(int value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }
    }
}