using System;
using System.Collections.Generic;
using UnityEngine;
using CRClone.Core;

namespace CRClone.Systems
{
    public class PoolManager : MonoBehaviour
    {
        private Dictionary<string, Queue<GameObject>> _pools = new();
        private Dictionary<string, GameObject> _prefabs = new();
        private Dictionary<GameObject, string> _objectToPool = new();
        private Transform _poolRoot;

        public void Initialize()
        {
            _poolRoot = new GameObject("PoolRoot").transform;
            _poolRoot.SetParent(transform);
            Debug.Log("[PoolManager] Initialized");
        }

        public void RegisterPrefab(string key, GameObject prefab, int prewarmCount = 0)
        {
            if (_prefabs.ContainsKey(key))
            {
                Debug.LogWarning($"[PoolManager] Prefab already registered: {key}");
                return;
            }

            _prefabs[key] = prefab;
            _pools[key] = new Queue<GameObject>();

            for (int i = 0; i < prewarmCount; i++)
            {
                var obj = CreateInstance(key);
                obj.SetActive(false);
                _pools[key].Enqueue(obj);
            }
        }

        public GameObject Spawn(string key, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (!_pools.ContainsKey(key))
            {
                Debug.LogError($"[PoolManager] Pool not registered: {key}");
                return null;
            }

            GameObject obj;
            if (_pools[key].Count > 0)
            {
                obj = _pools[key].Dequeue();
                obj.transform.SetPositionAndRotation(position, rotation);
                if (parent != null) obj.transform.SetParent(parent);
                obj.SetActive(true);
            }
            else
            {
                obj = CreateInstance(key, position, rotation, parent);
            }

            _objectToPool[obj] = key;

            // Call IPoolable.OnSpawn if implemented
            var poolables = obj.GetComponents<IPoolable>();
            foreach (var p in poolables) p.OnSpawn();

            return obj;
        }

        public GameObject Spawn(string key, Vector3 position) => Spawn(key, position, Quaternion.identity);
        public GameObject Spawn(string key) => Spawn(key, Vector3.zero, Quaternion.identity);

        private GameObject CreateInstance(string key, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
        {
            var prefab = _prefabs[key];
            GameObject obj;

            if (position == default && rotation == default)
            {
                obj = Instantiate(prefab, _poolRoot);
            }
            else
            {
                obj = Instantiate(prefab, position, rotation, parent ?? _poolRoot);
            }

            obj.name = key;
            return obj;
        }

        public void Despawn(GameObject obj)
        {
            if (obj == null) return;

            if (!_objectToPool.TryGetValue(obj, out string key))
            {
                Debug.LogWarning($"[PoolManager] Object not from pool: {obj.name}");
                Destroy(obj);
                return;
            }

            // Call IPoolable.OnDespawn
            var poolables = obj.GetComponents<IPoolable>();
            foreach (var p in poolables) p.OnDespawn();

            obj.SetActive(false);
            obj.transform.SetParent(_poolRoot);
            _pools[key].Enqueue(obj);
        }

        public void Despawn(string key, GameObject obj)
        {
            if (_objectToPool.TryGetValue(obj, out string actualKey) && actualKey == key)
            {
                Despawn(obj);
            }
            else
            {
                Debug.LogWarning($"[PoolManager] Key mismatch: expected {key}, got {actualKey}");
            }
        }

        public void ClearPool(string key)
        {
            if (_pools.TryGetValue(key, out var pool))
            {
                while (pool.Count > 0)
                {
                    var obj = pool.Dequeue();
                    _objectToPool.Remove(obj);
                    Destroy(obj);
                }
            }
        }

        public void ClearAllPools()
        {
            foreach (var kvp in _pools)
            {
                ClearPool(kvp.Key);
            }
            _objectToPool.Clear();
        }

        public int GetPoolSize(string key) => _pools.TryGetValue(key, out var pool) ? pool.Count : 0;
        public int GetActiveCount(string key) => _objectToPool.Count - GetPoolSize(key);
    }

    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
    }
}