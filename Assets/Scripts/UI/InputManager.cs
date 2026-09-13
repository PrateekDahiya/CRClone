using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CRClone.Core;

namespace CRClone.UI
{
    public enum InputActionType
    {
        None,
        // Navigation
        NavigateUp,
        NavigateDown,
        NavigateLeft,
        NavigateRight,
        Submit,
        Cancel,
        // Battle
        SelectCard1,
        SelectCard2,
        SelectCard3,
        SelectCard4,
        DeployCard,
        CancelDeploy,
        Pause,
        // Emotes
        Emote1,
        Emote2,
        Emote3,
        Emote4,
        // UI
        OpenDeckBuilder,
        OpenShop,
        OpenClan,
        OpenProfile,
        OpenSettings,
        // Accessibility
        TapToPlaceAlternative,
        DragAlternative
    }

    [Serializable]
    public class InputBinding
    {
        public InputActionType action;
        public Key keyboardKey;
        public GamepadButton gamepadButton;
        public string displayName;
        public bool isRemappable = true;
    }

    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [Header("Input Actions Asset")]
        [SerializeField] private InputActionAsset _inputActionsAsset;

        [Header("Default Bindings")]
        [SerializeField] private List<InputBinding> _defaultBindings = new List<InputBinding>
        {
            new InputBinding { action = InputActionType.NavigateUp, keyboardKey = Key.W, gamepadButton = GamepadButton.DpadUp, displayName = "Navigate Up" },
            new InputBinding { action = InputActionType.NavigateDown, keyboardKey = Key.S, gamepadButton = GamepadButton.DpadDown, displayName = "Navigate Down" },
            new InputBinding { action = InputActionType.NavigateLeft, keyboardKey = Key.A, gamepadButton = GamepadButton.DpadLeft, displayName = "Navigate Left" },
            new InputBinding { action = InputActionType.NavigateRight, keyboardKey = Key.D, gamepadButton = GamepadButton.DpadRight, displayName = "Navigate Right" },
            new InputBinding { action = InputActionType.Submit, keyboardKey = Key.Enter, gamepadButton = GamepadButton.A, displayName = "Submit / Select" },
            new InputBinding { action = InputActionType.Cancel, keyboardKey = Key.Escape, gamepadButton = GamepadButton.B, displayName = "Cancel / Back" },
            new InputBinding { action = InputActionType.SelectCard1, keyboardKey = Key.Digit1, gamepadButton = GamepadButton.RightTrigger, displayName = "Select Card 1" },
            new InputBinding { action = InputActionType.SelectCard2, keyboardKey = Key.Digit2, gamepadButton = GamepadButton.RightShoulder, displayName = "Select Card 2" },
            new InputBinding { action = InputActionType.SelectCard3, keyboardKey = Key.Digit3, gamepadButton = GamepadButton.LeftTrigger, displayName = "Select Card 3" },
            new InputBinding { action = InputActionType.SelectCard4, keyboardKey = Key.Digit4, gamepadButton = GamepadButton.LeftShoulder, displayName = "Select Card 4" },
            new InputBinding { action = InputActionType.DeployCard, keyboardKey = Key.Space, gamepadButton = GamepadButton.A, displayName = "Deploy Card" },
            new InputBinding { action = InputActionType.CancelDeploy, keyboardKey = Key.Escape, gamepadButton = GamepadButton.B, displayName = "Cancel Deploy" },
            new InputBinding { action = InputActionType.Pause, keyboardKey = Key.P, gamepadButton = GamepadButton.Start, displayName = "Pause" },
            new InputBinding { action = InputActionType.Emote1, keyboardKey = Key.F1, gamepadButton = GamepadButton.X, displayName = "Emote 1" },
            new InputBinding { action = InputActionType.Emote2, keyboardKey = Key.F2, gamepadButton = GamepadButton.Y, displayName = "Emote 2" },
            new InputBinding { action = InputActionType.Emote3, keyboardKey = Key.F3, gamepadButton = GamepadButton.LeftStick, displayName = "Emote 3" },
            new InputBinding { action = InputActionType.Emote4, keyboardKey = Key.F4, gamepadButton = GamepadButton.RightStick, displayName = "Emote 4" },
        };

        [Header("Settings")]
        [SerializeField] private bool _enableGamepadNavigation = true;
        [SerializeField] private float _gamepadRepeatDelay = 0.3f;
        [SerializeField] private float _gamepadRepeatRate = 0.1f;
        [SerializeField] private bool _tapAlternativeForDrag = true;

