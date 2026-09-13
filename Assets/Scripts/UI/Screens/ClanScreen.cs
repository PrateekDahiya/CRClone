using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.Data;
using CRClone.Network;
using CRClone.UI.Components;

namespace CRClone.UI.Screens
{
    public class ClanScreen : MonoBehaviour
    {
        [Header("Top Bar")]
        [SerializeField] private Text _clanNameText;
        [SerializeField] private Text _clanTrophyReqText;
        [SerializeField] private Text _clanMembersText;
        [SerializeField] private Button _backButton;

        [Header("Tabs")]
        [SerializeField] private Button _chatTab;
        [SerializeField] private Button _membersTab;
        [SerializeField] private Button _warTab;
        [SerializeField] private Button _capitalTab;
        [SerializeField] private Button _settingsTab;

        [Header("Tab Content")]
        [SerializeField] private Transform _chatContent;
        [SerializeField] private Transform _membersContent;
        [SerializeField] private Transform _warContent;
        [SerializeField] private Transform _capitalContent;
        [SerializeField] private Transform _settingsContent;

        [Header("Chat")]
        [SerializeField] private Transform _chatMessagesContainer;
        [SerializeField] private GameObject _chatMessagePrefab;
        [SerializeField] private InputField _chatInput;
        [SerializeField] private Button _sendButton;
        [SerializeField] private Button _donateRequestButton;

        [Header("Members")]
        [SerializeField] private Transform _membersListContainer;
        [SerializeField] private GameObject _memberItemPrefab;

        [Header("War")]
        [SerializeField] private Text _warStatusText;
        [SerializeField] private Button _warParticipateButton;

        [Header("Capital")]
        [SerializeField] private Text _capitalStatusText;

        [Header("Settings")]
        [SerializeField] private Button _leaveClanButton;
        [SerializeField] private InputField _clanDescriptionInput;

        private ClanTab _currentTab = ClanTab.Chat;
        private List<ClanMember> _members = new List<ClanMember>();
        private List<ChatMessage> _messages = new List<ChatMessage>();

        public enum ClanTab
        {
            Chat,
            Members,
            War,
            Capital,
            Settings
        }

