using System;
using UnityEngine;
using UnityEngine.UI;

namespace CRClone.UI
{
    public enum ColorBlindMode
    {
        None,
        Protanopia,
        Deuteranopia,
        Tritanopia
    }

    public class AccessibilityManager : MonoBehaviour
    {
        public static AccessibilityManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private ColorBlindMode _colorBlindMode = ColorBlindMode.None;
        [SerializeField] private bool _highContrastMode = false;
        [SerializeField] private bool _reduceMotion = false;
        [SerializeField] private float _textScale = 1.0f;

        [Header("Color Blind Palettes")]
        [SerializeField] private Color _protanopiaCommon = new Color(0.6f, 0.6f, 0.6f);
        [SerializeField] private Color _protanopiaRare = new Color(0.0f, 0.6f, 0.9f);
        [SerializeField] private Color _protanopiaEpic = new Color(0.7f, 0.3f, 0.7f);
        [SerializeField] private Color _protanopiaLegendary = new Color(1.0f, 0.7f, 0.0f);
        [SerializeField] private Color _protanopiaChampion = new Color(0.9f, 0.2f, 0.4f);

        [SerializeField] private Color _deuteranopiaCommon = new Color(0.6f, 0.6f, 0.6f);
        [SerializeField] private Color _deuteranopiaRare = new Color(0.0f, 0.7f, 0.8f);
        [SerializeField] private Color _deuteranopiaEpic = new Color(0.6f, 0.2f, 0.6f);
        [SerializeField] private Color _deuteranopiaLegendary = new Color(1.0f, 0.8f, 0.0f);
        [SerializeField] private Color _deuteranopiaChampion = new Color(0.8f, 0.1f, 0.3f);

        [SerializeField] private Color _tritanopiaCommon = new Color(0.6f, 0.6f, 0.6f);
        [SerializeField] private Color _tritanopiaRare = new Color(0.2f, 0.5f, 0.9f);
        [SerializeField] private Color _tritanopiaEpic = new Color(0.8f, 0.4f, 0.8f);
        [SerializeField] private Color _tritanopiaLegendary = new Color(1.0f, 0.5f, 0.0f);
        [SerializeField] private Color _tritanopiaChampion = new Color(0.9f, 0.3f, 0.5f);

        [Header("High Contrast Colors")]
        [SerializeField] private Color _highContrastBg = Color.black;
        [SerializeField] private Color _highContrastText = Color.white;
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

        public ColorBlindMode ColorBlindMode => _colorBlindMode;
        public bool HighContrastMode => _highContrastMode;
        public bool ReduceMotion => _reduceMotion;
        public float TextScale => _textScale;

        public event Action<ColorBlindMode> OnColorBlindModeChanged;
        public event Action<bool> OnHighContrastChanged;
        public event Action<bool> OnReduceMotionChanged;
        public event Action<float> OnTextScaleChanged;

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
            scale = Mathf.Clamp(scale, 0.5f, 2.0f);
            if (Mathf.Abs(_textScale - scale) > 0.01f)
            {
                _textScale = scale;
                SaveSettings();
                ApplyTextScale();
                OnTextScaleChanged?.Invoke(scale);
            }
        }

        private void LoadSettings()
        {
            _colorBlindMode = (ColorBlindMode)PlayerPrefs.GetInt("accessibility_colorblind", 0);
            _highContrastMode = PlayerPrefs.GetInt("accessibility_highcontrast", 0) == 1;
            _reduceMotion = PlayerPrefs.GetInt("accessibility_reducemotion", 0) == 1;
            _textScale = PlayerPrefs.GetFloat("accessibility_textscale", 1.0f);
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetInt("accessibility_colorblind", (int)_colorBlindMode);
            PlayerPrefs.SetInt("accessibility_highcontrast", _highContrastMode ? 1 : 0);
            PlayerPrefs.SetInt("accessibility_reducemotion", _reduceMotion ? 1 : 0);
            PlayerPrefs.SetFloat("accessibility_textscale", _textScale);
            PlayerPrefs.Save();
        }

        private void ApplySettings()
        {
            ApplyColorBlindMode();
            ApplyHighContrast();
            ApplyTextScale();
        }

        private void ApplyColorBlindMode()
        {
            // Apply color blind palette to UI elements with RarityColorTag
            var tags = FindObjectsOfType<RarityColorTag>();
            foreach (var tag in tags)
            {
                tag.UpdateColor();
            }
        }

        private void ApplyHighContrast()
        {
            if (_highContrastMode)
            {
                ApplyHighContrastToAllUI();
            }
            else
            {
                // Reset to original colors - would need to store originals
                RefreshAllUIColors();
            }
        }

