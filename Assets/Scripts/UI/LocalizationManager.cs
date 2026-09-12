using System;
using System.Collections.Generic;
using UnityEngine;

namespace CRClone.UI
{
    public enum SupportedLanguage
    {
        English = 0,
        ChineseSimplified = 1,
        ChineseTraditional = 2,
        Korean = 3,
        Japanese = 4,
        Spanish = 5,
        Portuguese = 6,
        French = 7,
        German = 8,
        Russian = 9,
        Turkish = 10,
        Arabic = 11,
        Thai = 12,
        Vietnamese = 13,
        Indonesian = 14
    }

    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private SupportedLanguage _defaultLanguage = SupportedLanguage.English;
        [SerializeField] private bool _autoDetectLanguage = true;
        [SerializeField] private TextAsset _localizationData;

        [Header("Font Fallbacks")]
        [SerializeField] private Font _latinFont;
        [SerializeField] private Font _cjkFont;
        [SerializeField] private Font _arabicFont;
        [SerializeField] private Font _thaiFont;

        private Dictionary<string, Dictionary<SupportedLanguage, string>> _localizedStrings;
        private SupportedLanguage _currentLanguage;
        private bool _isRTL;
        private float _textExpansionFactor = 1.3f;

        public SupportedLanguage CurrentLanguage => _currentLanguage;
        public bool IsRTL => _isRTL;
        public float TextExpansionFactor => _textExpansionFactor;

        public event Action<SupportedLanguage> OnLanguageChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void Initialize()
        {
            _localizedStrings = new Dictionary<string, Dictionary<SupportedLanguage, string>>();
            ParseLocalizationData();

            SupportedLanguage languageToUse = _defaultLanguage;

            if (_autoDetectLanguage)
            {
                languageToUse = DetectSystemLanguage();
            }

            languageToUse = (SupportedLanguage)PlayerPrefs.GetInt("localization_language", (int)languageToUse);
            SetLanguage(languageToUse);
        }

        private SupportedLanguage DetectSystemLanguage()
        {
            string systemLang = Application.systemLanguage.ToString();

            return systemLang switch
            {
                "Chinese" => SupportedLanguage.ChineseSimplified,
                "Japanese" => SupportedLanguage.Japanese,
                "Korean" => SupportedLanguage.Korean,
                "Spanish" => SupportedLanguage.Spanish,
                "Portuguese" => SupportedLanguage.Portuguese,
                "French" => SupportedLanguage.French,
                "German" => SupportedLanguage.German,
                "Russian" => SupportedLanguage.Russian,
                "Turkish" => SupportedLanguage.Turkish,
                "Arabic" => SupportedLanguage.Arabic,
                "Thai" => SupportedLanguage.Thai,
                "Vietnamese" => SupportedLanguage.Vietnamese,
                "Indonesian" => SupportedLanguage.Indonesian,
                _ => SupportedLanguage.English
            };
        }

        private void ParseLocalizationData()
        {
            if (_localizationData == null)
            {
                Debug.LogWarning("[LocalizationManager] No localization data assigned. Using fallback keys.");
                return;
            }

            string[] lines = _localizationData.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            bool headerParsed = false;
            List<string> languageHeaders = new List<string>();

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;

                string[] parts = line.Split('\t');
                if (parts.Length < 2) continue;

                if (!headerParsed)
                {
                    for (int i = 1; i < parts.Length; i++)
                    {
                        languageHeaders.Add(parts[i].Trim());
                    }
                    headerParsed = true;
                    continue;
                }

                string key = parts[0].Trim();
                var translations = new Dictionary<SupportedLanguage, string>();

                for (int i = 1; i < parts.Length && i - 1 < languageHeaders.Count; i++)
                {
                    if (Enum.TryParse<SupportedLanguage>(languageHeaders[i - 1], out SupportedLanguage lang))
                    {
                        translations[lang] = parts[i].Replace("\\n", "\n").Replace("\\t", "\t");
                    }
                }

                if (translations.Count > 0)
                {
                    _localizedStrings[key] = translations;
                }
            }

