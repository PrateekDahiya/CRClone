using System;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;

namespace CRClone.UI.Screens
{
    public class SettingsScreen : MonoBehaviour
    {
        [Header("Tabs")]
        [SerializeField] private Button _graphicsTab;
        [SerializeField] private Button _audioTab;
        [SerializeField] private Button _gameplayTab;
        [SerializeField] private Button _privacyTab;
        [SerializeField] private Button _accessibilityTab;

        [Header("Tab Content")]
        [SerializeField] private Transform _graphicsContent;
        [SerializeField] private Transform _audioContent;
        [SerializeField] private Transform _gameplayContent;
        [SerializeField] private Transform _privacyContent;
        [SerializeField] private Transform _accessibilityContent;

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
        [SerializeField] private Toggle _damageNumbersToggle;

        [Header("Privacy Settings")]
        [SerializeField] private Toggle _showProfileToggle;
        [SerializeField] private Toggle _showOnlineStatusToggle;
        [SerializeField] private Toggle _allowFriendRequestsToggle;
        [SerializeField] private Toggle _allowClanInvitesToggle;
        [SerializeField] private Button _deleteAccountButton;

        [Header("Accessibility Settings")]
        [SerializeField] private Toggle _highContrastToggle;
        [SerializeField] private Toggle _reduceMotionToggle;
        [SerializeField] private Slider _textScaleSlider;
        [SerializeField] private Dropdown _colorBlindDropdown;
        [SerializeField] private Button _resetAccessibilityButton;

        [Header("Action Buttons")]
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _resetToDefaultsButton;

        private SettingsTab _currentTab = SettingsTab.Graphics;

        public enum SettingsTab
        {
            Graphics,
            Audio,
            Gameplay,
            Privacy,
            Accessibility
        }

