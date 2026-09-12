using System;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.UI.Animation;

namespace CRClone.UI.Screens
{
    public class SettingsScreen : MonoBehaviour
    {
        [Header("Tabs")]
        [SerializeField] private Button _graphicsTab;
        [SerializeField] private Button _audioTab;
        [SerializeField] private Button _gameplayTab;
        [SerializeField] private Button _privacyTab;
        [SerializeField] private Button _notificationsTab;

        [Header("Tab Content")]
        [SerializeField] private Transform _graphicsContent;
        [SerializeField] private Transform _audioContent;
        [SerializeField] private Transform _gameplayContent;
        [SerializeField] private Transform _privacyContent;
        [SerializeField] private Transform _notificationsContent;

        [Header("Graphics Settings")]
        [SerializeField] private Dropdown _graphicsQualityDropdown;
        [SerializeField] private Dropdown _fpsCapDropdown;
        [SerializeField] private Toggle _vSyncToggle;
        [SerializeField] private Dropdown _resolutionDropdown;
        [SerializeField] private Dropdown _fullscreenDropdown;

        [Header("Audio Settings")]
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;
        [SerializeField] private Slider _voiceVolumeSlider;
        [SerializeField] private Toggle _muteToggle;
        [SerializeField] private Toggle _muteOnFocusLossToggle;

        [Header("Gameplay Settings")]
        [SerializeField] private Dropdown _deployModeDropdown;
        [SerializeField] private Dropdown _spellAimingDropdown;
        [SerializeField] private Toggle _autoTargetToggle;
        [SerializeField] private Toggle _leftHandedToggle;
        [SerializeField] private Toggle _cameraShakeToggle;
        [SerializeField] private Dropdown _damageNumbersDropdown;

        [Header("Accessibility")]
        [SerializeField] private Toggle _highContrastToggle;
        [SerializeField] private Toggle _reduceMotionToggle;
        [SerializeField] private Slider _textScaleSlider;
        [SerializeField] private Dropdown _colorBlindDropdown;

        [Header("Privacy")]
        [SerializeField] private Toggle _showProfileToggle;
        [SerializeField] private Toggle _showOnlineStatusToggle;
        [SerializeField] private Toggle _allowFriendRequestsToggle;
        [SerializeField] private Toggle _allowClanInvitesToggle;
        [SerializeField] private Button _deleteAccountButton;

        [Header("Notifications")]
        [SerializeField] private Toggle _pushNotificationsToggle;
        [SerializeField] private Toggle _battleNotificationsToggle;
        [SerializeField] private Toggle _clanNotificationsToggle;
        [SerializeField] private Toggle _shopNotificationsToggle;
        [SerializeField] private Toggle _eventNotificationsToggle;

        [Header("Actions")]
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _resetToDefaultsButton;

        private SettingsTab _currentTab = SettingsTab.Graphics;

        public enum SettingsTab
        {
            Graphics,
            Audio,
            Gameplay,
            Privacy,
            Notifications
        }

        public void Initialize()
        {
            _backButton?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.MainMenu));
            _resetToDefaultsButton?.onClick.AddListener(ResetToDefaults);

