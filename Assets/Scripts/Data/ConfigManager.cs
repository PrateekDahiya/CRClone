using System;
using UnityEngine;
using CRClone.Data;
using CRClone.Core;

namespace CRClone.Data
{
    public class ConfigManager : MonoBehaviour
    {
        private GameConfig _config;

        public void Initialize(GameConfig config)
        {
            _config = config;
            if (_config == null)
            {
                _config = Resources.Load<GameConfig>("Configs/GameConfig");
            }

            if (_config == null)
            {
                Debug.LogError("[ConfigManager] No GameConfig found! Creating default.");
                _config = ScriptableObject.CreateInstance<GameConfig>();
            }

            Debug.Log("[ConfigManager] Initialized");
        }

        public GameConfig GetConfig() => _config;

        public T GetConfigValue<T>(string key, T defaultValue)
        {
            // Could load from remote config / server
            return defaultValue;
        }
    }
}