        private void ApplyHighContrastToAllUI()
        {
            var texts = FindObjectsOfType<Text>();
            foreach (var text in texts)
            {
                if (text.GetComponent<IgnoreAccessibility>() == null)
                {
                    text.color = _highContrastText;
                }
            }

            var images = FindObjectsOfType<Image>();
            foreach (var image in images)
            {
                if (image.GetComponent<IgnoreAccessibility>() == null && image.sprite == null)
                {
                    if (image.color != Color.clear)
                    {
                        image.color = _highContrastBg;
                    }
                }
            }

            var buttons = FindObjectsOfType<Button>();
            foreach (var button in buttons)
            {
                if (button.GetComponent<IgnoreAccessibility>() == null)
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

        private void RefreshAllUIColors()
        {
            var tags = FindObjectsOfType<RarityColorTag>();
            foreach (var tag in tags)
            {
                tag.UpdateColor();
            }
        }

        private void ApplyTextScale()
        {
            var scalers = FindObjectsOfType<TextScaleHandler>();
            foreach (var scaler in scalers)
            {
                scaler.ApplyScale(_textScale);
            }
        }

        public Color GetRarityColor(CardRarity rarity)
        {
            if (_colorBlindMode != ColorBlindMode.None)
            {
                return GetColorBlindRarityColor(rarity);
            }

            return rarity switch
            {
                CardRarity.Common => new Color(0.62f, 0.62f, 0.62f),
                CardRarity.Rare => new Color(0.13f, 0.59f, 0.95f),
                CardRarity.Epic => new Color(0.61f, 0.15f, 0.69f),
                CardRarity.Legendary => new Color(1f, 0.6f, 0f),
                CardRarity.Champion => new Color(0.91f, 0.12f, 0.39f),
                _ => Color.white
            };
        }

        private Color GetColorBlindRarityColor(CardRarity rarity)
        {
            return _colorBlindMode switch
            {
                ColorBlindMode.Protanopia => rarity switch
                {
                    CardRarity.Common => _protanopiaCommon,
                    CardRarity.Rare => _protanopiaRare,
                    CardRarity.Epic => _protanopiaEpic,
                    CardRarity.Legendary => _protanopiaLegendary,
                    CardRarity.Champion => _protanopiaChampion,
                    _ => Color.white
                },
                ColorBlindMode.Deuteranopia => rarity switch
                {
                    CardRarity.Common => _deuteranopiaCommon,
                    CardRarity.Rare => _deuteranopiaRare,
                    CardRarity.Epic => _deuteranopiaEpic,
                    CardRarity.Legendary => _deuteranopiaLegendary,
                    CardRarity.Champion => _deuteranopiaChampion,
                    _ => Color.white
                },
                ColorBlindMode.Tritanopia => rarity switch
                {
                    CardRarity.Common => _tritanopiaCommon,
                    CardRarity.Rare => _tritanopiaRare,
                    CardRarity.Epic => _tritanopiaEpic,
                    CardRarity.Legendary => _tritanopiaLegendary,
                    CardRarity.Champion => _tritanopiaChampion,
                    _ => Color.white
                },
                _ => Color.white
            };
        }

        public Sprite GetRarityPattern(CardRarity rarity)
        {
            if (_colorBlindMode == ColorBlindMode.None) return null;

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
        public Color GetHighContrastBgColor() => _highContrastBg;
        public Color GetHighContrastAccentColor() => _highContrastAccent;

        public void ResetToDefaults()
        {
            _colorBlindMode = ColorBlindMode.None;
            _highContrastMode = false;
            _reduceMotion = false;
            _textScale = 1.0f;
            SaveSettings();
            ApplySettings();
        }
    }

    public class RarityColorTag : MonoBehaviour
    {
        [SerializeField] private CardRarity _rarity;
        [SerializeField] private Image _targetImage;
        [SerializeField] private bool _usePattern = true;

        public CardRarity Rarity => _rarity;

        private void Awake()
        {
            if (_targetImage == null) _targetImage = GetComponent<Image>();
            UpdateColor();
        }

        public void UpdateColor()
        {
            if (_targetImage == null) return;

            if (AccessibilityManager.Instance != null)
            {
                _targetImage.color = AccessibilityManager.Instance.GetRarityColor(_rarity);
            }
        }

        public void SetRarity(CardRarity rarity)
        {
            _rarity = rarity;
            UpdateColor();
        }
    }

    public class TextScaleHandler : MonoBehaviour
    {
        [SerializeField] private Text _text;
        [SerializeField] private float _baseFontSize = 16f;

        private void Awake()
        {
            if (_text == null) _text = GetComponent<Text>();
            _baseFontSize = _text.fontSize;

            if (AccessibilityManager.Instance != null)
            {
                AccessibilityManager.Instance.OnTextScaleChanged += OnTextScaleChanged;
                ApplyScale(AccessibilityManager.Instance.TextScale);
            }
        }

        private void OnDestroy()
        {
            if (AccessibilityManager.Instance != null)
            {
                AccessibilityManager.Instance.OnTextScaleChanged -= OnTextScaleChanged;
            }
        }

        private void OnTextScaleChanged(float scale)
        {
            ApplyScale(scale);
        }

        public void ApplyScale(float scale)
        {
            if (_text != null)
            {
                _text.fontSize = Mathf.RoundToInt(_baseFontSize * scale);
            }
        }
    }

    public class IgnoreAccessibility : MonoBehaviour { }
}