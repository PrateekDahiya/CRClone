using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.UI.Animation;

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

        public void Initialize()
        {
            _backButton?.onClick.AddListener(() => Services.Get<GameManager>().ChangeState(GameState.MainMenu));

            SetupTabs();
            LoadClanData();
            ShowTab(ClanTab.Chat);
        }

        private void SetupTabs()
        {
            _chatTab?.onClick.AddListener(() => ShowTab(ClanTab.Chat));
            _membersTab?.onClick.AddListener(() => ShowTab(ClanTab.Members));
            _warTab?.onClick.AddListener(() => ShowTab(ClanTab.War));
            _capitalTab?.onClick.AddListener(() => ShowTab(ClanTab.Capital));
            _settingsTab?.onClick.AddListener(() => ShowTab(ClanTab.Settings));

            _sendButton?.onClick.AddListener(OnSendMessage);
            _donateRequestButton?.onClick.AddListener(OnDonateRequest);
            _warParticipateButton?.onClick.AddListener(OnWarParticipate);
        }

        private void ShowTab(ClanTab tab)
        {
            _currentTab = tab;

            _chatContent?.gameObject.SetActive(tab == ClanTab.Chat);
            _membersContent?.gameObject.SetActive(tab == ClanTab.Members);
            _warContent?.gameObject.SetActive(tab == ClanTab.War);
            _capitalContent?.gameObject.SetActive(tab == ClanTab.Capital);
            _settingsContent?.gameObject.SetActive(tab == ClanTab.Settings);

            UpdateTabButtons();

            if (tab == ClanTab.Members)
            {
                RefreshMembersList();
            }
            else if (tab == ClanTab.Chat)
            {
                RefreshChatMessages();
            }
        }

        private void UpdateTabButtons()
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

        private void LoadClanData()
        {
            var playerData = Services.Get<GameManager>().LocalPlayer;
            if (playerData?.clan != null)
            {
                var clan = playerData.clan;
                if (_clanNameText != null) _clanNameText.text = clan.name;
                if (_clanTrophyReqText != null) _clanTrophyReqText.text = $"Trophy Req: {clan.trophyRequirement:N0}";
                if (_clanMembersText != null) _clanMembersText.text = $"Members: {clan.memberCount}/50";

                _members = clan.members ?? new List<ClanMember>();
                _messages = clan.messages ?? new List<ChatMessage>();
            }
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
                var memberUI = memberGO.GetComponent<PlayerListItemUI>();
                if (memberUI != null)
                {
                    memberUI.Initialize(member, OnMemberAction);
                }
            }
        }

        private void RefreshChatMessages()
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

        private void ScrollToBottom()
        {
            var scrollRect = _chatMessagesContainer.GetComponentInParent<ScrollRect>();
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

            Services.Get<NetworkClient>().Send(new NetworkClient.ClanChatMessage { message = message });
        }

        private void OnDonateRequest()
        {
            var modal = UIManager.Instance?.ShowModal(Resources.Load<GameObject>("UI/DonateRequestModal"));
            var modalUI = modal?.GetComponent<DonateRequestModal>();
            if (modalUI != null)
            {
                modalUI.Initialize(OnDonateRequestConfirmed);
            }
        }

        private void OnDonateRequestConfirmed(int cardId, int count)
        {
            Services.Get<NetworkClient>().Send(new NetworkClient.DonationRequest { cardId = cardId, count = count });
        }

        private void OnDonateClicked(int cardId, string requesterId)
        {
            var playerData = Services.Get<GameManager>().LocalPlayer;
            if (playerData?.collection.ContainsKey(cardId) == true)
            {
                int ownedCount = playerData.collection[cardId];
                int maxDonate = GetMaxDonation(cardId);

                if (ownedCount >= maxDonate)
                {
                    Services.Get<NetworkClient>().Send(new NetworkClient.DonateCard { cardId = cardId, recipientId = requesterId, count = maxDonate });
                    EventBus.RaiseToast("Card donated!");
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

        private void OnMemberAction(ClanMember member, MemberAction action)
        {
            switch (action)
            {
                case MemberAction.Promote:
                    Services.Get<NetworkClient>().Send(new NetworkClient.ClanMemberAction { targetId = member.playerId, action = "promote" });
                    break;
                case MemberAction.Demote:
                    Services.Get<NetworkClient>().Send(new NetworkClient.ClanMemberAction { targetId = member.playerId, action = "demote" });
                    break;
                case MemberAction.Kick:
                    Services.Get<NetworkClient>().Send(new NetworkClient.ClanMemberAction { targetId = member.playerId, action = "kick" });
                    break;
            }
        }

        private void OnWarParticipate()
        {
            Services.Get<NetworkClient>().Send(new NetworkClient.WarAction { action = "participate" });
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
        public string messageId;
        public string senderId;
        public string senderName;
        public string message;
        public DateTime timestamp;
        public MessageType type;
        public int? donationCardId;
        public int? donationCount;
        public string replayLink;
    }

    public enum MessageType
    {
        Normal,
        System,
        DonationRequest,
        ReplayShare,
        BattleResult
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