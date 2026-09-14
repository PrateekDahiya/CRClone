using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using CRClone.Data;
using CRClone.Core;
using CRClone.Network;
using CRClone.Battle.Simulation;
using CRClone.Battle.Presentation;
using CRClone.UI;
using CRClone.Systems;

namespace CRClone.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private GameConfig _gameConfig;
        [SerializeField] private bool _autoStart = true;
        [SerializeField] private GameState _initialState = GameState.Boot;

        // Core Services
        public NetworkClient Network { get; private set; }
        public DataManager Data { get; private set; }
        public AssetManager Assets { get; private set; }
        public AudioManager Audio { get; private set; }
        public PoolManager Pool { get; private set; }
        public ConfigManager Config { get; private set; }
        public BattleSimulation BattleSim { get; private set; }
        public BattleView BattleView { get; private set; }
        public UIManager UIManager { get; private set; }

        // State
        public GameState CurrentState { get; private set; }
        public PlayerLocalData LocalPlayer { get; private set; }
        public BattleData CurrentBattle { get; private set; }

        // Scene Management
        private AsyncOperation _sceneLoadOp;
        private GameState _targetStateAfterLoad;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeServices();
        }

        private void Start()
        {
            if (_autoStart)
            {
                ChangeState(_initialState);
            }
        }

        private void InitializeServices()
        {
            // Create service instances
            var networkGO = new GameObject("NetworkClient");
            networkGO.transform.SetParent(transform);
            Network = networkGO.AddComponent<NetworkClient>();

            var dataGO = new GameObject("DataManager");
            dataGO.transform.SetParent(transform);
            Data = dataGO.AddComponent<DataManager>();

            var assetGO = new GameObject("AssetManager");
            assetGO.transform.SetParent(transform);
            Assets = assetGO.AddComponent<AssetManager>();

            var audioGO = new GameObject("AudioManager");
            audioGO.transform.SetParent(transform);
            Audio = audioGO.AddComponent<AudioManager>();

            var poolGO = new GameObject("PoolManager");
            poolGO.transform.SetParent(transform);
            Pool = poolGO.AddComponent<PoolManager>();

            var configGO = new GameObject("ConfigManager");
            configGO.transform.SetParent(transform);
            Config = configGO.AddComponent<ConfigManager>();

            var uiGO = new GameObject("UIManager");
            uiGO.transform.SetParent(transform);
            UIManager = uiGO.AddComponent<UIManager>();

            // Register with ServiceLocator
            Services.Register(this);
            Services.Register(Network);
            Services.Register(Data);
            Services.Register(Assets);
            Services.Register(Audio);
            Services.Register(Pool);
            Services.Register(Config);
            Services.Register(UIManager);

            // Initialize configs
            Config.Initialize(_gameConfig);
            Data.Initialize();
            Pool.Initialize();
            Audio.Initialize();
        }

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;

            var previousState = CurrentState;
            CurrentState = newState;

            Debug.Log($"[GameManager] State changed: {previousState} -> {newState}");

            // Handle state transitions
            HandleStateExit(previousState);
            HandleStateEnter(newState);

            EventBus.Raise(newState);
        }

        private void HandleStateExit(GameState state)
        {
            switch (state)
            {
                case GameState.Battle:
                    CleanupBattle();
                    break;
                case GameState.Lobby:
                    // Keep lobby data
                    break;
            }
        }

        private void HandleStateEnter(GameState state)
        {
            switch (state)
            {
                case GameState.Boot:
                    LoadSceneAsync(GameScene.MainMenu, GameState.MainMenu);
                    break;
                case GameState.MainMenu:
                    LoadSceneAsync(GameScene.MainMenu);
                    break;
                case GameState.Lobby:
                    LoadSceneAsync(GameScene.Lobby);
                    break;
                case GameState.DeckBuilder:
                    LoadSceneAsync(GameScene.DeckBuilder);
                    break;
                case GameState.BattleLoading:
                    LoadSceneAsync(GameScene.Battle, GameState.Battle);
                    break;
                case GameState.Battle:
                    InitializeBattle();
                    break;
                case GameState.Shop:
                    LoadSceneAsync(GameScene.Shop);
                    break;
                case GameState.Clan:
                    LoadSceneAsync(GameScene.Clan);
                    break;
                case GameState.Profile:
                    LoadSceneAsync(GameScene.Profile);
                    break;
            }
        }

        private void LoadSceneAsync(GameScene scene, GameState? stateAfterLoad = null)
        {
            if (_sceneLoadOp != null) return;

            _targetStateAfterLoad = stateAfterLoad ?? CurrentState;
            string sceneName = scene.ToString();

            StartCoroutine(LoadSceneCoroutine(sceneName));
        }

        private IEnumerator LoadSceneCoroutine(string sceneName)
        {
            EventBus.RaiseLoadingProgress(0f);
            _sceneLoadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

            while (!_sceneLoadOp.isDone)
            {
                EventBus.RaiseLoadingProgress(_sceneLoadOp.progress);
                yield return null;
            }

            EventBus.RaiseLoadingProgress(1f);
            _sceneLoadOp = null;

            // Find BattleView if in battle scene
            if (SceneManager.GetActiveScene().name == "Battle")
            {
                BattleView = FindObjectOfType<BattleView>();
            }

            OnSceneLoaded();
        }

        private void OnSceneLoaded()
        {
            // Scene-specific initialization
            switch (CurrentState)
            {
                case GameState.Battle:
                    if (BattleView != null && BattleSim != null)
                    {
                        BattleView.Initialize(BattleSim);
                    }
                    break;
            }
        }

        private void InitializeBattle()
        {
            BattleSim = new BattleSimulation();
            BattleSim.Initialize(Config.GetConfig(), CurrentBattle.seed, CurrentBattle.player1.deck.cardIds, CurrentBattle.player2.deck.cardIds);
            
            if (BattleView != null)
            {
                BattleView.Initialize(BattleSim);
            }
        }

        private void CleanupBattle()
        {
            if (BattleSim != null)
            {
                BattleSim.Dispose();
                BattleSim = null;
            }
            BattleView = null;
            CurrentBattle = null;
        }

        public void StartBattle(BattleData battleData)
        {
            CurrentBattle = battleData;
            ChangeState(GameState.BattleLoading);
        }

        public void EndBattle(BattleResult result)
        {
            // Save replay, update player data, show result screen
            Data.SaveBattleResult(result);
            ChangeState(GameState.BattleResult);
        }

        public void ReturnToLobby()
        {
            ChangeState(GameState.Lobby);
        }

        // Additive compat: ReconnectionManager:159 calls
        // `Services.Get<GameManager>()?.LoadScene(GameManager.GameScene.MainMenu, null)`.
        // Namespace-level CRClone.Core.GameScene (GameTypes.cs) already exists with the same
        // members; this nested mirror satisfies the `GameManager.GameScene` qualification
        // without touching the consumer.
        public enum GameScene { Boot, MainMenu, Lobby, DeckBuilder, Battle, Shop, Clan, Profile }

        public void LoadScene(GameScene scene, Action onComplete)
        {
            LoadSceneAsync(scene);
            // TODO: onComplete fires immediately, not after async load completes.
            // Wire to OnSceneLoaded() when a completion hook exists.
            onComplete?.Invoke();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            Services.Clear();
        }
    }

    // Supporting data classes
    [Serializable]
    public class PlayerLocalData : PlayerData
    {
        public long playerId;
        public string username;
        public int avatarId;
        public string nameColor;
        public DeckData activeDeck;
        public PlayerSettings settings;
    }

    [Serializable]
    public class CardCollectionEntry
    {
        public int cardId;
        public int count;
        public int level;
        public int upgradeProgress;

        // Numeric interop (count-based): allows `collection[id] >= n`, `> n`,
        // `int x = collection[id]`, `collection[id] = 0`, `+= n`, `-= n`.
        public static implicit operator int(CardCollectionEntry e) => e != null ? e.count : 0;
        public static implicit operator CardCollectionEntry(int count) => new CardCollectionEntry { count = count };
        public static bool operator >=(CardCollectionEntry a, int b) => (a != null ? a.count : 0) >= b;
        public static bool operator <=(CardCollectionEntry a, int b) => (a != null ? a.count : 0) <= b;
        public static bool operator >(CardCollectionEntry a, int b) => (a != null ? a.count : 0) > b;
        public static bool operator <(CardCollectionEntry a, int b) => (a != null ? a.count : 0) < b;
        public static CardCollectionEntry operator +(CardCollectionEntry a, int b)
        {
            if (a == null) return new CardCollectionEntry { count = b };
            return new CardCollectionEntry { cardId = a.cardId, count = a.count + b, level = a.level, upgradeProgress = a.upgradeProgress };
        }
        public static CardCollectionEntry operator -(CardCollectionEntry a, int b)
        {
            if (a == null) return new CardCollectionEntry { count = -b };
            return new CardCollectionEntry { cardId = a.cardId, count = a.count - b, level = a.level, upgradeProgress = a.upgradeProgress };
        }
    }

    [Serializable]
    public class DeckData
    {
        public long deckId;
        public string name;
        public int[] cardIds = new int[8];
        public float avgElixir;
        public bool hasChampion;
    }

    [Serializable]
    public class PlayerSettings
    {
        public GraphicsQuality graphicsQuality = GraphicsQuality.High;
        public int frameRateCap = 60;
        public bool vsync = true;
        public bool showDamageNumbers = true;
        public bool cameraShake = true;
        public int masterVolume = 100;
        public int musicVolume = 80;
        public int sfxVolume = 100;
        public int voiceVolume = 100;
        public bool muteOnFocusLoss = true;
        public DeployMode deployMode = DeployMode.Both;
        public bool autoTarget = true;
        public bool leftHandedMode = false;
    }

    public enum GraphicsQuality { Low, Medium, High, Ultra }
    public enum DeployMode { Tap, Drag, Both }

    [Serializable]
    public class PlayerData
    {
        public string playerName;
        public List<CRClone.UI.Screens.BattleLogEntry> battleLog;
        // Additive: every `playerData.<member>` consumed repo-wide (verified per usage site).
        public string playerTag;
        public int trophies;
        public int bestTrophies;
        public int gems;
        public long gold;
        public int level;
        public long experience;
        public int wins;
        public int losses;
        public int draws;
        public int threeCrownWins;
        public int favoriteCardId;
        public Dictionary<int, CardCollectionEntry> collection = new();
        public Dictionary<int, int> cardLevels = new();
        public List<CRClone.UI.Components.ChestData> chestSlots = new();
        public PlayerClanData clan;
        // Tech-debt: Core -> UI reference (fully-qualified, no using added) to avoid touching consumers.
        public CRClone.UI.Screens.ClanRole clanRole;
    }

    [Serializable]
    public class PlayerClanData
    {
        public string name;
        public string description;
        public int trophyRequirement;
        public int memberCount;
        public List<CRClone.UI.Screens.ClanMember> members = new();
        public List<CRClone.UI.Screens.ChatMessage> messages = new();
    }

    [Serializable]
    public class BattleData
    {
        public long battleId;
        public BattleType type;
        public ulong seed;
        public PlayerBattleInfo player1;
        public PlayerBattleInfo player2;
        public long replayId;
    }

    public enum BattleType { Ladder, TwoVTwo, Tournament, Challenge, Friendly, Practice, ClanWar }

    [Serializable]
    public class PlayerBattleInfo
    {
        public long playerId;
        public string username;
        public int trophies;
        public DeckData deck;
        public int kingTowerLevel;
        public int princessTowerLevel;
    }

    [Serializable]
    public class BattleResult
    {
        public long battleId;
        public BattleStatus result;
        public int player1Crowns;
        public int player2Crowns;
        public int player1TrophyChange;
        public int player2TrophyChange;
        public float duration;
        public bool wentOvertime;
        public long replayId;
        public List<BattleEvent> events = new();
    }

    [Serializable]
    public class BattleEvent
    {
        public uint tick;
        public EventType type;
        public int playerId;
        public int cardId;
        public Vector2 position;
        public Dictionary<string, object> data;
    }

    public enum EventType
    {
        CardPlayed,
        SpellCast,
        UnitSpawned,
        UnitDied,
        BuildingPlaced,
        BuildingDestroyed,
        TowerDamaged,
        TowerDestroyed,
        KingActivated,
        ChampionAbility
    }
}