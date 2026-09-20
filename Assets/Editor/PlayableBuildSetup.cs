using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.Data;
using CRClone.UI.Screens;

namespace CRClone.EditorTools
{
    /// <summary>
    /// Builds the runnable shell the project was missing: a GameConfig asset, one
    /// screen prefab per <see cref="ScreenType"/> under Resources/UI/Screens, and a
    /// scene per <see cref="GameManager.GameScene"/> registered in Build Settings.
    ///
    /// GameManager drives navigation with SceneManager.LoadSceneAsync(GameScene name),
    /// so every enum value needs a real scene or the load silently fails and the game
    /// sits on an empty frame. Re-runnable: existing assets are overwritten in place.
    /// </summary>
    public static class PlayableBuildSetup
    {
        const string ScreenPrefabDir = "Assets/Resources/UI/Screens";
        const string ConfigPath = "Assets/Resources/Configs/GameConfig.asset";
        const string SceneDir = "Assets/Scenes";

        static readonly Color Bg = new Color(0.09f, 0.13f, 0.22f);
        static readonly Color Panel = new Color(0.16f, 0.22f, 0.35f);
        static readonly Color Accent = new Color(0.25f, 0.55f, 0.95f);

        // Child names are a contract with UIManager.Initialize*Screen(), which wires
        // navigation via transform.Find(...). Renaming these silently breaks buttons.
        static readonly Dictionary<ScreenType, string[]> ScreenButtons = new()
        {
            { ScreenType.MainMenu, new[] { "PlayButton", "DeckButton", "ShopButton", "ClanButton", "ProfileButton", "SettingsButton" } },
            { ScreenType.Lobby, new[] { "Battle1v1", "Battle2v2", "Tournament", "Friendly", "Practice", "DeckBuilderButton", "BackButton" } },
            { ScreenType.DeckBuilder, new[] { "BackButton" } },
            { ScreenType.Shop, new[] { "BackButton" } },
            { ScreenType.Clan, new[] { "BackButton" } },
            { ScreenType.Profile, new[] { "BackButton" } },
            { ScreenType.Settings, new[] { "BackButton" } },
            { ScreenType.BattleResult, new[] { "ContinueButton" } },
            { ScreenType.ChestUnlock, new[] { "BackButton" } },
            { ScreenType.QuestLog, new[] { "BackButton" } },
            { ScreenType.Tournament, new[] { "BackButton" } },
        };

        [MenuItem("CRClone/Asset Pipeline/Build Playable Shell")]
        public static void Build()
        {
            EnsureConfig();
            EnsureUILayer();

            Directory.CreateDirectory(ScreenPrefabDir);
            Directory.CreateDirectory(SceneDir);

            foreach (var kv in ScreenButtons)
                BuildScreenPrefab(kv.Key, kv.Value);

            var scenes = new List<EditorBuildSettingsScene>();
            foreach (GameScene scene in System.Enum.GetValues(typeof(GameScene)))
                scenes.Add(new EditorBuildSettingsScene(BuildScene(scene), true));

            // Boot must be index 0 -- it is the scene Unity opens and the one that
            // creates GameManager, which then drives every other load.
            EditorBuildSettings.scenes = scenes
                .OrderBy(s => s.path.EndsWith("/Boot.unity") ? 0 : 1)
                .ToArray();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PlayableBuildSetup] Built {ScreenButtons.Count} screens and {scenes.Count} scenes.");
        }

