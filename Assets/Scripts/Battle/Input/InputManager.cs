using System;
using UnityEngine;
using UnityEngine.EventSystems;
using CRClone.Core;
using CRClone.Network;
using CRClone.Battle.Simulation;
using CRClone.Data;
using CRClone.Battle.UI;

namespace CRClone.Battle.Input
{
    public class InputManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera _battleCamera;
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private LayerMask _uiLayer;
        [SerializeField] private float _maxRaycastDistance = 100f;

        [Header("Visual Feedback")]
        [SerializeField] private GameObject _validPlacementIndicator;
        [SerializeField] private GameObject _invalidPlacementIndicator;
        [SerializeField] private GameObject _spellRadiusIndicator;

        private NetworkClient _networkClient;
        private HandBar _handBar;
        private bool _isDragging = false;
        private int _selectedCardIndex = -1;
        private int _selectedCardId = -1;
        private Vector3 _dragStartPosition;
        private bool _isOverUI = false;

        private void Awake()
        {
            _networkClient = Services.Get<NetworkClient>();
            _handBar = FindObjectOfType<HandBar>();
        }

        private void Update()
        {
            if (_simulation == null || _simulation.Status != BattleStatus.Playing) return;

            _isOverUI = EventSystem.current.IsPointerOverGameObject();

            HandleMouseInput();
            HandleTouchInput();
            UpdateDragVisuals();
        }

        private BattleSimulation _simulation => Services.Get<GameManager>().BattleSim;

        private void HandleMouseInput()
        {
            // Left click
            if (Input.GetMouseButtonDown(0) && !_isOverUI)
            {
                OnPointerDown(Input.mousePosition);
            }
            else if (Input.GetMouseButton(0) && _isDragging)
            {
                OnPointerDrag(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0) && _isDragging)
            {
                OnPointerUp(Input.mousePosition);
            }

            // Right click / Escape to cancel
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelSelection();
            }

