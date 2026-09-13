using System;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.Battle.UI;

namespace CRClone.UI.Screens
{
    public class BattleScreen : MonoBehaviour
    {
        [Header("Battle HUD")]
        [SerializeField] private BattleHUD _battleHUD;

        [Header("Pause Menu")]
        [SerializeField] private GameObject _pauseMenu;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _concedeButton;
        [SerializeField] private Button _quitButton;

        [Header("End Battle")]
        [SerializeField] private GameObject _battleEndOverlay;
        [SerializeField] private Button _continueButton;

        private bool _isPaused;
        private bool _battleEnded;

        private void Awake()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            _resumeButton?.onClick.AddListener(OnResumeClicked);
            _settingsButton?.onClick.AddListener(OnSettingsClicked);
            _concedeButton?.onClick.AddListener(OnConcedeClicked);
            _quitButton?.onClick.AddListener(OnQuitClicked);
            _continueButton?.onClick.AddListener(OnContinueClicked);

            if (_pauseMenu != null) _pauseMenu.SetActive(false);
            if (_battleEndOverlay != null) _battleEndOverlay.SetActive(false);
        }

        private void OnEnable()
        {
            _isPaused = false;
            _battleEnded = false;
            Time.timeScale = 1f;
        }

        public void SetBattleHUD(BattleHUD hud)
        {
            _battleHUD = hud;
        }

        public BattleHUD GetBattleHUD()
        {
            return _battleHUD;
        }

        public void ShowPauseMenu()
        {
            if (_battleEnded) return;
            
            _isPaused = true;
            _pauseMenu?.SetActive(true);
            Time.timeScale = 0f;
            UISoundPlayer.Instance?.PlayScreenOpen();
        }

        public void HidePauseMenu()
        {
            _isPaused = false;
            _pauseMenu?.SetActive(false);
            Time.timeScale = 1f;
        }

        public void ShowBattleEnd()
        {
            _battleEnded = true;
            _battleEndOverlay?.SetActive(true);
            Time.timeScale = 1f;
        }

        private void OnResumeClicked()
        {
            HidePauseMenu();
            UISoundPlayer.Instance?.PlayButtonClick();
        }

        private void OnSettingsClicked()
        {
            UISoundPlayer.Instance?.PlayButtonClick();
            UIManager.Instance?.ShowModal(Resources.Load<GameObject>("UI/SettingsModal"));
        }

        private void OnConcedeClicked()
        {
            UISoundPlayer.Instance?.PlayButtonClick();
            UIManager.Instance?.ShowModal(Resources.Load<GameObject>("UI/ConcedeConfirmModal"));
        }

        private void OnQuitClicked()
        {
            UISoundPlayer.Instance?.PlayButtonClick();
            Time.timeScale = 1f;
            Services.Get<GameManager>().ChangeState(GameState.MainMenu);
        }

        private void OnContinueClicked()
        {
            UISoundPlayer.Instance?.PlayButtonClick();
            _battleEndOverlay?.SetActive(false);
            Services.Get<GameManager>().ChangeState(GameState.Lobby);
        }

        public bool IsPaused => _isPaused;
        public bool IsBattleEnded => _battleEnded;
    }
}