        static void EnsureConfig()
        {
            if (AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath) != null) return;
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<GameConfig>(), ConfigPath);
            Debug.Log($"[PlayableBuildSetup] Created {ConfigPath}");
        }

        /// <summary>Canvas objects are put on the "UI" layer; guarantee it exists.</summary>
        static void EnsureUILayer()
        {
            if (LayerMask.NameToLayer("UI") >= 0) return;
            Debug.LogWarning("[PlayableBuildSetup] No 'UI' layer found; canvases will use Default.");
        }

        // ---- screen prefabs ---------------------------------------------------

        static void BuildScreenPrefab(ScreenType type, string[] buttons)
        {
            var root = new GameObject(type.ToString(), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = (RectTransform)root.transform;
            Stretch(rt);
            root.GetComponent<Image>().color = Bg;

            MakeText(rt, "Title", type.ToString(), 64, new Vector2(0, -160), new Vector2(900, 110), FontStyle.Bold);

            if (type == ScreenType.MainMenu)
                BuildMainMenuExtras(rt);

            float y = type == ScreenType.MainMenu ? -420 : -340;
            foreach (var name in buttons)
            {
                MakeButton(rt, name, Prettify(name), new Vector2(0, y), new Vector2(620, 110));
                y -= 130;
            }

            // MainMenuScreen drives real behaviour; attach and wire what we built.
            if (type == ScreenType.MainMenu)
                WireMainMenuScreen(root);

            var path = $"{ScreenPrefabDir}/{type}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        static void BuildMainMenuExtras(RectTransform parent)
        {
            var bar = new GameObject("TopBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var brt = (RectTransform)bar.transform;
            brt.SetParent(parent, false);
            brt.anchorMin = new Vector2(0, 1);
            brt.anchorMax = new Vector2(1, 1);
            brt.pivot = new Vector2(0.5f, 1);
            brt.sizeDelta = new Vector2(0, 120);
            brt.anchoredPosition = Vector2.zero;
            bar.GetComponent<Image>().color = Panel;

            MakeText(brt, "PlayerNameText", "Player", 34, new Vector2(-330, -60), new Vector2(320, 60), FontStyle.Bold);
            MakeText(brt, "TrophyText", "0", 32, new Vector2(-40, -60), new Vector2(200, 60), FontStyle.Normal);
            MakeText(brt, "GemText", "0", 32, new Vector2(160, -60), new Vector2(200, 60), FontStyle.Normal);
            MakeText(brt, "GoldText", "0", 32, new Vector2(360, -60), new Vector2(200, 60), FontStyle.Normal);
        }

        static void WireMainMenuScreen(GameObject root)
        {
            var screen = root.AddComponent<MainMenuScreen>();
            var so = new SerializedObject(screen);

            Assign(so, "_playerNameText", Find<Text>(root, "PlayerNameText"));
            Assign(so, "_trophyText", Find<Text>(root, "TrophyText"));
            Assign(so, "_gemText", Find<Text>(root, "GemText"));
            Assign(so, "_goldText", Find<Text>(root, "GoldText"));
            Assign(so, "_battleButton", Find<Button>(root, "PlayButton"));
            Assign(so, "_settingsButton", Find<Button>(root, "SettingsButton"));
            Assign(so, "_clanNavButton", Find<Button>(root, "ClanButton"));
            Assign(so, "_shopNavButton", Find<Button>(root, "ShopButton"));
            Assign(so, "_cardsNavButton", Find<Button>(root, "DeckButton"));
            Assign(so, "_profileNavButton", Find<Button>(root, "ProfileButton"));

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Assign(SerializedObject so, string field, Object value)
        {
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning($"[PlayableBuildSetup] MainMenuScreen has no field '{field}'.");
                return;
            }
            prop.objectReferenceValue = value;
        }

        static T Find<T>(GameObject root, string name) where T : Component
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name)
                {
                    var c = t.GetComponent<T>();
                    if (c != null) return c;
                }
            return null;
        }

        // ---- scenes -----------------------------------------------------------

        static string BuildScene(GameScene which)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGO = new GameObject("Main Camera", typeof(Camera));
            camGO.tag = "MainCamera";
            var cam = camGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Bg;
            cam.orthographic = true;
            cam.orthographicSize = 10f;
            camGO.transform.position = new Vector3(0, 0, -10);

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            // Boot owns GameManager. Other scenes get it via DontDestroyOnLoad, or
            // via GameBootstrap's RuntimeInitializeOnLoadMethod when entered directly.
            if (which == GameScene.Boot)
            {
                var gm = new GameObject("GameManager", typeof(GameManager), typeof(GameBootstrap));
                var so = new SerializedObject(gm.GetComponent<GameManager>());
                var cfg = so.FindProperty("_gameConfig");
                if (cfg != null) cfg.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                // A visible placeholder so a directly-opened scene is never blank.
                BuildSceneLabel(which.ToString());
            }

            var path = $"{SceneDir}/{which}.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        static void BuildSceneLabel(string label)
        {
            var canvasGO = new GameObject("SceneCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -100; // sit behind UIManager's canvas
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            MakeText((RectTransform)canvasGO.transform, "SceneLabel", label, 48,
                     new Vector2(0, 0), new Vector2(900, 120), FontStyle.Bold);
        }

        // ---- uGUI helpers -----------------------------------------------------

        static Font UIFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static Text MakeText(RectTransform parent, string name, string content, int size,
                             Vector2 pos, Vector2 sizeDelta, FontStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = pos;

            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = UIFont;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        static Button MakeButton(RectTransform parent, string name, string label, Vector2 pos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = pos;

            var img = go.GetComponent<Image>();
            img.color = Accent;

            var button = go.GetComponent<Button>();
            button.targetGraphic = img;

            var textRT = MakeText(rt, "Label", label, 38, Vector2.zero, sizeDelta, FontStyle.Bold)
                         .GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            textRT.anchoredPosition = Vector2.zero;

            return button;
        }

        static string Prettify(string name)
        {
            var trimmed = name.EndsWith("Button") && name.Length > 6
                ? name.Substring(0, name.Length - 6)
                : name;
            return string.Concat(trimmed.Select((c, i) =>
                i > 0 && char.IsUpper(c) && !char.IsUpper(trimmed[i - 1]) ? " " + c : c.ToString()));
        }
    }
}