        private void Awake()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            _backButton?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.MainMenu));
            _resetToDefaultsButton?.onClick.AddListener(ResetToDefaults);

            _graphicsTab?.onClick.AddListener(() => SwitchTab(SettingsTab.Graphics));
            _audioTab?.onClick.AddListener(() => SwitchTab(SettingsTab.Audio));
            _gameplayTab?.onClick.AddListener(() => SwitchTab(SettingsTab.Gameplay));
            _privacyTab?.onClick.AddListener(() => SwitchTab(SettingsTab.Privacy));
            _accessibilityTab?.onClick.AddListener(() => SwitchTab(SettingsTab.Accessibility));

            SetupGraphicsSettings();
            SetupAudioSettings();
            SetupGameplaySettings();
            SetupPrivacySettings();
            SetupAccessibilitySettings();

            LoadSettings();
            SwitchTab(SettingsTab.Graphics);
        }

        private void SetupGraphicsSettings()
        {
            if (_graphicsQualityDropdown != null)
            {
                _graphicsQualityDropdown.ClearOptions();
                _graphicsQualityDropdown.AddOptions(new System.Collections.Generic.List<string> 
                { "Low", "Medium", "High", "Ultra" });
                _graphicsQualityDropdown.onValueChanged.AddListener(OnGraphicsQualityChanged);
            }

            if (_fpsCapDropdown != null)
            {
                _fpsCapDropdown.ClearOptions();
                _fpsCapDropdown.AddOptions(new System.Collections.Generic.List<string> 
                { "30 FPS", "60 FPS", "120 FPS", "Unlimited" });
                _fpsCapDropdown.onValueChanged.AddListener(OnFpsCapChanged);
            }

            if (_vSyncToggle != null)
            {
                _vSyncToggle.onValueChanged.AddListener(OnVSyncChanged);
            }

            if (_resolutionDropdown != null)
            {
                _resolutionDropdown.ClearOptions();
                var resolutions = Screen.resolutions;
                var options = new System.Collections.Generic.List<string>();
                foreach (var res in resolutions)
                {
                    options.Add($"{res.width}x{res.height} @{res.refreshRate}Hz");
                }
                if (options.Count == 0) options.Add("1920x1080 @60Hz");
                _resolutionDropdown.AddOptions(options);
                _resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            }

            if (_fullscreenDropdown != null)
            {
                _fullscreenDropdown.ClearOptions();
                _fullscreenDropdown.AddOptions(new System.Collections.Generic.List<string> 
                { "Windowed", "Borderless", "Fullscreen" });
                _fullscreenDropdown.onValueChanged.AddListener(OnFullscreenChanged);
            }
        }

        private void SetupAudioSettings()
        {
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }
            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }
            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            }
            if (_voiceVolumeSlider != null)
            {
                _voiceVolumeSlider.onValueChanged.AddListener(OnVoiceVolumeChanged);
            }
            if (_muteToggle != null)
            {
                _muteToggle.onValueChanged.AddListener(OnMuteChanged);
            }
            if (_muteOnFocusLossToggle != null)
            {
                _muteOnFocusLossToggle.onValueChanged.AddListener(OnMuteOnFocusLossChanged);
            }
        }

        private void SetupGameplaySettings()
        {
            if (_deployModeDropdown != null)
            {
                _deployModeDropdown.ClearOptions();
                _deployModeDropdown.AddOptions(new System.Collections.Generic.List<string> 
                { "Tap to Place", "Drag to Place", "Both" });
                _deployModeDropdown.onValueChanged.AddListener(OnDeployModeChanged);
            }

            if (_spellAimingDropdown != null)
            {
                _spellAimingDropdown.ClearOptions();
                _spellAimingDropdown.AddOptions(new System.Collections.Generic.List<string> 
                { "Cursor", "Drag", "Hybrid" });
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

            if (_damageNumbersToggle != null)
            {
                _damageNumbersToggle.onValueChanged.AddListener(OnDamageNumbersChanged);
            }
        }

        private void SetupPrivacySettings()
        {
            if (_showProfileToggle != null)
            {
                _showProfileToggle.onValueChanged.AddListener(OnShowProfileChanged);
            }
            if (_showOnlineStatusToggle != null)
            {
                _showOnlineStatusToggle.onValueChanged.AddListener(OnShowOnlineStatusChanged);
            }
            if (_allowFriendRequestsToggle != null)
            {
                _allowFriendRequestsToggle.onValueChanged.AddListener(OnAllowFriendRequestsChanged);
            }
            if (_allowClanInvitesToggle != null)
            {
                _allowClanInvitesToggle.onValueChanged.AddListener(OnAllowClanInvitesChanged);
            }
            if (_deleteAccountButton != null)
            {
                _deleteAccountButton.onClick.AddListener(OnDeleteAccount);
            }
        }

        private void SetupAccessibilitySettings()
        {
            if (_highContrastToggle != null)
            {
                _highContrastToggle.onValueChanged.AddListener(OnHighContrastChanged);
            }
            if (_reduceMotionToggle != null)
            {
                _reduceMotionToggle.onValueChanged.AddListener(OnReduceMotionChanged);
            }
            if (_textScaleSlider != null)
            {
                _textScaleSlider.onValueChanged.AddListener(OnTextScaleChanged);
            }
            if (_colorBlindDropdown != null)
            {
                _colorBlindDropdown.ClearOptions();
                _colorBlindDropdown.AddOptions(new System.Collections.Generic.List<string> 
                { "None", "Protanopia", "Deuteranopia", "Tritanopia" });
                _colorBlindDropdown.onValueChanged.AddListener(OnColorBlindChanged);
            }
            if (_resetAccessibilityButton != null)
            {
                _resetAccessibilityButton.onClick.AddListener(ResetAccessibility);
            }
        }

        private void SwitchTab(SettingsTab tab)
        {
            _currentTab = tab;
            UpdateTabVisuals();
            ShowTabContent(tab);
        }

        private void UpdateTabVisuals()
        {
            SetTabSelected(_graphicsTab, _currentTab == SettingsTab.Graphics);
            SetTabSelected(_audioTab, _currentTab == SettingsTab.Audio);
            SetTabSelected(_gameplayTab, _currentTab == SettingsTab.Gameplay);
            SetTabSelected(_privacyTab, _currentTab == SettingsTab.Privacy);
            SetTabSelected(_accessibilityTab, _currentTab == SettingsTab.Accessibility);
        }

        private void SetTabSelected(Button button, bool selected)
        {
            if (button == null) return;
            var colors = button.colors;
            colors.normalColor = selected ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            button.colors = colors;
        }

        private void ShowTabContent(SettingsTab tab)
        {
            _graphicsContent?.gameObject.SetActive(tab == SettingsTab.Graphics);
            _audioContent?.gameObject.SetActive(tab == SettingsTab.Audio);
            _gameplayContent?.gameObject.SetActive(tab == SettingsTab.Gameplay);
            _privacyContent?.gameObject.SetActive(tab == SettingsTab.Privacy);
            _accessibilityContent?.gameObject.SetActive(tab == SettingsTab.Accessibility);
        }

        private void LoadSettings()
        {
            // Graphics
            _graphicsQualityDropdown?.SetValueWithoutNotify(PlayerPrefs.GetInt("graphics_quality", 2));
            _fpsCapDropdown?.SetValueWithoutNotify(PlayerPrefs.GetInt("fps_cap", 1));
            _vSyncToggle?.SetIsOnWithoutNotify(PlayerPrefs.GetInt("vsync", 1) == 1);
            _resolutionDropdown?.SetValueWithoutNotify(PlayerPrefs.GetInt("resolution", 0));
            _fullscreenDropdown?.SetValueWithoutNotify(PlayerPrefs.GetInt("fullscreen", 1));

            // Audio
            _masterVolumeSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat("master_volume", 1f));
            _musicVolumeSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat("music_volume", 1f));
            _sfxVolumeSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat("sfx_volume", 1f));
            _voiceVolumeSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat("voice_volume", 1f));
            _muteToggle?.SetIsOnWithoutNotify(PlayerPrefs.GetInt("mute", 0) == 1);
            _muteOnFocusLossToggle?.SetIsOnWithoutNotify(PlayerPrefs.GetInt("mute_focus_loss", 1) == 1);

            // Gameplay
            _deployModeDropdown?.SetValueWithoutNotify(PlayerPrefs.GetInt("deploy_mode", 2));
            _spellAimingDropdown?.SetValueWithoutNotify(PlayerPrefs.GetInt("spell_aiming", 2));
            _autoTargetToggle?.SetIsOnWithoutNotify(PlayerPrefs.GetInt("auto_target", 1) == 1);
            _leftHandedToggle?.SetIsOnWithoutNotify(PlayerPrefs.GetInt("left_handed", 0) == 1);
            _cameraShakeToggle?.SetIsOnWithoutNotify(PlayerPrefs.GetInt("camera_shake", 1) == 1);
            _damageNumbersToggle?.SetIsOnWithoutNotify(PlayerPrefs.GetInt("damage_numbers", 1) == 1);

            // Privacy
            _showProfileToggle?.SetIsOnWithoutNotify(PlayerPrefs.GetInt("show_profile", 1) == 1);
            _showOnlineStatusToggle?.SetIsOnWithoutNotify(PlayerPrefs.GetInt("show_online", 1) == 1);
            _allowFriendRequestsToggle?.SetIsOnWithoutNotify(PlayerPrefs.GetInt("allow_friends", 1) == 1);
            _allowClanInvitesToggle?.SetIsOnWithoutNotify(PlayerPrefs.GetInt("allow_clan_invites", 1) == 1);

            // Accessibility
            _highContrastToggle?.SetIsOnWithoutNotify(PlayerPrefs.GetInt("high_contrast", 0) == 1);
            _reduceMotionToggle?.SetIsOnWithoutNotify(PlayerPrefs.GetInt("reduce_motion", 0) == 1);
            _textScaleSlider?.SetValueWithoutNotify(PlayerPrefs.GetFloat("text_scale", 1f));
            _colorBlindDropdown?.SetValueWithoutNotify(PlayerPrefs.GetInt("color_blind", 0));

            ApplySettings();
        }

        private void ApplySettings()
        {
            ApplyGraphicsSettings();
            ApplyAudioSettings();
            ApplyGameplaySettings();
            ApplyPrivacySettings();
            ApplyAccessibilitySettings();
        }

        private void ApplyGraphicsSettings()
        {
            QualitySettings.SetQualityLevel(_graphicsQualityDropdown?.value ?? 2);
            
            int[] fpsValues = { 30, 60, 120, -1 };
            Application.targetFrameRate = fpsValues[_fpsCapDropdown?.value ?? 1];
            
            QualitySettings.vSyncCount = _vSyncToggle?.isOn == true ? 1 : 0;

            FullScreenMode[] modes = { FullScreenMode.Windowed, FullScreenMode.FullScreenWindow, FullScreenMode.ExclusiveFullScreen };
            Screen.fullScreenMode = modes[_fullscreenDropdown?.value ?? 1];
        }

        private void ApplyAudioSettings()
        {
            if (AudioListener.volume != (_masterVolumeSlider?.value ?? 1f))
            {
                AudioListener.volume = _masterVolumeSlider?.value ?? 1f;
            }
            
            if (_muteToggle?.isOn == true)
            {
                AudioListener.volume = 0f;
            }
        }

        private void ApplyGameplaySettings()
        {
            // Settings applied via InputManager and other systems
        }

        private void ApplyPrivacySettings()
        {
            // Sent to server
        }

        private void ApplyAccessibilitySettings()
        {
            if (AccessibilityManager.Instance != null)
            {
                AccessibilityManager.Instance.SetHighContrastMode(_highContrastToggle?.isOn ?? false);
                AccessibilityManager.Instance.SetReduceMotion(_reduceMotionToggle?.isOn ?? false);
                AccessibilityManager.Instance.SetTextScale(_textScaleSlider?.value ?? 1f);
                AccessibilityManager.Instance.SetColorBlindMode((AccessibilityManager.ColorBlindMode)(_colorBlindDropdown?.value ?? 0));
            }
        }

        // Event Handlers
        private void OnGraphicsQualityChanged(int value)
        {
            PlayerPrefs.SetInt("graphics_quality", value);
            QualitySettings.SetQualityLevel(value);
        }

        private void OnFpsCapChanged(int value)
        {
            PlayerPrefs.SetInt("fps_cap", value);
            int[] fpsValues = { 30, 60, 120, -1 };
            Application.targetFrameRate = fpsValues[value];
        }

        private void OnVSyncChanged(bool value)
        {
            PlayerPrefs.SetInt("vsync", value ? 1 : 0);
            QualitySettings.vSyncCount = value ? 1 : 0;
        }

        private void OnResolutionChanged(int value)
        {
            PlayerPrefs.SetInt("resolution", value);
        }

        private void OnFullscreenChanged(int value)
        {
            PlayerPrefs.SetInt("fullscreen", value);
            FullScreenMode[] modes = { FullScreenMode.Windowed, FullScreenMode.FullScreenWindow, FullScreenMode.ExclusiveFullScreen };
            Screen.fullScreenMode = modes[value];
        }

        private void OnMasterVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("master_volume", value);
            AudioListener.volume = value;
        }

        private void OnMusicVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("music_volume", value);
        }

        private void OnSfxVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("sfx_volume", value);
        }

        private void OnVoiceVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("voice_volume", value);
        }

        private void OnMuteChanged(bool value)
        {
            PlayerPrefs.SetInt("mute", value ? 1 : 0);
            AudioListener.volume = value ? 0f : (_masterVolumeSlider?.value ?? 1f);
        }

        private void OnMuteOnFocusLossChanged(bool value)
        {
            PlayerPrefs.SetInt("mute_focus_loss", value ? 1 : 0);
        }

        private void OnDeployModeChanged(int value)
        {
            PlayerPrefs.SetInt("deploy_mode", value);
            if (InputManager.Instance != null)
            {
                InputManager.Instance.SetDeployMode((InputManager.DeployMode)value);
            }
        }

        private void OnSpellAimingChanged(int value)
        {
            PlayerPrefs.SetInt("spell_aiming", value);
        }

        private void OnAutoTargetChanged(bool value)
        {
            PlayerPrefs.SetInt("auto_target", value ? 1 : 0);
        }

        private void OnLeftHandedChanged(bool value)
        {
            PlayerPrefs.SetInt("left_handed", value ? 1 : 0);
        }

        private void OnCameraShakeChanged(bool value)
        {
            PlayerPrefs.SetInt("camera_shake", value ? 1 : 0);
        }

        private void OnDamageNumbersChanged(bool value)
        {
            PlayerPrefs.SetInt("damage_numbers", value ? 1 : 0);
        }

        private void OnShowProfileChanged(bool value)
        {
            PlayerPrefs.SetInt("show_profile", value ? 1 : 0);
        }

        private void OnShowOnlineStatusChanged(bool value)
        {
            PlayerPrefs.SetInt("show_online", value ? 1 : 0);
        }

        private void OnAllowFriendRequestsChanged(bool value)
        {
            PlayerPrefs.SetInt("allow_friends", value ? 1 : 0);
        }

        private void OnAllowClanInvitesChanged(bool value)
        {
            PlayerPrefs.SetInt("allow_clan_invites", value ? 1 : 0);
        }

        private void OnDeleteAccount()
        {
            var confirmModal = UIManager.Instance?.ShowModal(Resources.Load<GameObject>("UI/DeleteAccountConfirmModal"));
        }

        private void OnHighContrastChanged(bool value)
        {
            PlayerPrefs.SetInt("high_contrast", value ? 1 : 0);
            if (AccessibilityManager.Instance != null)
            {
                AccessibilityManager.Instance.SetHighContrastMode(value);
            }
        }

        private void OnReduceMotionChanged(bool value)
        {
            PlayerPrefs.SetInt("reduce_motion", value ? 1 : 0);
            if (AccessibilityManager.Instance != null)
            {
                AccessibilityManager.Instance.SetReduceMotion(value);
            }
        }

        private void OnTextScaleChanged(float value)
        {
            PlayerPrefs.SetFloat("text_scale", value);
            if (AccessibilityManager.Instance != null)
            {
                AccessibilityManager.Instance.SetTextScale(value);
            }
        }

        private void OnColorBlindChanged(int value)
        {
            PlayerPrefs.SetInt("color_blind", value);
            if (AccessibilityManager.Instance != null)
            {
                AccessibilityManager.Instance.SetColorBlindMode((AccessibilityManager.ColorBlindMode)value);
            }
        }

        private void ResetAccessibility()
        {
            _highContrastToggle?.SetIsOnWithoutNotify(false);
            _reduceMotionToggle?.SetIsOnWithoutNotify(false);
            _textScaleSlider?.SetValueWithoutNotify(1f);
            _colorBlindDropdown?.SetValueWithoutNotify(0);

            OnHighContrastChanged(false);
            OnReduceMotionChanged(false);
            OnTextScaleChanged(1f);
            OnColorBlindChanged(0);

            EventBus.RaiseToast("Accessibility settings reset!");
        }

        private void ResetToDefaults()
        {
            ResetAccessibility();

            // Reset graphics
            _graphicsQualityDropdown?.SetValueWithoutNotify(2);
            _fpsCapDropdown?.SetValueWithoutNotify(1);
            _vSyncToggle?.SetIsOnWithoutNotify(true);
            _fullscreenDropdown?.SetValueWithoutNotify(1);

            // Reset audio
            _masterVolumeSlider?.SetValueWithoutNotify(1f);
            _musicVolumeSlider?.SetValueWithoutNotify(1f);
            _sfxVolumeSlider?.SetValueWithoutNotify(1f);
            _voiceVolumeSlider?.SetValueWithoutNotify(1f);
            _muteToggle?.SetIsOnWithoutNotify(false);
            _muteOnFocusLossToggle?.SetIsOnWithoutNotify(true);

            // Reset gameplay
            _deployModeDropdown?.SetValueWithoutNotify(2);
            _spellAimingDropdown?.SetValueWithoutNotify(2);
            _autoTargetToggle?.SetIsOnWithoutNotify(true);
            _leftHandedToggle?.SetIsOnWithoutNotify(false);
            _cameraShakeToggle?.SetIsOnWithoutNotify(true);
            _damageNumbersToggle?.SetIsOnWithoutNotify(true);

            // Reset privacy
            _showProfileToggle?.SetIsOnWithoutNotify(true);
            _showOnlineStatusToggle?.SetIsOnWithoutNotify(true);
            _allowFriendRequestsToggle?.SetIsOnWithoutNotify(true);
            _allowClanInvitesToggle?.SetIsOnWithoutNotify(true);

            LoadSettings();
            EventBus.RaiseToast("All settings reset to defaults!");
        }
    }
}