using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.Data;
using CRClone.Network;
using CRClone.Systems;
using CRClone.UI.Animation;

namespace CRClone.UI.Screens
{
    public class BattleResultScreen : MonoBehaviour
    {
        [Header("Result Banner")]
        [SerializeField] private GameObject _victoryBanner;
        [SerializeField] private GameObject _defeatBanner;
        [SerializeField] private GameObject _drawBanner;
        [SerializeField] private Text _resultTitleText;

        [Header("Crowns")]
        [SerializeField] private Image[] _player1Crowns = new Image[3];
        [SerializeField] private Image[] _player2Crowns = new Image[3];
        [SerializeField] private Sprite _crownFilled;
        [SerializeField] private Sprite _crownEmpty;
        [SerializeField] private Sprite _kingCrown;

        [Header("Player Info")]
        [SerializeField] private Text _player1NameText;
        [SerializeField] private Text _player2NameText;
        [SerializeField] private Text _player1TrophyChangeText;
        [SerializeField] private Text _player2TrophyChangeText;

        [Header("Rewards")]
        [SerializeField] private Transform _rewardsContainer;
        [SerializeField] private GameObject _rewardItemPrefab;

        [Header("Battle Log")]
        [SerializeField] private Transform _battleLogContainer;
        [SerializeField] private GameObject _battleLogItemPrefab;

        [Header("Action Buttons")]
        [SerializeField] private Button _watchReplayButton;
        [SerializeField] private Button _shareButton;
        [SerializeField] private Button _rematchButton;
        [SerializeField] private Button _backToLobbyButton;

        [Header("Animation")]
        [SerializeField] private float _crownPopDelay = 0.1f;
        [SerializeField] private float _crownPopDuration = 0.3f;
        [SerializeField] private AnimationCurve _crownPopCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private EventBus.BattleEndedEvent _battleEvent;
        private Coroutine _animationCoroutine;

        public void DisplayResult(EventBus.BattleEndedEvent evt)
        {
            _battleEvent = evt;
            SetupUI();
            
            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
            _animationCoroutine = StartCoroutine(PlayResultAnimation());
        }

        private void SetupUI()
        {
            _watchReplayButton?.onClick.AddListener(OnWatchReplay);
            _shareButton?.onClick.AddListener(OnShare);
            _rematchButton?.onClick.AddListener(OnRematch);
            _backToLobbyButton?.onClick.AddListener(OnBackToLobby);

            bool isPlayer1Victory = _battleEvent.result == BattleStatus.Player1Won;
            bool isDraw = _battleEvent.result == BattleStatus.Draw;

            _victoryBanner?.SetActive(isPlayer1Victory && !isDraw);
            _defeatBanner?.SetActive(!isPlayer1Victory && !isDraw);
            _drawBanner?.SetActive(isDraw);

            string resultText = isDraw ? "DRAW" : (isPlayer1Victory ? "VICTORY" : "DEFEAT");
            if (_resultTitleText != null) _resultTitleText.text = resultText;

            if (_player1NameText != null) _player1NameText.text = _battleEvent.player1Name ?? "Player 1";
            if (_player2NameText != null) _player2NameText.text = _battleEvent.player2Name ?? "Player 2";

            SetupCrowns();
            SetupTrophyChanges();
            SetupRewards();
            SetupBattleLog();
        }

