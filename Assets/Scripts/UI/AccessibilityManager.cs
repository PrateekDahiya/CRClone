using System;
using UnityEngine;
using UnityEngine.UI;

namespace CRClone.UI
{
    public class AccessibilityManager : MonoBehaviour
    {
        public static AccessibilityManager Instance { get; private set; }

        [Header("Visual Settings")]
        [SerializeField] private bool _highContrastMode = false;
        [SerializeField] private bool _reduceMotion = false;
        [SerializeField] private float _textScale = 1.0f;
        [SerializeField] private ColorBlindMode _colorBlindMode = ColorBlindMode.None;

        [Header("Color Blind Palettes")]
        [SerializeField] private ColorBlindPalette _protanopiaPalette;
        [SerializeField] private ColorBlindPalette _deuteranopiaPalette;
        [SerializeField] private ColorBlindPalette _tritanopiaPalette;

        [Header("High Contrast Colors")]
        [SerializeField] private Color _highContrastText = Color.white;
        [SerializeField] private Color _highContrastBackground = Color.black;
        [SerializeField] private Color _highContrastAccent = Color.yellow;
        [SerializeField] private Color _highContrastButtonNormal = new Color(0.2f, 0.2f, 0.2f);
        [SerializeField] private Color _highContrastButtonHighlight = new Color(0.4f, 0.4f, 0.4f);
        [SerializeField] private Color _highContrastButtonPressed = new Color(0.6f, 0.6f, 0.6f);

        [Header("Rarity Patterns (for color blind)")]
        [SerializeField] private Sprite _commonPattern;
        [SerializeField] private Sprite _rarePattern;
        [SerializeField] private Sprite _epicPattern;
        [SerializeField] private Sprite _legendaryPattern;
        [SerializeField] private Sprite _championPattern;

        public bool HighContrastMode => _highContrastMode;
        public bool ReduceMotion => _reduceMotion;
        public float TextScale => _textScale;
        public ColorBlindMode ColorBlindMode => _colorBlindMode;

        public event Action<bool> OnHighContrastChanged;
        public event Action<bool> OnReduceMotionChanged;
        public event Action<float> OnTextScaleChanged;
        public event Action<ColorBlindMode> OnColorBlindModeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadSettings();
            ApplySettings();
        }

        public void SetHighContrastMode(bool enabled)
        {
            if (_highContrastMode != enabled)
            {
                _highContrastMode = enabled;
                SaveSettings();
                ApplyHighContrast();
                OnHighContrastChanged?.Invoke(enabled);
            }
        }

        public void SetReduceMotion(bool enabled)
        {
            if (_reduceMotion != enabled)
            {
                _reduceMotion = enabled;
                SaveSettings();
                OnReduceMotionChanged?.Invoke(enabled);
            }
        }

        public void SetTextScale(float scale)
        {
            scale = Mathf.Clamp(scale, 1.0f, 2.0f);
            if (Mathf.Abs(_textScale - scale) > 0.01f)
            {
                _textScale = scale;
                SaveSettings();
                ApplyTextScale();
                OnTextScaleChanged?.Invoke(scale);
            }
        }

        public void SetColorBlindMode(ColorBlindMode mode)
        {
            if (_colorBlindMode != mode)
            {
                _colorBlindMode = mode;
                SaveSettings();
                ApplyColorBlindMode();
                OnColorBlindModeChanged?.Invoke(mode);
            }
        }

        private void ApplySettings()
        {
            ApplyHighContrast();
            ApplyTextScale();
            ApplyColorBlindMode();
        }

        private void ApplyHighContrast()
        {
            if (_highContrastMode)
            {
                ApplyHighContrastToAllUI();
            }
            else
            {
                RestoreOriginalColors();
            }
        }

        private void ApplyHighContrastToAllUI()
        {
            var texts = FindObjectsOfType<Text>(true);
            foreach (var text in texts)
            {
                if (text.gameObject.GetComponent<AccessibilityIgnore>() == null)
                {
                    text.color = _highContrastText;
                }
            }

            var images = FindObjectsOfType<Image>(true);
            foreach (var image in images)
            {
                if (image.gameObject.GetComponent<AccessibilityIgnore>() == null && image.sprite == null)
                {
                    if (image.color != Color.clear)
                    {
                        image.color = _highContrastBackground;
                    }
                }
            }

            var buttons = FindObjectsOfType<Button>(true);
            foreach (var button in buttons)
            {
                if (button.gameObject.GetComponent<AccessibilityIgnore>() == null)
                {
                    var colors = button.colors;
                    colors.normalColor = _highContrastButtonNormal;
                    colors.highlightedColor = _highContrastButtonHighlight;
                    colors.pressedColor = _highContrastButtonPressed;
                    colors.disabledColor = _highContrastButtonNormal * 0.5f;
                    button.colors = colors;
                }
            }
        }

        private void RestoreOriginalColors()
        {
        }