            // Number keys 1-4 for quick card selection
            for (int i = 0; i < 4; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    SelectCardByIndex(i);
                }
            }
        }

        private void HandleTouchInput()
        {
            if (Input.touchCount == 0) return;

            var touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began && !_isOverUI)
            {
                OnPointerDown(touch.position);
            }
            else if (touch.phase == TouchPhase.Moved && _isDragging)
            {
                OnPointerDrag(touch.position);
            }
            else if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) && _isDragging)
            {
                OnPointerUp(touch.position);
            }
        }

        private void OnPointerDown(Vector2 screenPos)
        {
            // Check if clicking on a card in hand
            int cardIndex = _handBar?.GetCardIndexAtScreenPos(screenPos) ?? -1;
            
            if (cardIndex >= 0)
            {
                SelectCard(cardIndex);
                _dragStartPosition = screenPos;
            }
        }

        private void SelectCard(int index)
        {
            var cardId = _handBar?.GetCardIdAtIndex(index) ?? -1;
            if (cardId <= 0) return;

            var cardData = Services.Get<DataManager>().GetCard(cardId);
            if (cardData == null) return;

            // Check elixir
            var player = Services.Get<GameManager>().BattleSim.Player1;
            if (player.Elixir < cardData.elixirCost)
            {
                // Visual feedback: not enough elixir
                _handBar?.ShowInsufficientElixir(index);
                return;
            }

            _selectedCardIndex = index;
            _selectedCardId = cardId;
            _isDragging = true;

            _handBar?.SetCardSelected(index, true);

            // Show spell radius indicator for spells
            if (cardData.type == CardType.Spell)
            {
                ShowSpellRadius(cardData);
            }
        }

        private void SelectCardByIndex(int index)
        {
            if (_isDragging) CancelSelection();
            SelectCard(index);
        }

        private void OnPointerDrag(Vector2 screenPos)
        {
            if (!_isDragging) return;

            Vector3 worldPos = GetWorldPosition(screenPos);
            UpdatePlacementIndicator(worldPos);
        }

        private void OnPointerUp(Vector2 screenPos)
        {
            if (!_isDragging) return;

            Vector3 worldPos = GetWorldPosition(screenPos);
            
            if (IsValidPlacement(worldPos))
            {
                DeployCard(worldPos);
            }
            else
            {
                // Invalid placement - return to hand
                _handBar?.AnimateCardReturn(_selectedCardIndex);
            }

            CancelSelection();
        }

        private void CancelSelection()
        {
            if (!_isDragging) return;

            _handBar?.SetCardSelected(_selectedCardIndex, false);
            HideIndicators();
            _isDragging = false;
            _selectedCardIndex = -1;
            _selectedCardId = -1;
        }

        private void DeployCard(Vector3 worldPos)
        {
            var input = new PlayerInput
            {
                type = GetInputTypeForCard(_selectedCardId),
                cardId = (uint)_selectedCardId,
                position = new Vector2(worldPos.x, worldPos.y),
                clientTick = 0 // Will be set by network client
            };

            _networkClient?.SendInput(input);
            _handBar?.OnCardDeployed(_selectedCardIndex);
        }

        private InputType GetInputTypeForCard(int cardId)
        {
            var cardData = Services.Get<DataManager>().GetCard(cardId);
            return cardData.type == CardType.Spell ? InputType.CastSpell : InputType.PlayCard;
        }

        private bool IsValidPlacement(Vector3 worldPos)
        {
            var cardData = Services.Get<DataManager>().GetCard(_selectedCardId);
            if (cardData == null) return false;

            // Check deploy zone
            var sim = Services.Get<GameManager>().BattleSim;
            int playerId = 1; // Local player is always player 1
            
            float deployZoneMaxY = playerId == 1 ? GameConstants.DEPLOY_ZONE_Y_P1_MAX : GameConstants.DEPLOY_ZONE_Y_P2_MIN;
            
            if (playerId == 1 && worldPos.y > deployZoneMaxY) return false;
            if (playerId == 2 && worldPos.y < deployZoneMaxY) return false;

            // Spells can be placed anywhere
            if (cardData.type == CardType.Spell) return true;

            // Buildings must be on own side of river
            if (cardData.type == CardType.Building)
            {
                if (playerId == 1 && worldPos.y > GameConstants.RIVER_Y_MIN) return false;
                if (playerId == 2 && worldPos.y < GameConstants.RIVER_Y_MAX) return false;
            }

            return true;
        }

        private void UpdatePlacementIndicator(Vector3 worldPos)
        {
            bool valid = IsValidPlacement(worldPos);
            
            _validPlacementIndicator.SetActive(valid);
            _invalidPlacementIndicator.SetActive(!valid);

            Vector3 indicatorPos = worldPos;
            indicatorPos.z = -worldPos.y * 0.01f;
            
            _validPlacementIndicator.transform.position = indicatorPos;
            _invalidPlacementIndicator.transform.position = indicatorPos;

            // Update spell radius indicator
            if (_spellRadiusIndicator.activeSelf)
            {
                _spellRadiusIndicator.transform.position = indicatorPos;
            }
        }

        private void UpdateDragVisuals()
        {
            // TODO: Implement per-frame drag visual updates (placement validity, indicators).
        }

        private void ShowSpellRadius(CardData cardData)
        {
            if (cardData.type != CardType.Spell) return;

            float radius = GetSpellRadius(cardData);
            _spellRadiusIndicator.transform.localScale = Vector3.one * radius * 2f;
            _spellRadiusIndicator.SetActive(true);
        }

        private float GetSpellRadius(CardData cardData)
        {
            return cardData.cardName switch
            {
                "Fireball" => 2.5f,
                "Rocket" => 2f,
                "Lightning" => 3.5f,
                "Poison" => 3.5f,
                "Freeze" => 3f,
                "Rage" => 3.5f,
                "Tornado" => 5.5f,
                "Graveyard" => 4f,
                "Zap" => 2.5f,
                "Arrows" => 4f,
                "The Log" => 11.5f,
                "Giant Snowball" => 2.5f,
                "Earthquake" => 3.5f,
                "Royal Delivery" => 2.5f,
                "Barbarian Barrel" => 2.5f,
                "Clone" => 3f,
                _ => 2f
            };
        }

        private void HideIndicators()
        {
            _validPlacementIndicator.SetActive(false);
            _invalidPlacementIndicator.SetActive(false);
            _spellRadiusIndicator.SetActive(false);
        }

        private Vector3 GetWorldPosition(Vector2 screenPos)
        {
            var ray = _battleCamera.ScreenPointToRay(screenPos);
            var plane = new Plane(Vector3.forward, Vector3.zero);
            
            if (plane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }
            
            return Vector3.zero;
        }
    }
}