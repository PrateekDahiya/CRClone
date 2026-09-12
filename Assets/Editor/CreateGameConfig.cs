using UnityEngine;
using CRClone.Data;

namespace CRClone.Editor
{
    public class CreateGameConfig
    {
        [MenuItem("CRClone/Asset Pipeline/Create GameConfig Asset")]
        public static void CreateConfig()
        {
            var config = ScriptableObject.CreateInstance<GameConfig>();
            var path = "Assets/Resources/Configs/GameConfig.asset";
            
            var dir = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"[CreateGameConfig] Created: {path}");
            EditorUtility.DisplayDialog("Complete", "GameConfig created at Assets/Resources/Configs/GameConfig.asset", "OK");
        }
    }
}