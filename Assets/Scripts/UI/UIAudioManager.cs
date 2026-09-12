using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace CRClone.UI
{
    public class UIAudioManager : MonoBehaviour
    {
        public static UIAudioManager Instance { get; private set; }

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private string _masterVolumeParam = "MasterVolume";
        [SerializeField] private string _musicVolumeParam = "MusicVolume";
        [SerializeField] private string _sfxVolumeParam = "SFXVolume";
        [SerializeField] private string _voiceVolumeParam = "VoiceVolume";

        [Header("UI Sliders (Optional - for Settings screen)")]
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Slider _voiceSlider;
        [SerializeField] private Toggle _muteToggle;

        [Header("Settings")]
        [SerializeField] private bool _muteOnFocusLoss = true;
        [SerializeField] private float _minVolumeDb = -80f;
        [SerializeField] private float _maxVolumeDb = 0f;

        private float _masterVolume = 1f;
        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;
        private float _voiceVolume = 1f;
        private bool _isMuted = false;
        private float _previousMasterVolume = 1f;

        public float MasterVolume => _masterVolume;
        public float MusicVolume => _musicVolume;
        public float SFXVolume => _sfxVolume;
        public float VoiceVolume => _voiceVolume;
        public bool IsMuted => _isMuted;
        public bool MuteOnFocusLoss => _muteOnFocusLoss;

        public event Action<float> OnMasterVolumeChanged;
        public event Action<float> OnMusicVolumeChanged;
        public event Action<float> OnSFXVolumeChanged;
        public event Action<float> OnVoiceVolumeChanged;
        public event Action<bool> OnMuteToggled;

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
            ApplyVolumes();
            BindSliders();
        }

        private void OnEnable()
        {
            Application.focusChanged += OnFocusChanged;
        }

        private void OnDisable()
        {
            Application.focusChanged -= OnFocusChanged;
        }

        private void OnFocusChanged(bool hasFocus)
        {
            if (_muteOnFocusLoss && !hasFocus && !_isMuted)
            {
                SetMute(true);
            }
            else if (_muteOnFocusLoss && hasFocus && _isMuted && _previousMasterVolume > 0)
            {
                SetMute(false);
            }
        }

        private void BindSliders()
        {
            if (_masterSlider != null)
            {
                _masterSlider.value = _masterVolume;
                _masterSlider.onValueChanged.AddListener(SetMasterVolume);
            }
            if (_musicSlider != null)
            {
                _musicSlider.value = _musicVolume;
                _musicSlider.onValueChanged.AddListener(SetMusicVolume);
            }
            if (_sfxSlider != null)
            {
                _sfxSlider.value = _sfxVolume;
                _sfxSlider.onValueChanged.AddListener(SetSFXVolume);
            }
            if (_voiceSlider != null)
            {
                _voiceSlider.value = _voiceVolume;
                _voiceSlider.onValueChanged.AddListener(SetVoiceVolume);
            }
            if (_muteToggle != null)
            {
                _muteToggle.isOn = _isMuted;
                _muteToggle.onValueChanged.AddListener(SetMute);
            }
        }

        public void SetMasterVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            _masterVolume = volume;
            if (!_isMuted)
            {
                ApplyVolume(_masterVolumeParam, volume);
            }
            UpdateSlider(_masterSlider, volume);
            SaveSettings();
            OnMasterVolumeChanged?.Invoke(volume);
        }

        public void SetMusicVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            _musicVolume = volume;
            ApplyVolume(_musicVolumeParam, volume);
            UpdateSlider(_musicSlider, volume);
            SaveSettings();
            OnMusicVolumeChanged?.Invoke(volume);
        }

        public void SetSFXVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            _sfxVolume = volume;
            ApplyVolume(_sfxVolumeParam, volume);
            UpdateSlider(_sfxSlider, volume);
            SaveSettings();
            OnSFXVolumeChanged?.Invoke(volume);
        }

        public void SetVoiceVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            _voiceVolume = volume;
            ApplyVolume(_voiceVolumeParam, volume);
            UpdateSlider(_voiceSlider, volume);
            SaveSettings();
            OnVoiceVolumeChanged?.Invoke(volume);
        }

        public void SetMute(bool muted)
        {
            if (_isMuted == muted) return;

            _isMuted = muted;
            UpdateSlider(_muteToggle, muted);

            if (muted)
            {
                _previousMasterVolume = _masterVolume;
                ApplyVolume(_masterVolumeParam, 0f);
            }
            else
            {
                ApplyVolume(_masterVolumeParam, _previousMasterVolume);
            }

            SaveSettings();
            OnMuteToggled?.Invoke(muted);
        }

        public void ToggleMute()
        {
            SetMute(!_isMuted);
        }

        public void SetMuteOnFocusLoss(bool enabled)
        {
            _muteOnFocusLoss = enabled;
            PlayerPrefs.SetInt("audio_mute_on_focus_loss", enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void ApplyVolumes()
        {
            ApplyVolume(_masterVolumeParam, _isMuted ? 0f : _masterVolume);
            ApplyVolume(_musicVolumeParam, _musicVolume);
            ApplyVolume(_sfxVolumeParam, _sfxVolume);
            ApplyVolume(_voiceVolumeParam, _voiceVolume);

            UpdateSlider(_masterSlider, _masterVolume);
            UpdateSlider(_musicSlider, _musicVolume);
            UpdateSlider(_sfxSlider, _sfxVolume);
            UpdateSlider(_voiceSlider, _voiceVolume);
            UpdateSlider(_muteToggle, _isMuted);
        }

        private void ApplyVolume(string paramName, float linearVolume)
        {
            if (_audioMixer != null)
            {
                float db = LinearToDb(linearVolume);
                _audioMixer.SetFloat(paramName, db);
            }
        }

        private float LinearToDb(float linear)
        {
            if (linear <= 0f) return _minVolumeDb;
            return Mathf.Lerp(_minVolumeDb, _maxVolumeDb, linear);
        }

        private float DbToLinear(float db)
        {
            return Mathf.InverseLerp(_minVolumeDb, _maxVolumeDb, db);
        }

        private void UpdateSlider(Slider slider, float value)
        {
            if (slider != null && !Mathf.Approximately(slider.value, value))
            {
                slider.SetValueWithoutNotify(value);
            }
        }

        private void UpdateSlider(Toggle toggle, bool value)
        {
            if (toggle != null && toggle.isOn != value)
            {
                toggle.SetIsOnWithoutNotify(value);
            }
        }

        private void LoadSettings()
        {
            _masterVolume = PlayerPrefs.GetFloat("audio_master_volume", 1f);
            _musicVolume = PlayerPrefs.GetFloat("audio_music_volume", 1f);
            _sfxVolume = PlayerPrefs.GetFloat("audio_sfx_volume", 1f);
            _voiceVolume = PlayerPrefs.GetFloat("audio_voice_volume", 1f);
            _isMuted = PlayerPrefs.GetInt("audio_muted", 0) == 1;
            _muteOnFocusLoss = PlayerPrefs.GetInt("audio_mute_on_focus_loss", 1) == 1;
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetFloat("audio_master_volume", _masterVolume);
            PlayerPrefs.SetFloat("audio_music_volume", _musicVolume);
            PlayerPrefs.SetFloat("audio_sfx_volume", _sfxVolume);
            PlayerPrefs.SetFloat("audio_voice_volume", _voiceVolume);
            PlayerPrefs.SetInt("audio_muted", _isMuted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void RegisterSliders(Slider master, Slider music, Slider sfx, Slider voice, Toggle mute)
        {
            _masterSlider = master;
            _musicSlider = music;
            _sfxSlider = sfx;
            _voiceSlider = voice;
            _muteToggle = mute;
            BindSliders();
        }

        public void ResetToDefaults()
        {
            _masterVolume = 1f;
            _musicVolume = 1f;
            _sfxVolume = 1f;
            _voiceVolume = 1f;
            _isMuted = false;
            _muteOnFocusLoss = true;
            ApplyVolumes();
            SaveSettings();
        }
    }

    public class UISoundPlayer : MonoBehaviour
    {
        [Header("UI Sounds")]
        [SerializeField] private AudioClip _buttonClick;
        [SerializeField] private AudioClip _buttonHover;
        [SerializeField] private AudioClip _screenOpen;
        [SerializeField] private AudioClip _screenClose;
        [SerializeField] private AudioClip _toastShow;
        [SerializeField] private AudioClip _errorSound;
        [SerializeField] private AudioClip _successSound;
        [SerializeField] private AudioClip _cardSelect;
        [SerializeField] private AudioClip _cardDeploy;
        [SerializeField] private AudioClip _elixirTick;
        [SerializeField] private AudioClip _chestUnlock;
        [SerializeField] private AudioClip _victoryFanfare;
        [SerializeField] private AudioClip _defeatSound;

        [Header("Settings")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private bool _useMixerGroup = true;
        [SerializeField] private AudioMixerGroup _sfxMixerGroup;

        private static UISoundPlayer _instance;
        public static UISoundPlayer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<UISoundPlayer>();
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
                if (_audioSource == null)
                {
                    _audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            if (_useMixerGroup && _sfxMixerGroup != null)
            {
                _audioSource.outputAudioMixerGroup = _sfxMixerGroup;
            }

            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;
        }

        public void PlayButtonClick() => PlayClip(_buttonClick);
        public void PlayButtonHover() => PlayClip(_buttonHover);
        public void PlayScreenOpen() => PlayClip(_screenOpen);
        public void PlayScreenClose() => PlayClip(_screenClose);
        public void PlayToast() => PlayClip(_toastShow);
        public void PlayError() => PlayClip(_errorSound);
        public void PlaySuccess() => PlayClip(_successSound);
        public void PlayCardSelect() => PlayClip(_cardSelect);
        public void PlayCardDeploy() => PlayClip(_cardDeploy);
        public void PlayElixirTick() => PlayClip(_elixirTick);
        public void PlayChestUnlock() => PlayClip(_chestUnlock);
        public void PlayVictory() => PlayClip(_victoryFanfare);
        public void PlayDefeat() => PlayClip(_defeatSound);

        public void PlayClip(AudioClip clip, float volume = 1f)
        {
            if (clip == null || _audioSource == null) return;
            if (UIAudioManager.Instance?.IsMuted == true) return;

            _audioSource.PlayOneShot(clip, volume * (UIAudioManager.Instance?.SFXVolume ?? 1f));
        }

        public void PlayClipAtPoint(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;
            if (UIAudioManager.Instance?.IsMuted == true) return;

            AudioSource.PlayClipAtPoint(clip, position, volume * (UIAudioManager.Instance?.SFXVolume ?? 1f));
        }
    }

    public class ButtonSound : MonoBehaviour
    {
        [SerializeField] private AudioClip _clickSound;
        [SerializeField] private AudioClip _hoverSound;
        [SerializeField] private bool _useDefaultSounds = true;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.AddListener(OnClick);
            }
        }

        private void OnClick()
        {
            if (_useDefaultSounds && UISoundPlayer.Instance != null)
            {
                UISoundPlayer.Instance.PlayButtonClick();
            }
            else if (_clickSound != null && UISoundPlayer.Instance != null)
            {
                UISoundPlayer.Instance.PlayClip(_clickSound);
            }
        }

        public void OnPointerEnter()
        {
            if (_useDefaultSounds && UISoundPlayer.Instance != null)
            {
                UISoundPlayer.Instance.PlayButtonHover();
            }
            else if (_hoverSound != null && UISoundPlayer.Instance != null)
            {
                UISoundPlayer.Instance.PlayClip(_hoverSound);
            }
        }
    }
}