        private void ApplyTextScale()
        {
            var texts = FindObjectsOfType<Text>(true);
            foreach (var text in texts)
            {
                if (text.gameObject.GetComponent<AccessibilityIgnore>() == null)
                {
                    var scaler = text.GetComponent<TextScaleHandler>();
                    if (scaler == null) scaler = text.gameObject.AddComponent<TextScaleHandler>();
                    scaler.BaseFontSize = text.fontSize;
                    scaler.ApplyScale(_textScale);
                }
            }

            var tmpTexts = FindObjectsOfType<TMPro.TextMeshProUGUI>(true);
            foreach (var tmpText in tmpTexts)
            {
                if (tmpText.gameObject.GetComponent<AccessibilityIgnore>() == null)
                {
                    var scaler = tmpText.GetComponent<TextScaleHandler>();
                    if (scaler == null) scaler = tmpText.gameObject.AddComponent<TextScaleHandler>();
                    scaler.BaseFontSize = (int)tmpText.fontSize;
                    scaler.ApplyScale(_textScale);
                }
            }
        }

        private void ApplyColorBlindMode()
        {
            ColorBlindPalette palette = _colorBlindMode switch
            {
                ColorBlindMode.Protanopia => _protanopiaPalette,
                ColorBlindMode.Deuteranopia => _deuteranopiaPalette,
                ColorBlindMode.Tritanopia => _tritanopiaPalette,
                _ => null
            };

            if (palette != null)
            {
                ApplyPalette(palette);
            }
            else
            {
                RestoreOriginalColors();
            }
        }

        private void ApplyPalette(ColorBlindPalette palette)
        {
            var images = FindObjectsOfType<Image>(true);
            foreach (var image in images)
            {
                if (image.gameObject.GetComponent<AccessibilityIgnore>() == null)
                {
                    var rarityTag = image.GetComponent<RarityColorTag>();
                    if (rarityTag != null)
                    {
                        image.color = palette.GetColorForRarity(rarityTag.Rarity);
                    }
                }
            }
        }

        public Sprite GetRarityPattern(CardRarity rarity)
        {
            return rarity switch
            {
                CardRarity.Common => _commonPattern,
                CardRarity.Rare => _rarePattern,
                CardRarity.Epic => _epicPattern,
                CardRarity.Legendary => _legendaryPattern,
                CardRarity.Champion => _championPattern,
                _ => null
            };
        }

        public Color GetHighContrastTextColor() => _highContrastText;
        public Color GetHighContrastBackgroundColor() => _highContrastBackground;
        public Color GetHighContrastAccentColor() => _highContrastAccent;

        private void LoadSettings()
        {
            _highContrastMode = PlayerPrefs.GetInt("accessibility_high_contrast", 0) == 1;
            _reduceMotion = PlayerPrefs.GetInt("accessibility_reduce_motion", 0) == 1;
            _textScale = PlayerPrefs.GetFloat("accessibility_text_scale", 1.0f);
            _colorBlindMode = (ColorBlindMode)PlayerPrefs.GetInt("accessibility_color_blind", 0);
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetInt("accessibility_high_contrast", _highContrastMode ? 1 : 0);
            PlayerPrefs.SetInt("accessibility_reduce_motion", _reduceMotion ? 1 : 0);
            PlayerPrefs.SetFloat("accessibility_text_scale", _textScale);
            PlayerPrefs.SetInt("accessibility_color_blind", (int)_colorBlindMode);
            PlayerPrefs.Save();
        }

        public void ResetToDefaults()
        {
            _highContrastMode = false;
            _reduceMotion = false;
            _textScale = 1.0f;
            _colorBlindMode = ColorBlindMode.None;
            SaveSettings();
            ApplySettings();
        }
    }

    public enum ColorBlindMode
    {
        None = 0,
        Protanopia = 1,
        Deuteranopia = 2,
        Tritanopia = 3
    }

    [Serializable]
    public class ColorBlindPalette
    {
        public Color common = Color.gray;
        public Color rare = Color.blue;
        public Color epic = Color.magenta;
        public Color legendary = Color.yellow;
        public Color champion = new Color(1f, 0.2f, 0.6f);

        public Color GetColorForRarity(CardRarity rarity)
        {
            return rarity switch
            {
                CardRarity.Common => common,
                CardRarity.Rare => rare,
                CardRarity.Epic => epic,
                CardRarity.Legendary => legendary,
                CardRarity.Champion => champion,
                _ => Color.white
            };
        }
    }

    public class AccessibilityIgnore : MonoBehaviour { }

    public class TextScaleHandler : MonoBehaviour
    {
        public int BaseFontSize { get; set; }
        private Text _text;
        private TMPro.TextMeshProUGUI _tmpText;

        private void Awake()
        {
            _text = GetComponent<Text>();
            _tmpText = GetComponent<TMPro.TextMeshProUGUI>();
        }

        public void ApplyScale(float scale)
        {
            int newSize = Mathf.RoundToInt(BaseFontSize * scale);
            if (_text != null) _text.fontSize = newSize;
            if (_tmpText != null) _tmpText.fontSize = newSize;
        }
    }

    public class RarityColorTag : MonoBehaviour
    {
        public CardRarity Rarity;
    }
}