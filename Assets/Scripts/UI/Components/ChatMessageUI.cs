using System;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.Data;
using CRClone.Network;
using CRClone.UI.Screens;

namespace CRClone.UI.Components
{
    public class ChatMessageUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image _senderAvatar;
        [SerializeField] private Text _senderNameText;
        [SerializeField] private Text _messageText;
        [SerializeField] private Text _timeText;
        [SerializeField] private GameObject _donationRequestContainer;
        [SerializeField] private Text _donationCardNameText;
        [SerializeField] private Text _donationCountText;
        [SerializeField] private Button _donateButton;
        [SerializeField] private GameObject _replayLinkContainer;
        [SerializeField] private Text _replayLinkText;
        [SerializeField] private Button _viewReplayButton;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Sprite _ownMessageBg;
        [SerializeField] private Sprite _otherMessageBg;
        [SerializeField] private Sprite _systemMessageBg;
        [SerializeField] private Sprite _donationMessageBg;

        private ChatMessage _message;
        private Action<int, string> _onDonateClicked;

        public void Initialize(ChatMessage message, Action<int, string> onDonateClicked)
        {
            _message = message;
            _onDonateClicked = onDonateClicked;

            var playerData = Services.Get<GameManager>().LocalPlayer;
            bool isOwnMessage = playerData != null && message.senderId == playerData.playerId.ToString();

            UpdateVisuals(isOwnMessage);
        }

        private void UpdateVisuals(bool isOwnMessage)
        {
            if (_senderNameText != null) _senderNameText.text = _message.senderName;
            if (_timeText != null) _timeText.text = _message.timestamp.ToString("HH:mm");

            if (_backgroundImage != null)
            {
                switch (_message.type)
                {
                    case ChatMessage.MessageType.System:
                        _backgroundImage.sprite = _systemMessageBg;
                        break;
                    case ChatMessage.MessageType.DonationRequest:
                        _backgroundImage.sprite = _donationMessageBg;
                        break;
                    default:
                        _backgroundImage.sprite = isOwnMessage ? _ownMessageBg : _otherMessageBg;
                        break;
                }
            }

            switch (_message.type)
            {
                case ChatMessage.MessageType.DonationRequest:
                    _messageText?.gameObject.SetActive(false);
                    _donationRequestContainer?.SetActive(true);
                    _replayLinkContainer?.SetActive(false);

                    if (_donationCardNameText != null && _message.donationCardId.HasValue)
                    {
                        var cardData = Services.Get<DataManager>().GetCard(_message.donationCardId.Value);
                        _donationCardNameText.text = cardData?.cardName ?? "Unknown Card";
                    }

                    if (_donationCountText != null && _message.donationCount.HasValue)
                    {
                        _donationCountText.text = $"x{_message.donationCount.Value}";
                    }

                    if (_donateButton != null)
                    {
                        _donateButton.onClick.RemoveAllListeners();
                        _donateButton.onClick.AddListener(() => 
                        {
                            if (_message.donationCardId.HasValue)
                            {
                                _onDonateClicked?.Invoke(_message.donationCardId.Value, _message.senderId);
                            }
                        });
                    }
                    break;

                case ChatMessage.MessageType.ReplayShare:
                    _messageText?.gameObject.SetActive(false);
                    _donationRequestContainer?.SetActive(false);
                    _replayLinkContainer?.SetActive(true);

                    if (_replayLinkText != null)
                    {
                        _replayLinkText.text = "Battle Replay";
                    }

                    if (_viewReplayButton != null)
                    {
                        _viewReplayButton.onClick.RemoveAllListeners();
                        _viewReplayButton.onClick.AddListener(() =>
                        {
                            if (!string.IsNullOrEmpty(_message.replayLink))
                            {
                                Services.Get<NetworkClient>().Send(new ReplayRequest { replayCode = _message.replayLink });
                            }
                        });
                    }
                    break;

                default:
                    _messageText?.gameObject.SetActive(true);
                    _donationRequestContainer?.SetActive(false);
                    _replayLinkContainer?.SetActive(false);

                    if (_messageText != null)
                    {
                        _messageText.text = _message.message;
                    }
                    break;
            }
        }
    }
}