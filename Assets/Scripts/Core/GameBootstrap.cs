using UnityEngine;
using CRClone.Core;
using CRClone.Data;

namespace CRClone.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Core Prefabs")]
        [SerializeField] private GameObject _gameManagerPrefab;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeSceneLoad()
        {
            // Ensure GameManager exists
            if (GameManager.Instance == null)
            {
                var gmPrefab = Resources.Load<GameObject>("Prefabs/GameManager");
                if (gmPrefab != null)
                {
                    Instantiate(gmPrefab);
                }
                else
                {
                    var go = new GameObject("GameManager");
                    go.AddComponent<GameManager>();
                }
            }
        }

        private void Awake()
        {
            // Register this as bootstrap
            Services.Register(this);
        }
    }
}