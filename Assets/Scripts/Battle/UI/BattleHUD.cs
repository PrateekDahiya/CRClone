using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.Battle.Simulation;

namespace CRClone.Battle.UI
{
    public class BattleHUD : MonoBehaviour
    {
        [Header("Timer")]
        [SerializeField] private Text _timerText;
        [SerializeField] private Image _timerBackground;

        [Header("Crowns")]
        [SerializeField] private Image[] _p1Crowns = new Image[3];
        [SerializeField] private Image[] _p2Crowns = new Image[3];
        [SerializeField] private Sprite _crownFilled;
        [SerializeField] private Sprite _crownEmpty;

        [Header("Player Names")]
        [SerializeField] private Text _p1NameText;
        [SerializeField] private Text _p2NameText;

        [Header("Pause")]
        [SerializeField] private Button _pauseButton;
        [SerializeField] private GameObject _pauseMenu;

        private BattleSimulation _simulation;

        private void Awake()
        {
            _simulation = Services.Get<GameManager>().BattleSim;
            _pauseButton?.onClick.AddListener(OnPauseClicked);
        }

        private void Update()
        {
            if (_simulation == null) return;

            UpdateTimer();
            UpdateCrowns();
        }

        private void UpdateTimer()
        {
            float remaining = Mathf.Max(0, _simulation._config.battleDuration - _simulation.CurrentTick * BattleSimulation.FIXED_DT);
            bool overtime = remaining <= 0;

            if (overtime)
            {
                remaining = Mathf.Max(0, _simulation._config.overtimeDuration - (_simulation.CurrentTick * BattleSimulation.FIXED_DT - _simulation._config.battleDuration));
            }

            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);

            if (_timerText != null)
            {
                _timerText.text = $"{minutes:00}:{seconds:00}";
                _timerText.color = overtime ? Color.red : Color.white;
            }

            if (_timerBackground != null)
            {
                _timerBackground.color = overtime ? new Color(1f, 0.3f, 0.3f) : Color.black;
            }
        }

        private void UpdateCrowns()
        {
            int p1Crowns = 0, p2Crowns = 0;

            foreach (var tower in _simulation.Towers)
            {
                if (tower.Type != TowerType.King && tower.IsDead)
                {
                    if (tower.OwnerPlayerId == 1) p1Crowns++;
                    else p2Crowns++;
                }
            }

            for (int i = 0; i < 3; i++)
            {
                if (i < _p1Crowns.Length)
                    _p1Crowns[i].sprite = i < p2Crowns ? _crownFilled : _crownEmpty; // p2 crowns = p1 towers destroyed
                
                if (i < _p2Crowns.Length)
                    _p2Crowns[i].sprite = i < p1Crowns ? _crownFilled : _crownEmpty; // p1 crowns = p2 towers destroyed
            }
        }

        private void OnPauseClicked()
        {
            _pauseMenu.SetActive(true);
            Time.timeScale = 0f;
        }

        public void OnResumeClicked()
        {
            _pauseMenu.SetActive(false);
            Time.timeScale = 1f;
        }

        public void OnQuitClicked()
        {
            Time.timeScale = 1f;
            Services.Get<GameManager>().ReturnToLobby();
        }
    }
}