        private void SetupCrowns()
        {
            for (int i = 0; i < 3; i++)
            {
                if (i < _player1Crowns.Length)
                {
                    _player1Crowns[i].sprite = i < _battleEvent.player1Crowns ? _crownFilled : _crownEmpty;
                    _player1Crowns[i].transform.localScale = Vector3.zero;
                }
                if (i < _player2Crowns.Length)
                {
                    _player2Crowns[i].sprite = i < _battleEvent.player2Crowns ? _crownFilled : _crownEmpty;
                    _player2Crowns[i].transform.localScale = Vector3.zero;
                }
            }

            if (_battleEvent.player1Crowns == 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (i < _player1Crowns.Length) _player1Crowns[i].sprite = _kingCrown;
                }
            }
            if (_battleEvent.player2Crowns == 3)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (i < _player2Crowns.Length) _player2Crowns[i].sprite = _kingCrown;
                }
            }
        }

        private void SetupTrophyChanges()
        {
            int p1Change = _battleEvent.player1TrophyChange;
            int p2Change = _battleEvent.player2TrophyChange;

            if (_player1TrophyChangeText != null)
            {
                _player1TrophyChangeText.text = p1Change >= 0 ? $"+{p1Change}" : p1Change.ToString();
                _player1TrophyChangeText.color = p1Change >= 0 ? Color.green : Color.red;
            }

            if (_player2TrophyChangeText != null)
            {
                _player2TrophyChangeText.text = p2Change >= 0 ? $"+{p2Change}" : p2Change.ToString();
                _player2TrophyChangeText.color = p2Change >= 0 ? Color.green : Color.red;
            }
        }

        private void SetupRewards()
        {
            if (_rewardsContainer == null || _rewardItemPrefab == null) return;

            foreach (Transform child in _rewardsContainer)
            {
                Destroy(child.gameObject);
            }

            if (_battleEvent.rewards != null)
            {
                foreach (var reward in _battleEvent.rewards)
                {
                    var rewardGO = Instantiate(_rewardItemPrefab, _rewardsContainer);
                    var rewardUI = rewardGO.GetComponent<BattleRewardItemUI>();
                    if (rewardUI != null)
                    {
                        rewardUI.Initialize(reward);
                    }
                }
            }
        }

        private void SetupBattleLog()
        {
            if (_battleLogContainer == null || _battleLogItemPrefab == null || _battleEvent.keyEvents == null) return;

            foreach (Transform child in _battleLogContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var logEvent in _battleEvent.keyEvents)
            {
                var logGO = Instantiate(_battleLogItemPrefab, _battleLogContainer);
                var logUI = logGO.GetComponent<BattleLogItemUI>();
                if (logUI != null)
                {
                    logUI.Initialize(logEvent);
                }
            }
        }

        private IEnumerator PlayResultAnimation()
        {
            yield return new WaitForSeconds(0.5f);

            var p1Crowns = new List<Image>(_player1Crowns);
            var p2Crowns = new List<Image>(_player2Crowns);

            int maxCrowns = Math.Max(_battleEvent.player1Crowns, _battleEvent.player2Crowns);

            for (int i = 0; i < maxCrowns; i++)
            {
                if (i < p1Crowns.Count && i < _battleEvent.player1Crowns)
                {
                    StartCoroutine(PopCrown(p1Crowns[i]));
                }
                if (i < p2Crowns.Count && i < _battleEvent.player2Crowns)
                {
                    StartCoroutine(PopCrown(p2Crowns[i]));
                }
                yield return new WaitForSeconds(_crownPopDelay);
            }

            yield return new WaitForSeconds(0.5f);

            if (_player1TrophyChangeText != null)
            {
                StartCoroutine(AnimateTrophyChange(_player1TrophyChangeText, 0, _battleEvent.player1TrophyChange));
            }
            if (_player2TrophyChangeText != null)
            {
                StartCoroutine(AnimateTrophyChange(_player2TrophyChangeText, 0, _battleEvent.player2TrophyChange));
            }
        }

        private IEnumerator PopCrown(Image crown)
        {
            if (crown == null) yield break;

            float elapsed = 0f;
            Vector3 startScale = Vector3.zero;
            Vector3 targetScale = Vector3.one * 1.2f;
            Vector3 endScale = Vector3.one;

            while (elapsed < _crownPopDuration)
            {
                elapsed += Time.deltaTime;
                float t = _crownPopCurve.Evaluate(elapsed / _crownPopDuration);
                crown.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < 0.1f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.1f;
                crown.transform.localScale = Vector3.Lerp(targetScale, endScale, t);
                yield return null;
            }

            crown.transform.localScale = endScale;
        }

        private IEnumerator AnimateTrophyChange(Text text, int startValue, int endValue)
        {
            float duration = 1f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                int currentValue = Mathf.RoundToInt(Mathf.Lerp(startValue, endValue, t));
                text.text = currentValue >= 0 ? $"+{currentValue}" : currentValue.ToString();
                yield return null;
            }

            text.text = endValue >= 0 ? $"+{endValue}" : endValue.ToString();
        }

        private void OnWatchReplay()
        {
            UISoundPlayer.Instance?.PlayButtonClick();
            Services.Get<NetworkClient>().Send(new NetworkClient.ReplayRequest { replayId = _battleEvent.replayId });
        }

        private void OnShare()
        {
            UISoundPlayer.Instance?.PlayButtonClick();
            string shareText = $"Just got {(_battleEvent.result == BattleStatus.Player1Won ? "VICTORY" : "DEFEAT")} in Clash Royale Clone! {_battleEvent.player1Crowns}-{_battleEvent.player2Crowns} crowns";
            GUIUtility.systemCopyBuffer = shareText;
            EventBus.RaiseToast("Battle result copied to clipboard!");
        }

        private void OnRematch()
        {
            UISoundPlayer.Instance?.PlayButtonClick();
            Services.Get<NetworkClient>().Send(new NetworkClient.RematchRequest { battleId = _battleEvent.battleId });
        }

        private void OnBackToLobby()
        {
            UISoundPlayer.Instance?.PlayButtonClick();
            Services.Get<GameManager>().ChangeState(GameState.Lobby);
        }
    }

    public class BattleRewardItemUI : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _countText;
        [SerializeField] private Image _rarityFrame;

        public void Initialize(EventBus.ChestReward reward)
        {
            string name = "";
            int count = 0;

            switch (reward.type)
            {
                case EventBus.RewardType.Card:
                    var cardData = Services.Get<DataManager>().GetCard(reward.cardId);
                    if (cardData != null)
                    {
                        name = cardData.cardName;
                        count = reward.count;
                        if (_iconImage != null)
                        {
                            _iconImage.sprite = Services.Get<AssetManager>().LoadSprite(cardData.portraitId);
                        }
                        if (_rarityFrame != null)
                        {
                            _rarityFrame.color = GetRarityColor(cardData.rarity);
                        }
                    }
                    break;
                case EventBus.RewardType.Gold:
                    name = "Gold";
                    count = reward.gold;
                    if (_iconImage != null) _iconImage.color = Color.yellow;
                    break;
                case EventBus.RewardType.Gems:
                    name = "Gems";
                    count = reward.gems;
                    if (_iconImage != null) _iconImage.color = new Color(0f, 0.7f, 1f);
                    break;
                case EventBus.RewardType.WildCard:
                    name = $"Wild {reward.rarity}";
                    count = reward.count;
                    if (_rarityFrame != null)
                    {
                        _rarityFrame.color = GetRarityColor(reward.rarity);
                    }
                    break;
            }

            if (_nameText != null) _nameText.text = name;
            if (_countText != null) _countText.text = count > 1 ? $"x{count}" : "";
        }

        private Color GetRarityColor(CardRarity rarity)
        {
            if (AccessibilityManager.Instance != null)
            {
                return AccessibilityManager.Instance.GetRarityColor(rarity);
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
    }

    public class BattleLogItemUI : MonoBehaviour
    {
        [SerializeField] private Text _timeText;
        [SerializeField] private Text _eventText;
        [SerializeField] private Image _eventIcon;

        public void Initialize(BattleLogEvent logEvent)
        {
            if (_timeText != null)
            {
                int minutes = Mathf.FloorToInt(logEvent.time / 60f);
                int seconds = Mathf.FloorToInt(logEvent.time % 60f);
                _timeText.text = $"{minutes:00}:{seconds:00}";
            }

            if (_eventText != null)
            {
                _eventText.text = logEvent.description;
            }

            if (_eventIcon != null)
            {
                // Set icon based on event type
            }
        }
    }

    [Serializable]
    public class BattleLogEvent
    {
        public float time;
        public string description;
        public BattleLogType type;
    }

    public enum BattleLogType
    {
        FirstBlood,
        TowerDestroyed,
        KingTowerActivated,
        BigSpell,
        Comeback
    }
}