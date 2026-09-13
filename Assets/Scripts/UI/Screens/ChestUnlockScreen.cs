using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.Data;
using CRClone.Systems;
using CRClone.UI.Animation;

namespace CRClone.UI.Screens
{
    public class ChestUnlockScreen : MonoBehaviour
    {
        [Header("Chest Visual")]
        [SerializeField] private Image _chestImage;
        [SerializeField] private ParticleSystem _chestParticles;
        [SerializeField] private ParticleSystem _unlockParticles;
        [SerializeField] private AnimationCurve _chestOpenCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float _chestOpenDuration = 2f;

        [Header("Rewards Display")]
        [SerializeField] private Transform _rewardsContainer;
        [SerializeField] private GameObject _rewardItemPrefab;
        [SerializeField] private float _staggerDelay = 0.15f;

        [Header("Chest Name")]
        [SerializeField] private Text _chestNameText;

        [Header("Actions")]
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _openAnotherButton;

        private int _chestTypeId;
        private List<EventBus.ChestReward> _rewards;
        private bool _isAnimating;

        public void DisplayChest(int chestTypeId, List<EventBus.ChestReward> rewards)
        {
            _chestTypeId = chestTypeId;
            _rewards = rewards ?? new List<EventBus.ChestReward>();

            SetupUI();
            StartCoroutine(PlayUnlockAnimation());
        }

        private void SetupUI()
        {
            _continueButton?.onClick.AddListener(OnContinue);
            _openAnotherButton?.onClick.AddListener(OnOpenAnother);

            if (_chestNameText != null)
            {
                _chestNameText.text = GetChestName(_chestTypeId);
            }

            if (_chestImage != null)
            {
                _chestImage.sprite = GetChestSprite(_chestTypeId);
            }

            if (_continueButton != null) _continueButton.gameObject.SetActive(false);
            if (_openAnotherButton != null) _openAnotherButton.gameObject.SetActive(false);
        }

        private System.Collections.IEnumerator PlayUnlockAnimation()
        {
            _isAnimating = true;

            yield return AnimateChestOpen();
            yield return AnimateRewards();

            _isAnimating = false;

            if (_continueButton != null) _continueButton.gameObject.SetActive(true);
            if (_openAnotherButton != null) _openAnotherButton.gameObject.SetActive(true);
        }

        private System.Collections.IEnumerator AnimateChestOpen()
        {
            if (_chestParticles != null) _chestParticles.Play();

            float elapsed = 0f;
            Vector3 startScale = _chestImage.transform.localScale;
            Vector3 maxScale = startScale * 1.2f;

            while (elapsed < _chestOpenDuration * 0.3f)
            {
                elapsed += Time.deltaTime;
                float t = _chestOpenCurve.Evaluate(elapsed / (_chestOpenDuration * 0.3f));
                _chestImage.transform.localScale = Vector3.Lerp(startScale, maxScale, t);
                yield return null;
            }

            if (_unlockParticles != null) _unlockParticles.Play();
            UISoundPlayer.Instance?.PlayChestUnlock();

            yield return new WaitForSeconds(0.3f);

            elapsed = 0f;
            while (elapsed < _chestOpenDuration * 0.4f)
            {
                elapsed += Time.deltaTime;
                float t = _chestOpenCurve.Evaluate(elapsed / (_chestOpenDuration * 0.4f));
                _chestImage.transform.localScale = Vector3.Lerp(maxScale, startScale * 1.5f, t);
                _chestImage.color = Color.Lerp(Color.white, new Color(1f, 1f, 1f, 0f), t);
                yield return null;
            }

            _chestImage.gameObject.SetActive(false);
        }

        private System.Collections.IEnumerator AnimateRewards()
        {
            if (_rewardsContainer == null || _rewardItemPrefab == null) yield break;

            for (int i = 0; i < _rewards.Count; i++)
            {
                var rewardGO = Instantiate(_rewardItemPrefab, _rewardsContainer);
                var rewardUI = rewardGO.GetComponent<ChestRewardItemUI>();
                if (rewardUI != null)
                {
                    rewardUI.Initialize(_rewards[i]);
                }

                rewardGO.transform.localScale = Vector3.zero;
                StartCoroutine(PopReward(rewardGO));
                yield return new WaitForSeconds(_staggerDelay);
            }
        }

        private System.Collections.IEnumerator PopReward(GameObject reward)
        {
            float elapsed = 0f;
            float duration = 0.4f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = AnimationCurves.EaseOutBack.Evaluate(elapsed / duration);
                reward.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
                yield return null;
            }

            reward.transform.localScale = Vector3.one;
        }

        private void OnContinue()
        {
            UISoundPlayer.Instance?.PlayButtonClick();
            Services.Get<GameManager>().ChangeState(GameState.Lobby);
        }

        private void OnOpenAnother()
        {
            UISoundPlayer.Instance?.PlayButtonClick();
        }

        private string GetChestName(int chestTypeId)
        {
            return chestTypeId switch
            {
                1 => "Wooden Chest",
                2 => "Silver Chest",
                3 => "Golden Chest",
                4 => "Magical Chest",
                5 => "Giant Chest",
                6 => "Legendary Chest",
                7 => "Epic Chest",
                8 => "Champion Chest",
                _ => "Chest"
            };
        }

        private Sprite GetChestSprite(int chestTypeId)
        {
            return null;
        }
    }

    public class ChestRewardItemUI : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _countText;
        [SerializeField] private Image _rarityFrame;

        public void Initialize(EventBus.ChestReward reward)
        {
            string name = "";
            int count = 0;
            CardRarity rarity = CardRarity.Common;

            switch (reward.type)
            {
                case EventBus.RewardType.Card:
                    var cardData = Services.Get<DataManager>().GetCard(reward.cardId);
                    if (cardData != null)
                    {
                        name = cardData.cardName;
                        count = reward.count;
                        rarity = cardData.rarity;
                        if (_iconImage != null)
                        {
                            _iconImage.sprite = Services.Get<AssetManager>().LoadSprite(cardData.portraitId);
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
                    rarity = reward.rarity;
                    if (_iconImage != null) _iconImage.color = GetRarityColor(rarity);
                    break;
            }

            if (_nameText != null) _nameText.text = name;
            if (_countText != null) _countText.text = count > 1 ? $"x{count}" : "";

            if (_rarityFrame != null)
            {
                _rarityFrame.color = GetRarityColor(rarity);
            }
        }

        private Color GetRarityColor(CardRarity rarity)
        {
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
}