        private Dictionary<InputActionType, InputAction> _actions = new Dictionary<InputActionType, InputAction>();
        private Dictionary<InputActionType, InputBinding> _currentBindings = new Dictionary<InputActionType, InputBinding>();
        private Dictionary<InputActionType, float> _actionTimestamps = new Dictionary<InputActionType, float>();
        private Dictionary<InputActionType, bool> _actionHeld = new Dictionary<InputActionType, bool>();

        public bool EnableGamepadNavigation => _enableGamepadNavigation;
        public bool TapAlternativeForDrag => _tapAlternativeForDrag;

        public event Action<InputActionType> OnActionPressed;
        public event Action<InputActionType> OnActionReleased;
        public event Action<InputActionType> OnActionHeld;
        public event Action<InputBinding> OnBindingChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeActions();
            LoadBindings();
        }

        private void InitializeActions()
        {
            if (_inputActionsAsset != null)
            {
                foreach (var actionMap in _inputActionsAsset.actionMaps)
                {
                    foreach (var action in actionMap.actions)
                    {
                        if (Enum.TryParse<InputActionType>(action.name, out InputActionType type))
                        {
                            _actions[type] = action;
                            action.Enable();
                        }
                    }
                }
            }
            else
            {
                CreateDefaultActions();
            }
        }

        private void CreateDefaultActions()
        {
            foreach (var binding in _defaultBindings)
            {
                var action = new InputAction(binding.action.ToString(), InputActionType.Button);
                action.AddBinding($"<Keyboard>/{binding.keyboardKey}");
                action.AddBinding($"<Gamepad>/{binding.gamepadButton}");
                action.Enable();
                _actions[binding.action] = action;
            }
        }

        private void Update()
        {
            ProcessGamepadNavigation();
            CheckActionStates();
        }

        private void ProcessGamepadNavigation()
        {
            if (!_enableGamepadNavigation) return;

            CheckNavigation(InputActionType.NavigateUp, Vector2.up);
            CheckNavigation(InputActionType.NavigateDown, Vector2.down);
            CheckNavigation(InputActionType.NavigateLeft, Vector2.left);
            CheckNavigation(InputActionType.NavigateRight, Vector2.right);
        }

        private void CheckNavigation(InputActionType actionType, Vector2 direction)
        {
            if (!IsActionPressed(actionType)) return;

            float now = Time.unscaledTime;
            float lastTime = _actionTimestamps.GetValueOrDefault(actionType, 0f);
            bool wasHeld = _actionHeld.GetValueOrDefault(actionType, false);

            if (!wasHeld || now - lastTime >= (wasHeld ? _gamepadRepeatRate : _gamepadRepeatDelay))
            {
                _actionTimestamps[actionType] = now;
                _actionHeld[actionType] = true;
                OnActionHeld?.Invoke(actionType);
                NavigateUI(direction);
            }
        }

        private void NavigateUI(Vector2 direction)
        {
            if (EventSystem.current == null) return;

            GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
            if (currentSelected == null) return;

            Selectable currentSelectable = currentSelected.GetComponent<Selectable>();
            if (currentSelectable == null) return;

            Selectable nextSelectable = null;

            switch (direction)
            {
                case Vector2 v when v == Vector2.up:
                    nextSelectable = currentSelectable.FindSelectableOnUp();
                    break;
                case Vector2 v when v == Vector2.down:
                    nextSelectable = currentSelectable.FindSelectableOnDown();
                    break;
                case Vector2 v when v == Vector2.left:
                    nextSelectable = currentSelectable.FindSelectableOnLeft();
                    break;
                case Vector2 v when v == Vector2.right:
                    nextSelectable = currentSelectable.FindSelectableOnRight();
                    break;
            }

            if (nextSelectable != null)
            {
                nextSelectable.Select();
                UISoundPlayer.Instance?.PlayButtonHover();
            }
        }

        private void CheckActionStates()
        {
            foreach (var kvp in _actions)
            {
                InputActionType type = kvp.Key;
                InputAction action = kvp.Value;

                bool wasPressed = _actionHeld.GetValueOrDefault(type, false);
                bool isPressed = action.IsPressed();

                if (isPressed && !wasPressed)
                {
                    _actionHeld[type] = true;
                    OnActionPressed?.Invoke(type);
                }
                else if (!isPressed && wasPressed)
                {
                    _actionHeld[type] = false;
                    OnActionReleased?.Invoke(type);
                }
            }
        }

        public bool IsActionPressed(InputActionType type)
        {
            if (_actions.TryGetValue(type, out var action))
            {
                return action.IsPressed();
            }
            return false;
        }

        public bool WasActionPressedThisFrame(InputActionType type)
        {
            return _actionHeld.GetValueOrDefault(type, false) && _actions.TryGetValue(type, out var action) && action.WasPressedThisFrame();
        }

        public bool WasActionReleasedThisFrame(InputActionType type)
        {
            return !_actionHeld.GetValueOrDefault(type, false) && _actions.TryGetValue(type, out var action) && action.WasReleasedThisFrame();
        }

        public Vector2 GetNavigationInput()
        {
            Vector2 input = Vector2.zero;

            if (IsActionPressed(InputActionType.NavigateUp)) input.y += 1;
            if (IsActionPressed(InputActionType.NavigateDown)) input.y -= 1;
            if (IsActionPressed(InputActionType.NavigateLeft)) input.x -= 1;
            if (IsActionPressed(InputActionType.NavigateRight)) input.x += 1;

            return input;
        }

        public int GetSelectedCardIndex()
        {
            if (WasActionPressedThisFrame(InputActionType.SelectCard1)) return 0;
            if (WasActionPressedThisFrame(InputActionType.SelectCard2)) return 1;
            if (WasActionPressedThisFrame(InputActionType.SelectCard3)) return 2;
            if (WasActionPressedThisFrame(InputActionType.SelectCard4)) return 3;
            return -1;
        }

        public void RemapBinding(InputActionType actionType, Key newKey)
        {
            if (!_currentBindings.TryGetValue(actionType, out var binding) || !binding.isRemappable) return;

            binding.keyboardKey = newKey;
            SaveBinding(binding);
            RebuildAction(actionType, binding);
            OnBindingChanged?.Invoke(binding);
        }

        public void RemapBinding(InputActionType actionType, GamepadButton newButton)
        {
            if (!_currentBindings.TryGetValue(actionType, out var binding) || !binding.isRemappable) return;

            binding.gamepadButton = newButton;
            SaveBinding(binding);
            RebuildAction(actionType, binding);
            OnBindingChanged?.Invoke(binding);
        }

        public void ResetBinding(InputActionType actionType)
        {
            var defaultBinding = _defaultBindings.Find(b => b.action == actionType);
            if (defaultBinding == null) return;

            _currentBindings[actionType] = new InputBinding
            {
                action = defaultBinding.action,
                keyboardKey = defaultBinding.keyboardKey,
                gamepadButton = defaultBinding.gamepadButton,
                displayName = defaultBinding.displayName,
                isRemappable = defaultBinding.isRemappable
            };

            SaveBinding(_currentBindings[actionType]);
            RebuildAction(actionType, _currentBindings[actionType]);
            OnBindingChanged?.Invoke(_currentBindings[actionType]);
        }

        public void ResetAllBindings()
        {
            foreach (var defaultBinding in _defaultBindings)
            {
                _currentBindings[defaultBinding.action] = new InputBinding
                {
                    action = defaultBinding.action,
                    keyboardKey = defaultBinding.keyboardKey,
                    gamepadButton = defaultBinding.gamepadButton,
                    displayName = defaultBinding.displayName,
                    isRemappable = defaultBinding.isRemappable
                };
                SaveBinding(_currentBindings[defaultBinding.action]);
                RebuildAction(defaultBinding.action, _currentBindings[defaultBinding.action]);
            }
        }

        public InputBinding GetBinding(InputActionType actionType)
        {
            return _currentBindings.GetValueOrDefault(actionType);
        }

        public IReadOnlyList<InputBinding> GetAllBindings()
        {
            return _currentBindings.Values.ToList().AsReadOnly();
        }

        private void RebuildAction(InputActionType actionType, InputBinding binding)
        {
            if (_actions.TryGetValue(actionType, out var action))
            {
                action.Disable();
                action.RemoveAllBindings();
                action.AddBinding($"<Keyboard>/{binding.keyboardKey}");
                action.AddBinding($"<Gamepad>/{binding.gamepadButton}");
                action.Enable();
            }
        }

        private void LoadBindings()
        {
            foreach (var defaultBinding in _defaultBindings)
            {
                string keyPrefix = $"input_{actionType}_{defaultBinding.action}";

                Key savedKey = (Key)PlayerPrefs.GetInt($"{keyPrefix}_keyboard", (int)defaultBinding.keyboardKey);
                GamepadButton savedButton = (GamepadButton)PlayerPrefs.GetInt($"{keyPrefix}_gamepad", (int)defaultBinding.gamepadButton);

                _currentBindings[defaultBinding.action] = new InputBinding
                {
                    action = defaultBinding.action,
                    keyboardKey = savedKey,
                    gamepadButton = savedButton,
                    displayName = defaultBinding.displayName,
                    isRemappable = defaultBinding.isRemappable
                };

                RebuildAction(defaultBinding.action, _currentBindings[defaultBinding.action]);
            }
        }

        private void SaveBinding(InputBinding binding)
        {
            string keyPrefix = $"input_{binding.action}";

            PlayerPrefs.SetInt($"{keyPrefix}_keyboard", (int)binding.keyboardKey);
            PlayerPrefs.SetInt($"{keyPrefix}_gamepad", (int)binding.gamepadButton);
            PlayerPrefs.Save();
        }

        public void SetGamepadNavigationEnabled(bool enabled)
        {
            _enableGamepadNavigation = enabled;
            PlayerPrefs.SetInt("input_gamepad_navigation", enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetTapAlternativeForDrag(bool enabled)
        {
            _tapAlternativeForDrag = enabled;
            PlayerPrefs.SetInt("input_tap_alternative", enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void EnableAllActions()
        {
            foreach (var action in _actions.Values)
            {
                action.Enable();
            }
        }

        public void DisableAllActions()
        {
            foreach (var action in _actions.Values)
            {
                action.Disable();
            }
        }

        public void DisableAction(InputActionType type)
        {
            if (_actions.TryGetValue(type, out var action))
            {
                action.Disable();
            }
        }

        public void EnableAction(InputActionType type)
        {
            if (_actions.TryGetValue(type, out var action))
            {
                action.Enable();
            }
        }
    }

    public class GamepadNavigationHelper : MonoBehaviour
    {
        [SerializeField] private Selectable _firstSelected;
        [SerializeField] private bool _autoSelectOnEnable = true;

        private void OnEnable()
        {
            if (_autoSelectOnEnable && _firstSelected != null)
            {
                _firstSelected.Select();
            }

            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnActionPressed += OnActionPressed;
            }
        }

        private void OnDisable()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnActionPressed -= OnActionPressed;
            }
        }

        private void OnActionPressed(InputActionType actionType)
        {
            switch (actionType)
            {
                case InputActionType.Submit:
                    SubmitCurrentSelection();
                    break;
                case InputActionType.Cancel:
                    CancelCurrentSelection();
                    break;
            }
        }

        private void SubmitCurrentSelection()
        {
            if (EventSystem.current?.currentSelectedGameObject == null) return;

            var selectable = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>();
            if (selectable != null)
            {
                var button = selectable as Button;
                button?.onClick.Invoke();
            }
        }

        private void CancelCurrentSelection()
        {
            var modalController = FindObjectOfType<ModalController>();
            if (modalController != null)
            {
                modalController.Close();
            }
            else
            {
                UIManager.Instance?.ShowScreen(ScreenType.MainMenu);
            }
        }
    }

    public class TouchAlternativeHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [SerializeField] private bool _enableTapAlternative = true;
        [SerializeField] private float _tapThreshold = 0.2f;
        [SerializeField] private float _dragThreshold = 20f;

        private Vector2 _pressPosition;
        private float _pressTime;
        private bool _isDragging;

        public event Action<Vector2> OnTap;
        public event Action<Vector2> OnDragStart;
        public event Action<Vector2> OnDragged;
        public event Action<Vector2> OnDragEnd;

        private void Awake()
        {
            if (InputManager.Instance != null)
            {
                _enableTapAlternative = InputManager.Instance.TapAlternativeForDrag;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_enableTapAlternative) return;

            _pressPosition = eventData.position;
            _pressTime = Time.unscaledTime;
            _isDragging = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_enableTapAlternative) return;

            float pressDuration = Time.unscaledTime - _pressPosition;
            float dragDistance = Vector2.Distance(eventData.position, _pressPosition);

            if (!_isDragging && pressDuration < _tapThreshold && dragDistance < _dragThreshold)
            {
                OnTap?.Invoke(eventData.position);
            }
            else if (_isDragging)
            {
                OnDragEnd?.Invoke(eventData.position);
                _isDragging = false;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_enableTapAlternative) return;

            float dragDistance = Vector2.Distance(eventData.position, _pressPosition);

            if (!_isDragging && dragDistance >= _dragThreshold)
            {
                _isDragging = true;
                OnDragStart?.Invoke(_pressPosition);
            }

            if (_isDragging)
            {
                OnDragged?.Invoke(eventData.position);
            }
        }
    }
}