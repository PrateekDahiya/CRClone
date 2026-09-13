using System;
using UnityEngine;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.Network;

namespace CRClone.UI.Components
{
    public class ChangeNameModal : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private InputField _nameInput;
        [SerializeField] private Text _errorText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        private Action<string> _onNameChanged;

        private void Awake()
        {
            _confirmButton?.onClick.AddListener(OnConfirm);
            _cancelButton?.onClick.AddListener(OnCancel);
            _nameInput?.onValueChanged.AddListener(OnNameInputChanged);
        }

        public void Initialize(Action<string> onNameChanged)
        {
            _onNameChanged = onNameChanged;
            _errorText?.gameObject.SetActive(false);
            if (_nameInput != null) _nameInput.text = "";
        }

        private void OnNameInputChanged(string text)
        {
            _errorText?.gameObject.SetActive(false);
        }

        private void OnConfirm()
        {
            string newName = _nameInput?.text?.Trim() ?? "";

            if (string.IsNullOrEmpty(newName))
            {
                ShowError("Name cannot be empty");
                return;
            }

            if (newName.Length < 3)
            {
                ShowError("Name must be at least 3 characters");
                return;
            }

            if (newName.Length > 15)
            {
                ShowError("Name must be at most 15 characters");
                return;
            }

            // Check for invalid characters
            if (!System.Text.RegularExpressions.Regex.IsMatch(newName, @"^[a-zA-Z0-9_\-]+$"))
            {
                ShowError("Name can only contain letters, numbers, underscore and hyphen");
                return;
            }

            _onNameChanged?.Invoke(newName);
            Destroy(gameObject);
        }

        private void OnCancel()
        {
            Destroy(gameObject);
        }

        private void ShowError(string message)
        {
            if (_errorText != null)
            {
                _errorText.text = message;
                _errorText.gameObject.SetActive(true);
            }
        }
    }
}