            SetupTabs();
            LoadSettings();
            BindControls();
            ShowTab(SettingsTab.Graphics);
        }

        private void SetupTabs()
        {
            _graphicsTab?.onClick.AddListener(() => ShowTab(SettingsTab.Graphics));
            _audioTab?.onClick.AddListener(() => ShowTab(SettingsTab.Audio));
            _gameplayTab?.onClick.AddListener(() => ShowTab(SettingsTab.Gameplay));
            _privacyTab?.onClick.AddListener(() => ShowTab(SettingsTab.Privacy));
            _notificationsTab?.onClick.AddListener(() => ShowTab(SettingsTab.Notifications));
        }

        private void ShowTab(SettingsTab tab)
        {
            _currentTab = tab;

            _graphicsContent?.gameObject.SetActive(tab == SettingsTab.Graphics);
            _audioContent?.gameObject.SetActive(tab == SettingsTab.Audio);
            _gameplayContent?.gameObject.SetActive(tab == SettingsTab.Gameplay);
            _privacyContent?.gameObject.SetActive(tab == SettingsTab.Privacy);
            _notificationsContent?.gameObject.SetActive(tab == SettingsTab.Notifications);

            UpdateTabButtons();
        }

        private void UpdateTabButtons()
        {
            SetTabSelected(_graphicsTab, _currentTab == SettingsTab.Graphics);
            SetTabSelected(_audioTab, _currentTab == SettingsTab.Audio);
            SetTabSelected(_gameplayTab, _currentTab == SettingsTab.Gameplay);
            SetTabSelected(_privacyTab, _currentTab == SettingsTab.Privacy);
            SetTabSelected(_notificationsTab, _currentTab == SettingsTab.Notifications);
        }

        private void SetTabSelected(Button button, bool selected)
        {
            if (button == null) return;
            var colors = button.colors;
            colors.normalColor = selected ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            button.colors = colors;
        }

        private void LoadSettings()
        {
            if (_graphicsQualityDropdown != null)
            {
                _graphicsQualityDropdown.value = PlayerPrefs.GetInt("graphics_quality", 2);
            }
            if (_fpsCapDropdown != null)
            {
                _fpsCapDropdown.value = PlayerPrefs.GetInt("fps_cap", 1);
            }
            if (_vSyncToggle != null)
            {
                _vSyncToggle.isOn = PlayerPrefs.GetInt("vsync", 1) == 1;
            }
            if (_resolutionDropdown != null)
            {
                _resolutionDropdown.value = PlayerPrefs.GetInt("resolution", 0);
            }
            if (_fullscreenDropdown != null)
            {
                _fullscreenDropdown.value = PlayerPrefs.GetInt("fullscreen", 1);
            }

            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.value = PlayerPrefs.GetFloat("audio_master", 1f);
            }
            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.value = PlayerPrefs.GetFloat("audio_music", 1f);
            }
            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.value = PlayerPrefs.GetFloat("audio_sfx", 1f);
            }
            if (_voiceVolumeSlider != null)
            {
                _voiceVolumeSlider.value = PlayerPrefs.GetFloat("audio_voice", 1f);
            }
            if (_muteToggle != null)
            {
                _muteToggle.isOn = PlayerPrefs.GetInt("audio_mute", 0) == 1;
            }
            if (_muteOnFocusLossToggle != null)
            {
                _muteOnFocusLossToggle.isOn = PlayerPrefs.GetInt("audio_mute_focus", 1) == 1;
            }

            if (_deployModeDropdown != null)
            {
                _deployModeDropdown.value = PlayerPrefs.GetInt("deploy_mode", 0);
            }
            if (_spellAimingDropdown != null)
            {
                _spellAimingDropdown.value = PlayerPrefs.GetInt("spell_aiming", 0);
            }
            if (_autoTargetToggle != null)
            {
                _autoTargetToggle.isOn = PlayerPrefs.GetInt("auto_target", 0) == 1;
            }
            if (_leftHandedToggle != null)
            {
                _leftHandedToggle.isOn = PlayerPrefs.GetInt("left_handed", 0) == 1;
            }
            if (_cameraShakeToggle != null)
            {
                _cameraShakeToggle.isOn = PlayerPrefs.GetInt("camera_shake", 1) == 1;
            }
            if (_damageNumbersDropdown != null)
            {
                _damageNumbersDropdown.value = PlayerPrefs.GetInt("damage_numbers", 0);
            }

            if (_highContrastToggle != null)
            {
                _highContrastToggle.isOn = PlayerPrefs.GetInt("accessibility_high_contrast", 0) == 1;
            }
            if (_reduceMotionToggle != null)
            {
                _reduceMotionToggle.isOn = PlayerPrefs.GetInt("accessibility_reduce_motion", 0) == 1;
            }
            if (_textScaleSlider != null)
            {
                _textScaleSlider.value = PlayerPrefs.GetFloat("accessibility_text_scale", 1f);
            }
            if (_colorBlindDropdown != null)
            {
                _colorBlindDropdown.value = PlayerPrefs.GetInt("accessibility_color_blind", 0);
            }

            if (_showProfileToggle != null)
            {
                _showProfileToggle.isOn = PlayerPrefs.GetInt("privacy_show_profile", 1) == 1;
            }
            if (_showOnlineStatusToggle != null)
            {
                _showOnlineStatusToggle.isOn = PlayerPrefs.GetInt("privacy_show_online", 1) == 1;
            }
            if (_allowFriendRequestsToggle != null)
            {
                _allowFriendRequestsToggle.isOn = PlayerPrefs.GetInt("privacy_friends", 1) == 1;
            }
            if (_allowClanInvitesToggle != null)
            {
                _allowClanInvitesToggle.isOn = PlayerPrefs.GetInt("privacy_clan_invites", 1) == 1;
            }

            if (_pushNotificationsToggle != null)
            {
                _pushNotificationsToggle.isOn = PlayerPrefs.GetInt("notif_push", 1) == 1;
            }
            if (_battleNotificationsToggle != null)
            {
                _battleNotificationsToggle.isOn = PlayerPrefs.GetInt("notif_battle", 1) == 1;
            }
            if (_clanNotificationsToggle != null)
            {
                _clanNotificationsToggle.isOn = PlayerPrefs.GetInt("notif_clan", 1) == 1;
            }
            if (_shopNotificationsToggle != null)
            {
                _shopNotificationsToggle.isOn = PlayerPrefs.GetInt("notif_shop", 1) == 1;
            }
            if (_eventNotificationsToggle != null)
            {
                _eventNotificationsToggle.isOn = PlayerPrefs.GetInt("notif_events", 1) == 1;
            }
        }

        private void BindControls()
        {
            if (_graphicsQualityDropdown != null)
            {
                _graphicsQualityDropdown.onValueChanged.AddListener(OnGraphicsQualityChanged);
            }
            if (_fpsCapDropdown != null)
            {
                _fpsCapDropdown.onValueChanged.AddListener(OnFpsCapChanged);
            }
            if (_vSyncToggle != null)
            {
                _vSyncToggle.onValueChanged.AddListener(OnVSyncChanged);
            }
            if (_resolutionDropdown != null)
            {
                _resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            }
            if (_fullscreenDropdown != null)
            {
                _fullscreenDropdown.onValueChanged.AddListener(OnFullscreenChanged);
            }

            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.onValueChanged.AddListener(UIAudioManager.Instance?.SetMasterVolume);
            }
            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.onValueChanged.AddListener(UIAudioManager.Instance?.SetMusicVolume);
            }
            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.onValueChanged.AddListener(UIAudioManager.Instance?.SetSFXVolume);
            }
            if (_voiceVolumeSlider != null)
            {
                _voiceVolumeSlider.onValueChanged.AddListener(UIAudioManager.Instance?.SetVoiceVolume);
            }
            if (_muteToggle != null)
            {
                _muteToggle.onValueChanged.AddListener(UIAudioManager.Instance?.SetMute);
            }
            if (_muteOnFocusLossToggle != null)
            {
                _muteOnFocusLossToggle.onValueChanged.AddListener(UIAudioManager.Instance?.SetMuteOnFocusLoss);
            }

            if (_deployModeDropdown != null)
            {
                _deployModeDropdown.onValueChanged.AddListener(OnDeployModeChanged);
            }
            if (_spellAimingDropdown != null)
            {
                _spellAimingDropdown.onValueChanged.AddListener(OnSpellAimingChanged);
            }
            if (_autoTargetToggle != null)
            {
                _autoTargetToggle.onValueChanged.AddListener(OnAutoTargetChanged);
            }
            if (_leftHandedToggle != null)
            {
                _leftHandedToggle.onValueChanged.AddListener(OnLeftHandedChanged);
            }
            if (_cameraShakeToggle != null)
            {
                _cameraShakeToggle.onValueChanged.AddListener(OnCameraShakeChanged);
            }
            if (_damageNumbersDropdown != null)
            {
                _damageNumbersDropdown.onValueChanged.AddListener(OnDamageNumbersChanged);
            }

            if (_highContrastToggle != null)
            {
                _highContrastToggle.onValueChanged.AddListener(AccessibilityManager.Instance?.SetHighContrastMode);
            }
            if (_reduceMotionToggle != null)
            {
                _reduceMotionToggle.onValueChanged.AddListener(AccessibilityManager.Instance?.SetReduceMotion);
            }
            if (_textScaleSlider != null)
            {
                _textScaleSlider.onValueChanged.AddListener(AccessibilityManager.Instance?.SetTextScale);
            }
            if (_colorBlindDropdown != null)
            {
                _colorBlindDropdown.onValueChanged.AddListener(OnColorBlindChanged);
            }
        }

        private void OnGraphicsQualityChanged(int value)
        {
            PlayerPrefs.SetInt("graphics_quality", value);
            QualitySettings.SetQualityLevel(value);
            PlayerPrefs.Save();
        }

        private void OnFpsCapChanged(int value)
        {
            int[] fpsValues = { 30, 60, 120, -1 };
            int targetFps = fpsValues[value];
            Application.targetFrameRate = targetFps;
            PlayerPrefs.SetInt("fps_cap", value);
            PlayerPrefs.Save();
        }

        private void OnVSyncChanged(bool value)
        {
            QualitySettings.vSyncCount = value ? 1 : 0;
            PlayerPrefs.SetInt("vsync", value ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void OnResolutionChanged(int value)
        {
            PlayerPrefs.SetInt("resolution", value);
            PlayerPrefs.Save();
        }

        private void OnFullscreenChanged(int value)
        {
            FullScreenMode mode = value switch
            {
                0 => FullScreenMode.Windowed,
                1 => FullScreenMode.FullScreenWindow,
                2 => FullScreenMode.ExclusiveFullScreen,
                _ => FullScreenMode.FullScreenWindow
            };
            Screen.fullScreenMode = mode;
            PlayerPrefs.SetInt("fullscreen", value);
            PlayerPrefs.Save();
        }

        private void OnDeployModeChanged(int value)
        {
            PlayerPrefs.SetInt("deploy_mode", value);
            PlayerPrefs.Save();
            InputManager.Instance?.SetDeployMode((InputManager.DeployMode)value);
        }

        private void OnSpellAimingChanged(int value)
        {
            PlayerPrefs.SetInt("spell_aiming", value);
            PlayerPrefs.Save();
        }

        private void OnAutoTargetChanged(bool value)
        {
            PlayerPrefs.SetInt("auto_target", value ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void OnLeftHandedChanged(bool value)
        {
            PlayerPrefs.SetInt("left_handed", value ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void OnCameraShakeChanged(bool value)
        {
            PlayerPrefs.SetInt("camera_shake", value ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void OnDamageNumbersChanged(int value)
        {
            PlayerPrefs.SetInt("damage_numbers", value);
            PlayerPrefs.Save();
        }

        private void OnColorBlindChanged(int value)
        {
            AccessibilityManager.Instance?.SetColorBlindMode((AccessibilityManager.ColorBlindMode)value);
        }

        private void ResetToDefaults()
        {
            UIAudioManager.Instance?.ResetToDefaults();
            AccessibilityManager.Instance?.ResetToDefaults();
            InputManager.Instance?.ResetAllBindings();

            LoadSettings();
            EventBus.RaiseToast("Settings reset to defaults!");
        }
    }
}