            Debug.Log($"[LocalizationManager] Loaded {_localizedStrings.Count} localization keys for {languageHeaders.Count} languages");
        }

        public void SetLanguage(SupportedLanguage language)
        {
            if (_currentLanguage == language) return;

            _currentLanguage = language;
            _isRTL = language == SupportedLanguage.Arabic;
            PlayerPrefs.SetInt("localization_language", (int)language);
            PlayerPrefs.Save();

            UpdateFontFallbacks();
            OnLanguageChanged?.Invoke(language);

            Debug.Log($"[LocalizationManager] Language changed to: {language}");
        }

        private void UpdateFontFallbacks()
        {
            Font fallbackFont = _currentLanguage switch
            {
                SupportedLanguage.ChineseSimplified or SupportedLanguage.ChineseTraditional or
                SupportedLanguage.Korean or SupportedLanguage.Japanese => _cjkFont,
                SupportedLanguage.Arabic => _arabicFont,
                SupportedLanguage.Thai => _thaiFont,
                _ => _latinFont
            };
        }

        public string GetString(string key, params object[] args)
        {
            if (_localizedStrings.TryGetValue(key, out var translations))
            {
                if (translations.TryGetValue(_currentLanguage, out string value))
                {
                    return args.Length > 0 ? string.Format(value, args) : value;
                }

                if (translations.TryGetValue(SupportedLanguage.English, out string fallback))
                {
                    return args.Length > 0 ? string.Format(fallback, args) : fallback;
                }
            }

            return key;
        }

        public string GetStringOrEmpty(string key, params object[] args)
        {
            string result = GetString(key, args);
            return result == key ? "" : result;
        }

        public bool HasKey(string key)
        {
            return _localizedStrings.ContainsKey(key);
        }

        public void AddRuntimeTranslation(string key, SupportedLanguage language, string value)
        {
            if (!_localizedStrings.ContainsKey(key))
            {
                _localizedStrings[key] = new Dictionary<SupportedLanguage, string>();
            }
            _localizedStrings[key][language] = value;
        }

        public string GetNumberString(float number, string format = "N0")
        {
            return number.ToString(format, GetCultureInfo());
        }

        public string GetDateString(DateTime date, string format = "d")
        {
            return date.ToString(format, GetCultureInfo());
        }

        private System.Globalization.CultureInfo GetCultureInfo()
        {
            return _currentLanguage switch
            {
                SupportedLanguage.English => System.Globalization.CultureInfo.GetCultureInfo("en-US"),
                SupportedLanguage.ChineseSimplified => System.Globalization.CultureInfo.GetCultureInfo("zh-CN"),
                SupportedLanguage.ChineseTraditional => System.Globalization.CultureInfo.GetCultureInfo("zh-TW"),
                SupportedLanguage.Korean => System.Globalization.CultureInfo.GetCultureInfo("ko-KR"),
                SupportedLanguage.Japanese => System.Globalization.CultureInfo.GetCultureInfo("ja-JP"),
                SupportedLanguage.Spanish => System.Globalization.CultureInfo.GetCultureInfo("es-ES"),
                SupportedLanguage.Portuguese => System.Globalization.CultureInfo.GetCultureInfo("pt-BR"),
                SupportedLanguage.French => System.Globalization.CultureInfo.GetCultureInfo("fr-FR"),
                SupportedLanguage.German => System.Globalization.CultureInfo.GetCultureInfo("de-DE"),
                SupportedLanguage.Russian => System.Globalization.CultureInfo.GetCultureInfo("ru-RU"),
                SupportedLanguage.Turkish => System.Globalization.CultureInfo.GetCultureInfo("tr-TR"),
                SupportedLanguage.Arabic => System.Globalization.CultureInfo.GetCultureInfo("ar-SA"),
                SupportedLanguage.Thai => System.Globalization.CultureInfo.GetCultureInfo("th-TH"),
                SupportedLanguage.Vietnamese => System.Globalization.CultureInfo.GetCultureInfo("vi-VN"),
                SupportedLanguage.Indonesian => System.Globalization.CultureInfo.GetCultureInfo("id-ID"),
                _ => System.Globalization.CultureInfo.InvariantCulture
            };
        }

        public Vector2 CalculateTextSize(string key, Text textComponent, params object[] args)
        {
            string localized = GetString(key, args);
            textComponent.text = localized;
            textComponent.rectTransform.sizeDelta = new Vector2(textComponent.preferredWidth * _textExpansionFactor, textComponent.preferredHeight);
            return textComponent.rectTransform.sizeDelta;
        }

        public Vector2 CalculateTextSize(string key, TMPro.TextMeshProUGUI tmpComponent, params object[] args)
        {
            string localized = GetString(key, args);
            tmpComponent.text = localized;
            tmpComponent.ForceMeshUpdate();
            return new Vector2(tmpComponent.preferredWidth * _textExpansionFactor, tmpComponent.preferredHeight);
        }
    }

    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string _localizationKey;
        [SerializeField] private bool _autoUpdate = true;
        [SerializeField] private object[] _formatArgs;

        private Text _text;
        private TMPro.TextMeshProUGUI _tmpText;

        private void Awake()
        {
            _text = GetComponent<Text>();
            _tmpText = GetComponent<TMPro.TextMeshProUGUI>();

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
                UpdateText();
            }
        }

        private void OnDestroy()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
            }
        }

        private void OnLanguageChanged(SupportedLanguage language)
        {
            if (_autoUpdate)
            {
                UpdateText();
            }
        }

        public void UpdateText(params object[] args)
        {
            if (args.Length > 0) _formatArgs = args;

            string localized = LocalizationManager.Instance?.GetString(_localizationKey, _formatArgs) ?? _localizationKey;

            if (_text != null) _text.text = localized;
            if (_tmpText != null) _tmpText.text = localized;
        }

        public void SetKey(string key)
        {
            _localizationKey = key;
            UpdateText();
        }

        public void SetFormatArgs(params object[] args)
        {
            _formatArgs = args;
            UpdateText();
        }
    }

    public class RTLLayoutHandler : MonoBehaviour
    {
        [SerializeField] private RectTransform _targetRect;
        [SerializeField] private bool _mirrorHorizontally = true;
        [SerializeField] private bool _mirrorChildren = true;

        private void Awake()
        {
            if (_targetRect == null) _targetRect = GetComponent<RectTransform>();

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
                ApplyRTL(LocalizationManager.Instance.IsRTL);
            }
        }

        private void OnDestroy()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
            }
        }

        private void OnLanguageChanged(SupportedLanguage language)
        {
            ApplyRTL(LocalizationManager.Instance.IsRTL);
        }

        private void ApplyRTL(bool isRTL)
        {
            if (!isRTL || !_mirrorHorizontally) return;

            Vector2 anchorMin = _targetRect.anchorMin;
            Vector2 anchorMax = _targetRect.anchorMax;

            float temp = anchorMin.x;
            anchorMin.x = 1f - anchorMax.x;
            anchorMax.x = 1f - temp;

            _targetRect.anchorMin = anchorMin;
            _targetRect.anchorMax = anchorMax;

            if (_mirrorChildren)
            {
                foreach (RectTransform child in _targetRect)
                {
                    Vector2 childAnchorMin = child.anchorMin;
                    Vector2 childAnchorMax = child.anchorMax;

                    float childTemp = childAnchorMin.x;
                    childAnchorMin.x = 1f - childAnchorMax.x;
                    childAnchorMax.x = 1f - childTemp;

                    child.anchorMin = childAnchorMin;
                    child.anchorMax = childAnchorMax;
                }
            }
        }
    }
}