using System;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.UI.Screens;

namespace CRClone.UI.Components
{
    public class PlayerListItemUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image _avatarImage;
        [SerializeField] private Text _playerNameText;
        [SerializeField] private Text _roleText;
        [SerializeField] private Image _roleBadge;
        [SerializeField] private Text _trophiesText;
        [SerializeField] private Text _donationsText;
        [SerializeField] private Button _promoteButton;
        [SerializeField] private Button _demoteButton;
        [SerializeField] private Button _kickButton;
        [SerializeField] private GameObject _onlineIndicator;
        [SerializeField] private Sprite _leaderBadge;
        [SerializeField] private Sprite _coLeaderBadge;
        [SerializeField] private Sprite _elderBadge;
        [SerializeField] private Sprite _memberBadge;

        private ClanMember _member;
        private Action<ClanMember, MemberAction> _onAction;

        public void Initialize(ClanMember member, Action<ClanMember, MemberAction> onAction)
        {
            _member = member;
            _onAction = onAction;

            var playerData = Services.Get<GameManager>().LocalPlayer;
            bool isSelf = playerData != null && member.playerId == playerData.playerId.ToString();
            bool canManage = playerData != null && 
                            (playerData.clanRole == ClanRole.Leader || 
                             playerData.clanRole == ClanRole.CoLeader);

            UpdateVisuals(isSelf, canManage);
        }

        private void UpdateVisuals(bool isSelf, bool canManage)
        {
            if (_playerNameText != null) _playerNameText.text = _member.playerName + (isSelf ? " (You)" : "");
            if (_trophiesText != null) _trophiesText.text = _member.trophies.ToString("N0");
            if (_donationsText != null) _donationsText.text = $"Donations: {_member.donationsThisWeek}/wk";

            if (_roleBadge != null)
            {
                _roleBadge.sprite = _member.role switch
                {
                    ClanRole.Leader => _leaderBadge,
                    ClanRole.CoLeader => _coLeaderBadge,
                    ClanRole.Elder => _elderBadge,
                    ClanRole.Member => _memberBadge,
                    _ => _memberBadge
                };
            }

            if (_roleText != null)
            {
                _roleText.text = _member.role.ToString();
            }

            if (_onlineIndicator != null)
            {
                _onlineIndicator.SetActive(_member.isOnline);
            }

            bool showActions = canManage && !isSelf && _member.role != ClanRole.Leader;
            bool canDemote = canManage && !isSelf && _member.role == ClanRole.Elder;
            bool canKick = canManage && !isSelf && _member.role != ClanRole.Leader && _member.role != ClanRole.CoLeader;

            _promoteButton?.gameObject.SetActive(showActions && _member.role == ClanRole.Member);
            _demoteButton?.gameObject.SetActive(canDemote);
            _kickButton?.gameObject.SetActive(canKick);

            if (_promoteButton != null)
            {
                _promoteButton.onClick.RemoveAllListeners();
                _promoteButton.onClick.AddListener(() => _onAction?.Invoke(_member, MemberAction.Promote));
            }

            if (_demoteButton != null)
            {
                _demoteButton.onClick.RemoveAllListeners();
                _demoteButton.onClick.AddListener(() => _onAction?.Invoke(_member, MemberAction.Demote));
            }

            if (_kickButton != null)
            {
                _kickButton.onClick.RemoveAllListeners();
                _kickButton.onClick.AddListener(() => _onAction?.Invoke(_member, MemberAction.Kick));
            }
        }
    }
}