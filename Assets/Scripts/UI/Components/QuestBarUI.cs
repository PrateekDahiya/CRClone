using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.UI.Animation;

namespace CRClone.UI.Components
{
    public class QuestBarUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform _questContainer;
        [SerializeField] private GameObject _questItemPrefab;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private int _maxVisibleQuests = 3;

        [Header("Quest Item Template")]
        [SerializeField] private Image _questIcon;
        [SerializeField] private Text _questTitleText;
        [SerializeField] private Text _questProgressText;
        [SerializeField] private Image _progressFill;
        [SerializeField] private Button _claimButton;
        [SerializeField] private GameObject _completedBadge;

        private List<QuestItemUI> _questItems = new List<QuestItemUI>();
        private List<QuestData> _currentQuests = new List<QuestData>();

        public void Initialize()
        {
            if (_scrollRect != null)
            {
                _scrollRect.horizontal = true;
                _scrollRect.vertical = false;
                _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            }
        }

        public void SetQuests(List<QuestData> quests)
        {
            _currentQuests = quests ?? new List<QuestData>();
            RefreshQuestItems();
        }

        public void AddQuest(QuestData quest)
        {
            if (quest == null) return;

            _currentQuests.Add(quest);
            CreateQuestItem(quest);
        }

        public void UpdateQuestProgress(int questId, int currentProgress, int maxProgress)
        {
            var questItem = _questItems.Find(q => q.QuestId == questId);
            if (questItem != null)
            {
                questItem.UpdateProgress(currentProgress, maxProgress);
            }

            var questData = _currentQuests.Find(q => q.questId == questId);
            if (questData != null)
            {
                questData.currentProgress = currentProgress;
                questData.maxProgress = maxProgress;
            }
        }

        public void CompleteQuest(int questId)
        {
            var questItem = _questItems.Find(q => q.QuestId == questId);
            if (questItem != null)
            {
                questItem.SetCompleted(true);
            }

            var questData = _currentQuests.Find(q => q.questId == questId);
            if (questData != null)
            {
                questData.isCompleted = true;
                questData.currentProgress = questData.maxProgress;
            }
        }

        public void ClaimQuest(int questId)
        {
            var questItem = _questItems.Find(q => q.QuestId == questId);
            if (questItem != null)
            {
                questItem.PlayClaimAnimation();
            }

            _currentQuests.RemoveAll(q => q.questId == questId);
        }

        private void RefreshQuestItems()
        {
            foreach (var item in _questItems)
            {
                if (item != null) Destroy(item.gameObject);
            }
            _questItems.Clear();

            foreach (var quest in _currentQuests)
            {
                CreateQuestItem(quest);
            }
        }

        private void CreateQuestItem(QuestData quest)
        {
            if (_questItemPrefab == null || _questContainer == null) return;

            var itemGO = Instantiate(_questItemPrefab, _questContainer);
            var itemUI = itemGO.GetComponent<QuestItemUI>();
            if (itemUI == null) itemUI = itemGO.AddComponent<QuestItemUI>();

            itemUI.Initialize(quest, OnQuestClaimed);
            _questItems.Add(itemUI);
        }

        private void OnQuestClaimed(int questId)
        {
            var quest = _currentQuests.Find(q => q.questId == questId);
            if (quest?.rewards != null)
            {
                UIManager.Instance?.ShowToast($"Quest completed! Rewards claimed.");
            }
            ClaimQuest(questId);
        }
    }

    public class QuestItemUI : MonoBehaviour
    {
        [SerializeField] private Image _questIcon;
        [SerializeField] private Text _questTitleText;
        [SerializeField] private Text _questProgressText;
        [SerializeField] private Image _progressFill;
        [SerializeField] private Button _claimButton;
        [SerializeField] private GameObject _completedBadge;
        [SerializeField] private GameObject _inProgressBadge;

        public int QuestId { get; private set; }

        public void Initialize(QuestData quest, Action<int> onClaimed)
        {
            QuestId = quest.questId;

            if (_questTitleText != null) _questTitleText.text = quest.title;
            UpdateProgress(quest.currentProgress, quest.maxProgress);

            if (_claimButton != null)
            {
                _claimButton.onClick.RemoveAllListeners();
                _claimButton.onClick.AddListener(() => onClaimed?.Invoke(QuestId));
            }

            SetCompleted(quest.isCompleted);
        }

        public void UpdateProgress(int current, int max)
        {
            if (_questProgressText != null)
            {
                _questProgressText.text = $"{current}/{max}";
            }

            if (_progressFill != null && max > 0)
            {
                _progressFill.fillAmount = (float)current / max;
            }

            bool isComplete = current >= max;
            _claimButton.OrNull()?.gameObject.SetActive(isComplete);
            _inProgressBadge.OrNull()?.SetActive(!isComplete);
        }

        public void SetCompleted(bool completed)
        {
            _completedBadge.OrNull()?.SetActive(completed);
            _inProgressBadge.OrNull()?.SetActive(!completed);
            _claimButton.OrNull()?.gameObject.SetActive(completed);
        }

        public void PlayClaimAnimation()
        {
            StartCoroutine(ClaimAnimation());
        }

        private System.Collections.IEnumerator ClaimAnimation()
        {
            yield return transform.ScaleTo(Vector3.one * 1.2f, 0.15f, AnimationCurves.EaseOutBack);
            yield return transform.ScaleTo(Vector3.zero, 0.2f, AnimationCurves.EaseInBack);
            Destroy(gameObject);
        }
    }

    [Serializable]
    public class QuestData
    {
        public int questId;
        public string title;
        public string description;
        public int currentProgress;
        public int maxProgress;
        public bool isCompleted;
        public bool isClaimed;
        public List<EventBus.ChestReward> rewards;
        public QuestType type;
    }

    public enum QuestType
    {
        Daily,
        Weekly,
        Seasonal,
        Achievement
    }
}