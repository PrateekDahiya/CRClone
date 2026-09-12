using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;
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
        [SerializeField] private AnimationCurve _bannerCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float _bannerDuration = 1f;

        [Header("Crown Display")]
        [SerializeField] private Transform _p1CrownsContainer;
        [SerializeField] private Transform _p2CrownsContainer;
        [SerializeField] private GameObject _crownPrefab;
        [SerializeField] private Sprite _crownFilled;
        [SerializeField] private Sprite _crownEmpty;
        [SerializeField] private Sprite _kingCrown;

        [Header("Player Info")]
        [SerializeField] private Text _p1NameText;
        [SerializeField] private Text _p2NameText;
        [SerializeField] private Text _p1TrophyChangeText;
        [SerializeField] private Text _p2TrophyChangeText;

        [Header("Rewards")]
        [SerializeField] private Transform _rewardsContainer;
        [SerializeField] private GameObject _rewardItemPrefab;
        [SerializeField] private Text _chestRewardText;
        [SerializeField] private Text _goldRewardText;
        [SerializeField] private Text _xpRewardText;

        [Header("Battle Log")]
        [SerializeField] private Transform _battleLogContainer;
        [SerializeField] private GameObject _battleLogItemPrefab;

        [Header("Action Buttons")]
        [SerializeField] private Button _watchReplayButton;
        [SerializeField] private Button _shareButton;
        [SerializeField] private Button _rematchButton;
        [SerializeField] private Button _backToLobbyButton;

        [Header("Animation")]
        [SerializeField] private float _staggerDelay = 0.1f;
        [SerializeField] private float _crownPopDuration = 0.3f;

        private EventBus.BattleEndedEvent _battleEvent;
        private bool _isAnimating;

        public void DisplayResult(EventBus.BattleEndedEvent evt)
        {
            _battleEvent = evt;
            SetupUI();
            StartCoroutine(PlayResultAnimation());
        }

        private void SetupUI()
        {
            _watchReplayButton?.onClick.AddListener(OnWatchReplay);
            _shareButton?.onClick.AddListener(OnShare);
            _rematchButton?.onClick.AddListener(OnRematch);
            _backToLobbyButton?.onClick.AddListener(OnBackToLobby);

            bool isPlayer1Victory = evt.result == BattleStatus.Player1Won;
            bool isDraw = evt.result == BattleStatus.Draw;

            _victoryBanner?.SetActive(isPlayer1Victory && !isDraw);
            _defeatBanner?.SetActive(!isPlayer1Victory && !isDraw);
            _drawBanner?.SetActive(isDraw);

            string resultText = isDraw ? "DRAW" : (isPlayer1Victory ? "VICTORY" : "DEFEAT");
            if (_resultTitleText != null) _resultTitleText.text = resultText;

            if (_p1NameText != null) _p1NameText.text = evt.player1Name ?? "Player 1";
            if (_p2NameText != null) _p2NameText.text = evt.player2Name ?? "Player 2";

            SetupCrowns();
            SetupTrophyChanges();
            SetupRewards();
            SetupBattleLog();
        }

        private void SetupCrowns()
        {
            ClearCrowns(_p1CrownsContainer);
            ClearCrowns(_p2CrownsContainer);

            for (int i = 0; i < 3; i++)
            {
                var p1Crown = CreateCrown(_p1CrownsContainer, i < _battleEvent.player1Crowns);
                var p2Crown = CreateCrown(_p2CrownsContainer, i < _battleEvent.player2Crowns);
            }

            if (_battleEvent.player1Crowns == 3)
            {
                AddKingCrown(_p1CrownsContainer);
            }
            if (_battleEvent.player2Crowns == 3)
            {
                AddKingCrown(_p2CrownsContainer);
            }
        }

        private GameObject CreateCrown(Transform container, bool filled)
        {
            if (_crownPrefab == null || container == null) return null;

            var crownGO = Instantiate(_crownPrefab, container);
            var image = crownGO.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = filled ? _crownFilled : _crownEmpty;
            }
            crownGO.transform.localScale = Vector3.zero;
            return crownGO;
        }

        private void AddKingCrown(Transform container)
        {
            if (_crownPrefab == null || container == null || _kingCrown == null) return;

            var crownGO = Instantiate(_crownPrefab, container);
            var image = crownGO.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = _kingCrown;
            }
            crownGO.transform.localScale = Vector3.zero;
        }

        private void ClearCrowns(Transform container)
        {
            if (container == null) return;
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
        }

        private void SetupTrophyChanges()
        {
            int p1Change = _battleEvent.player1TrophyChange;
            int p2Change = _battleEvent.player2TrophyChange;

            if (_p1TrophyChangeText != null)
            {
                _p1TrophyChangeText.text = p1Change >= 0 ? $"+{p1Change}" : p1Change.ToString();
                _p1TrophyChangeText.color = p1Change >= 0 ? Color.green : Color.red;
            }

            if (_p2TrophyChangeText != null)
            {
                _p2TrophyChangeText.text = p2Change >= 0 ? $"+{p2Change}" : p2Change.ToString();
                _p2TrophyChangeText.color = p2Change >= 0 ? Color.green : Color.red;
            }
        }

        private void SetupRewards()
        {
            if (_rewardsContainer == null) return;

            foreach (Transform child in _rewardsContainer)
            {
                Destroy(child.gameObject);
            }

            if (_battleEvent.rewards != null)
            {
                foreach (var reward in _battleEvent.rewards)
                {
                    var rewardGO = Instantiate(_rewardItemPrefab, _rewardsContainer);
                    var rewardUI = rewardGO.GetComponent<RewardItemUI>();
                    if (rewardUI != null)
                    {
                        rewardUI.Initialize(reward);
                    }
                }
            }
        }

        private void SetupBattleLog()
        {
            if (_battleLogContainer == null || _battleEvent.keyEvents == null) return;

            foreach (Transform child in _battleLogContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var evt in _battleEvent.keyEvents)
            {
                var logGO = Instantiate(_battleLogItemPrefab, _battleLogContainer);
                var logUI = logGO.GetComponent<BattleLogItemUI>();
                if (logUI != null)
                {
                    logUI.Initialize(evt);
                }
            }
        }

        private System.Collections.IEnumerator PlayResultAnimation()
        {
            _isAnimating = true;

            yield return AnimateBanner();
            yield return AnimateCrowns();
            yield return AnimateTrophyChanges();
            yield return AnimateRewards();

            _isAnimating = false;
        }

        private System.Collections.IEnumerator AnimateBanner()
        {
            GameObject activeBanner = _victoryBanner?.activeSelf == true ? _victoryBanner :
                                     _defeatBanner?.activeSelf == true ? _defeatBanner : _drawBanner;

            if (activeBanner == null) yield break;

            var canvasGroup = activeBanner.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = activeBanner.AddComponent<CanvasGroup>();
            var rect = activeBanner.GetComponent<RectTransform>();

            canvasGroup.alpha = 0f;
            rect.localScale = Vector3.one * 0.5f;

            float elapsed = 0f;
            while (elapsed < _bannerDuration)
            {
                elapsed += Time.deltaTime;
                float t = _bannerCurve.Evaluate(elapsed / _bannerDuration);
                canvasGroup.alpha = t;
                rect.localScale = Vector3.Lerp(Vector3.one * 0.5f, Vector3.one, t);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            rect.localScale = Vector3.one;

            UISoundPlayer.Instance?.PlayVictory();
        }

        private System.Collections.IEnumerator AnimateCrowns()
        {
            yield return new WaitForSeconds(0.3f);

            var p1Crowns = GetCrownObjects(_p1CrownsContainer);
            var p2Crowns = GetCrownObjects(_p2CrownsContainer);

            int maxCrowns = Math.Max(p1Crowns.Count, p2Crowns.Count);

            for (int i = 0; i < maxCrowns; i++)
            {
                if (i < p1Crowns.Count)
                {
                    StartCoroutine(PopCrown(p1Crowns[i]));
                }
                if (i < p2Crowns.Count)
                {
                    StartCoroutine(PopCrown(p2Crowns[i]));
                }
                yield return new WaitForSeconds(_staggerDelay);
            }
        }

        private List<GameObject> GetCrownObjects(Transform container)
        {
            var list = new List<GameObject>();
            if (container == null) return list;
            foreach (Transform child in container)
            {
                list.Add(child.gameObject);
            }
            return list;
        }

        private System.Collections.IEnumerator PopCrown(GameObject crown)
        {
            if (crown == null) yield break;

            float elapsed = 0f;
            while (elapsed < _crownPopDuration)
            {
                elapsed += Time.deltaTime;
                float t = AnimationCurves.EaseOutBack.Evaluate(elapsed / _crownPopDuration);
                crown.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
                yield return null;
            }
            crown.transform.localScale = Vector3.one;
        }

        private System.Collections.IEnumerator AnimateTrophyChanges()
        {
            yield return new WaitForSeconds(0.5f);

            AnimateTrophyText(_p1TrophyChangeText, _battleEvent.player1TrophyChange);
            AnimateTrophyText(_p2TrophyChangeText, _battleEvent.player2TrophyChange);
        }

        private void AnimateTrophyText(Text text, int change)
        {
            if (text == null) return;

            StartCoroutine(TrophyCountAnimation(text, change));
        }

        private System.Collections.IEnumerator TrophyCountAnimation(Text text, int targetChange)
        {
            int startValue = 0;
            int endValue = targetChange;
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

        private System.Collections.IEnumerator AnimateRewards()
        {
            yield return new WaitForSeconds(0.3f);

            if (_rewardsContainer == null) yield break;

            for (int i = 0; i < _rewardsContainer.childCount; i++)
            {
                var reward = _rewardsContainer.GetChild(i).gameObject;
                reward.transform.localScale = Vector3.zero;
                StartCoroutine(PopReward(reward));
                yield return new WaitForSeconds(_staggerDelay);
            }
        }

        private System.Collections.IEnumerator PopReward(GameObject reward)
        {
            float elapsed = 0f;
            float duration = 0.3f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = AnimationCurves.EaseOutBack.Evaluate(elapsed / duration);
                reward.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
                yield return null;
            }
            reward.transform.localScale = Vector3.one;
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

    public class RewardItemUI : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private Text _amountText;
        [SerializeField] private Text _typeText;

        public void Initialize(EventBus.ChestReward reward)
        {
            if (_typeText != null)
            {
                _typeText.text = reward.type.ToString();
            }

            if (_amountText != null)
            {
                switch (reward.type)
                {
                    case EventBus.RewardType.Card:
                        _amountText.text = $"{reward.cardId} x{reward.count}";
                        break;
                    case EventBus.RewardType.Gold:
                        _amountText.text = reward.gold.ToString("N0");
                        break;
                    case EventBus.RewardType.Gems:
                        _amountText.text = reward.gems.ToString("N0");
                        break;
                    case EventBus.RewardType.WildCard:
                        _amountText.text = $"Wild {reward.rarity} x{reward.count}";
                        break;
                }
            }
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