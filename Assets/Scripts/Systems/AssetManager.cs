using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using CRClone.Core;

namespace CRClone.Systems
{
    public class AssetManager : MonoBehaviour
    {
        private Dictionary<string, UnityEngine.Object> _loadedAssets = new();
        private Dictionary<string, AsyncOperationHandle> _loadingHandles = new();

        public void Initialize()
        {
            // Initialize Addressables if used
            // Addressables.InitializeAsync();
            Debug.Log("[AssetManager] Initialized");
        }

        public T Load<T>(string key) where T : UnityEngine.Object
        {
            if (_loadedAssets.TryGetValue(key, out var asset))
            {
                return asset as T;
            }

            // Try Resources first
            var resource = Resources.Load<T>(key);
            if (resource != null)
            {
                _loadedAssets[key] = resource;
                return resource;
            }

            Debug.LogWarning($"[AssetManager] Asset not found in Resources: {key}");
            return null;
        }

        public void LoadAsync<T>(string key, Action<T> callback) where T : UnityEngine.Object
        {
            if (_loadedAssets.TryGetValue(key, out var asset))
            {
                callback?.Invoke(asset as T);
                return;
            }

            // Using Resources.LoadAsync
            StartCoroutine(LoadAsyncCoroutine(key, callback));
        }

        private IEnumerator LoadAsyncCoroutine<T>(string key, Action<T> callback) where T : UnityEngine.Object
        {
            var request = Resources.LoadAsync<T>(key);
            yield return request;

            if (request.asset != null)
            {
                _loadedAssets[key] = request.asset;
                callback?.Invoke(request.asset as T);
            }
            else
            {
                callback?.Invoke(null);
            }
        }

        public void Preload(string[] keys)
        {
            foreach (var key in keys)
            {
                Load<UnityEngine.Object>(key);
            }
        }

        public void Release(string key)
        {
            if (_loadedAssets.TryGetValue(key, out var asset))
            {
                _loadedAssets.Remove(key);
                if (asset is GameObject)
                {
                    // Don't unload GameObjects from Resources
                }
                else
                {
                    Resources.UnloadAsset(asset);
                }
            }
        }

        public void ReleaseAll()
        {
            foreach (var asset in _loadedAssets.Values)
            {
                if (!(asset is GameObject))
                {
                    Resources.UnloadAsset(asset);
                }
            }
            _loadedAssets.Clear();
            Resources.UnloadUnusedAssets();
        }

        public Sprite LoadSprite(string key)
        {
            return Load<Sprite>(key);
        }

        public GameObject LoadPrefab(string key)
        {
            return Load<GameObject>($"Prefabs/{key}");
        }

        public RuntimeAnimatorController LoadAnimator(string key)
        {
            return Load<RuntimeAnimatorController>($"Animations/{key}");
        }

        public AudioClip LoadAudio(string key)
        {
            return Load<AudioClip>($"Audio/{key}");
        }

        // Addressables support (for production)
        public void LoadAddressable<T>(string key, Action<T> callback) where T : UnityEngine.Object
        {
            // var handle = Addressables.LoadAssetAsync<T>(key);
            // handle.Completed += (op) => callback?.Invoke(op.Result);
            // _loadingHandles[key] = handle;
        }

        public void ReleaseAddressable(string key)
        {
            // if (_loadingHandles.TryGetValue(key, out var handle))
            // {
            //     Addressables.Release(handle);
            //     _loadingHandles.Remove(key);
            // }
        }
    }
}