        private void Awake()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            _backButton?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.MainMenu));

            _chatTab?.onClick.AddListener(() => SwitchTab(ClanTab.Chat));
            _membersTab?.onClick.AddListener(() => SwitchTab(ClanTab.Members));
            _warTab?.onClick.AddListener(() => SwitchTab(ClanTab.War));
            _capitalTab?.onClick.AddListener(() => SwitchTab(ClanTab.Capital));
            _settingsTab?.onClick.AddListener(() => SwitchTab(ClanTab.Settings));

            _sendButton?.onClick.AddListener(OnSendMessage);
            _donateRequestButton?.onClick.AddListener(OnDonateRequest);
            _warParticipateButton?.onClick.AddListener(OnWarParticipate);
            _leaveClanButton?.onClick.AddListener(OnLeaveClan);

            LoadClanData();
        }

        private void OnEnable()
        {
            SwitchTab(ClanTab.Chat);
            RefreshCurrentTab();
        }

        private void SwitchTab(ClanTab tab)
        {
            _currentTab = tab;
            UpdateTabVisuals();
            ShowTabContent(tab);
            RefreshCurrentTab();
        }

        private void UpdateTabVisuals()
        {
            SetTabSelected(_chatTab, _currentTab == ClanTab.Chat);
            SetTabSelected(_membersTab, _currentTab == ClanTab.Members);
            SetTabSelected(_warTab, _currentTab == ClanTab.War);
            SetTabSelected(_capitalTab, _currentTab == ClanTab.Capital);
            SetTabSelected(_settingsTab, _currentTab == ClanTab.Settings);
        }

        private void SetTabSelected(Button button, bool selected)
        {
            if (button == null) return;
            var colors = button.colors;
            colors.normalColor = selected ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            button.colors = colors;
        }

        private void ShowTabContent(ClanTab tab)
        {
            _chatContent?.gameObject.SetActive(tab == ClanTab.Chat);
            _membersContent?.gameObject.SetActive(tab == ClanTab.Members);
            _warContent?.gameObject.SetActive(tab == ClanTab.War);
            _capitalContent?.gameObject.SetActive(tab == ClanTab.Capital);
            _settingsContent?.gameObject.SetActive(tab == ClanTab.Settings);
        }

        private void RefreshCurrentTab()
        {
            switch (_currentTab)
            {
                case ClanTab.Chat:
                    RefreshChat();
                    break;
                case ClanTab.Members:
                    RefreshMembersList();
                    break;
                case ClanTab.War:
                    RefreshWar();
                    break;
                case ClanTab.Capital:
                    RefreshCapital();
                    break;
                case ClanTab.Settings:
                    RefreshSettings();
                    break;
            }
        }

        private void LoadClanData()
        {
            var playerData = Services.Get<GameManager>().LocalPlayer;
            if (playerData?.clan != null)
            {
                var clan = playerData.clan;
                _clanNameText.text = clan.name;
                _clanTrophyReqText.text = $"Trophy Req: {clan.trophyRequirement:N0}";
                _clanMembersText.text = $"Members: {clan.memberCount}/50";

                _members = clan.members ?? new List<ClanMember>();
                _messages = clan.messages ?? new List<ChatMessage>();
            }
        }

        private void RefreshChat()
        {
            if (_chatMessagesContainer == null || _chatMessagePrefab == null) return;

            foreach (Transform child in _chatMessagesContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var message in _messages)
            {
                var msgGO = Instantiate(_chatMessagePrefab, _chatMessagesContainer);
                var msgUI = msgGO.GetComponent<ChatMessageUI>();
                if (msgUI != null)
                {
                    msgUI.Initialize(message, OnDonateClicked);
                }
            }

            ScrollToBottom();
        }

        private void RefreshMembersList()
        {
            if (_membersListContainer == null || _memberItemPrefab == null) return;

            foreach (Transform child in _membersListContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var member in _members)
            {
                var memberGO = Instantiate(_memberItemPrefab, _membersListContainer);
                var memberUI = memberGO.GetComponent<ClanMemberItemUI>();
                if (memberUI != null)
                {
                    memberUI.Initialize(member, OnMemberAction);
                }
            }
        }

        private void RefreshWar()
        {
            if (_warStatusText != null)
            {
                _warStatusText.text = "River Race: Season 1\nYour Clan: Rank 3\nNext battle in 2 hours";
            }
        }

        private void RefreshCapital()
        {
            if (_capitalStatusText != null)
            {
                _capitalStatusText.text = "Clan Capital: Level 2\nDistricts: 3/5 unlocked\nNext raid: Friday 6 PM";
            }
        }

        private void RefreshSettings()
        {
            if (_clanDescriptionInput != null)
            {
                var playerData = Services.Get<GameManager>().LocalPlayer;
                if (playerData?.clan != null)
                {
                    _clanDescriptionInput.text = playerData.clan.description;
                }
            }
        }

        private void ScrollToBottom()
        {
            var scrollRect = _chatMessagesContainer?.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void OnSendMessage()
        {
            if (string.IsNullOrWhiteSpace(_chatInput?.text)) return;

            string message = _chatInput.text;
            _chatInput.text = "";

            var chatMessage = new ChatMessage
            {
                senderId = Services.Get<GameManager>().LocalPlayer?.playerId.ToString() ?? "",
                senderName = Services.Get<GameManager>().LocalPlayer?.playerName ?? "You",
                message = message,
                timestamp = DateTime.Now,
                type = ChatMessage.MessageType.Normal
            };

            _messages.Add(chatMessage);

            var msgGO = Instantiate(_chatMessagePrefab, _chatMessagesContainer);
            var msgUI = msgGO.GetComponent<ChatMessageUI>();
            if (msgUI != null)
            {
                msgUI.Initialize(chatMessage, OnDonateClicked);
            }

            ScrollToBottom();

            Services.Get<NetworkClient>().Send(new NetworkClient.ClanChatMessage { message = message });
        }

        private void OnDonateRequest()
        {
            UIManager.Instance?.ShowModal(Resources.Load<GameObject>("UI/DonateRequestModal"));
            var modalUI = UIManager.Instance?.GetComponentInChildren<DonateRequestModal>();
            if (modalUI != null)
            {
                modalUI.Initialize(OnDonateRequestConfirmed);
            }
        }

        private void OnDonateRequestConfirmed(int cardId, int count)
        {
            Services.Get<NetworkClient>().Send(new NetworkClient.ClanDonationRequest { cardId = cardId, count = count });
        }

        private void OnDonateClicked(int cardId, string requesterId)
        {
            var playerData = Services.Get<GameManager>().LocalPlayer;
            if (playerData?.collection.ContainsKey(cardId) == true)
            {
                int maxDonate = GetMaxDonation(cardId);
                if (playerData.collection[cardId].count >= maxDonate)
                {
                    Services.Get<NetworkClient>().Send(new NetworkClient.ClanDonate { cardId = cardId, recipientId = requesterId, count = maxDonate });
                    EventBus.RaiseToast("Donated!");
                }
                else
                {
                    EventBus.RaiseToast("Not enough cards to donate!");
                }
            }
        }

        private int GetMaxDonation(int cardId)
        {
            var cardData = Services.Get<DataManager>().GetCard(cardId);
            if (cardData == null) return 0;

            return cardData.rarity switch
            {
                CardRarity.Common => 10,
                CardRarity.Rare => 1,
                CardRarity.Epic => 0,
                CardRarity.Legendary => 0,
                CardRarity.Champion => 0,
                _ => 0
            };
        }

        private void OnWarParticipate()
        {
            Services.Get<NetworkClient>().Send(new NetworkClient.ClanWarAction { action = "participate" });
        }

        private void OnLeaveClan()
        {
            UIManager.Instance?.ShowModal(Resources.Load<GameObject>("UI/LeaveClanConfirmModal"));
        }

        private void OnMemberAction(ClanMember member, MemberAction action)
        {
            Services.Get<NetworkClient>().Send(new NetworkClient.ClanMemberAction 
            { 
                targetPlayerId = member.playerId, 
                action = action.ToString().ToLower() 
            });
        }

        public void OnMessageReceived(ChatMessage message)
        {
            _messages.Add(message);
            
            if (_currentTab == ClanTab.Chat)
            {
                var msgGO = Instantiate(_chatMessagePrefab, _chatMessagesContainer);
                var msgUI = msgGO.GetComponent<ChatMessageUI>();
                if (msgUI != null)
                {
                    msgUI.Initialize(message, OnDonateClicked);
                }
                ScrollToBottom();
            }
        }

        public void OnMemberUpdated(ClanMember member)
        {
            int index = _members.FindIndex(m => m.playerId == member.playerId);
            if (index >= 0)
            {
                _members[index] = member;
            }
            else
            {
                _members.Add(member);
            }

            if (_currentTab == ClanTab.Members)
            {
                RefreshMembersList();
            }
        }
    }

    [Serializable]
    public class ClanMember
    {
        public string playerId;
        public string playerName;
        public ClanRole role;
        public int trophies;
        public int donationsThisWeek;
        public DateTime lastSeen;
        public bool isOnline;
    }

    [Serializable]
    public class ChatMessage
    {
        public string senderId;
        public string senderName;
        public string message;
        public DateTime timestamp;
        public MessageType type;
        public int? donationCardId;
        public int? donationCount;
        public string replayLink;

        public enum MessageType
        {
            Normal,
            System,
            DonationRequest,
            ReplayShare
        }
    }

    public enum ClanRole
    {
        Leader,
        CoLeader,
        Elder,
        Member
    }

    public enum MemberAction
    {
        Promote,
        Demote,
        Kick
    }
}