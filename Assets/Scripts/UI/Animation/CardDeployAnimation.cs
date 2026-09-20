using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CRClone.Core;
using CRClone.Data;
using CRClone.Battle.UI;
using CRClone.UI.Animation;

namespace CRClone.UI.Animation
{
    public class CardDeployAnimation : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HandBar _handBar;
        [SerializeField] private RectTransform _deployZoneIndicator;
        [SerializeField] private Image _spellRadiusPreview;
        [SerializeField] private GameObject _invalidPlacementIndicator;

        [Header("Animation Settings")]
        [SerializeField] private float _selectScale = 1.2f;
        [SerializeField] private float _selectDuration = 0.15f;
        [SerializeField] private float _deployDuration = 0.3f;
        [SerializeField] private float _drawDuration = 0.3f;
        [SerializeField] private AnimationCurve _selectCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve _deployCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Visual Feedback")]
        [SerializeField] private Color _validPlacementColor = new Color(0f, 1f, 0f, 0.3f);
        [SerializeField] private Color _invalidPlacementColor = new Color(1f, 0f, 0f, 0.3f);
        [SerializeField] private float _invalidShakeMagnitude = 10f;
        [SerializeField] private float _invalidShakeDuration = 0.2f;

        private int _selectedCardIndex = -1;
        private CardData _selectedCardData;
        private Vector2 _deployPosition;
        private bool _isAiming;
        private bool _isSpell;
        private float _spellRadius;

        public event Action<int, Vector2> OnCardDeployed;

        private void Update()
        {
            if (!_isAiming) return;

            UpdateDeployPreview();
        }

        public void StartCardSelection(int cardIndex, Vector2 screenPosition)
        {
            _selectedCardIndex = cardIndex;
            _selectedCardData = GetCardData(cardIndex);

            if (_selectedCardData == null) return;

            _isSpell = _selectedCardData.type == CardType.Spell;
            _spellRadius = GetSpellRadius(_selectedCardData);

            _isAiming = true;
            _deployPosition = ScreenToWorldPoint(screenPosition);

            ShowSelectionFeedback();
            ShowDeployZone();
        }

        public void UpdateAim(Vector2 screenPosition)
        {
            if (!_isAiming) return;

            _deployPosition = ScreenToWorldPoint(screenPosition);
            UpdateDeployPreview();
        }

        public void ConfirmDeploy()
        {
            if (!_isAiming || _selectedCardIndex < 0) return;

            bool validPlacement = ValidatePlacement(_deployPosition);

            if (validPlacement)
            {
                PlayDeployAnimation();
                OnCardDeployed?.Invoke(_selectedCardIndex, _deployPosition);
            }
            else
            {
                PlayInvalidPlacementFeedback();
            }

            EndAiming();
        }

        public void CancelDeploy()
        {
            if (!_isAiming) return;

            EndAiming();
            _handBar?.AnimateCardReturn(_selectedCardIndex);
        }

        private void EndAiming()
        {
            _isAiming = false;
            HideDeployZone();
            HideSpellRadius();
            HideInvalidIndicator();
        }

        private void ShowSelectionFeedback()
        {
            _handBar?.SetCardSelected(_selectedCardIndex, true);
        }

        private void ShowDeployZone()
        {
            if (_deployZoneIndicator != null)
            {
                _deployZoneIndicator.gameObject.SetActive(true);
            }
        }

        private void HideDeployZone()
        {
            if (_deployZoneIndicator != null)
            {
                _deployZoneIndicator.gameObject.SetActive(false);
            }
        }

        private void UpdateDeployPreview()
        {
            if (_deployZoneIndicator != null)
            {
                _deployZoneIndicator.position = _deployPosition;
            }

            if (_isSpell && _spellRadiusPreview != null)
            {
                _spellRadiusPreview.gameObject.SetActive(true);
                _spellRadiusPreview.transform.position = _deployPosition;
                _spellRadiusPreview.rectTransform.sizeDelta = Vector2.one * _spellRadius * 2f;

                bool valid = ValidatePlacement(_deployPosition);
                _spellRadiusPreview.color = valid ? _validPlacementColor : _invalidPlacementColor;
            }
        }

        private void ShowSpellRadius()
        {
            if (_spellRadiusPreview != null)
            {
                _spellRadiusPreview.gameObject.SetActive(true);
            }
        }

        private void HideSpellRadius()
        {
            if (_spellRadiusPreview != null)
            {
                _spellRadiusPreview.gameObject.SetActive(false);
            }
        }

        private void ShowInvalidIndicator()
        {
            if (_invalidPlacementIndicator != null)
            {
                _invalidPlacementIndicator.SetActive(true);
                _invalidPlacementIndicator.transform.position = _deployPosition;
            }
        }

        private void HideInvalidIndicator()
        {
            if (_invalidPlacementIndicator != null)
            {
                _invalidPlacementIndicator.SetActive(false);
            }
        }

        private void PlayDeployAnimation()
        {
            _handBar?.OnCardDeployed(_selectedCardIndex);
        }

        private void PlayInvalidPlacementFeedback()
        {
            UISoundPlayer.Instance?.PlayError();

            if (_invalidPlacementIndicator != null)
            {
                StartCoroutine(ShakeInvalidIndicator());
            }

            _handBar?.ShowInsufficientElixir(_selectedCardIndex);
        }

        private IEnumerator ShakeInvalidIndicator()
        {
            ShowInvalidIndicator();

            Vector3 originalPos = _invalidPlacementIndicator.transform.localPosition;
            float elapsed = 0f;

            while (elapsed < _invalidShakeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _invalidShakeDuration;
                float magnitude = _invalidShakeMagnitude * (1f - t);

                float offsetX = UnityEngine.Random.Range(-magnitude, magnitude);
                float offsetY = UnityEngine.Random.Range(-magnitude, magnitude);

                _invalidPlacementIndicator.transform.localPosition = originalPos + new Vector3(offsetX, offsetY, 0);
                yield return null;
            }

            _invalidPlacementIndicator.transform.localPosition = originalPos;
            HideInvalidIndicator();
        }

        private bool ValidatePlacement(Vector2 position)
        {
            if (_selectedCardData == null) return false;

            var config = Services.Get<GameManager>().BattleSim?._config;
            if (config == null) return false;

            float deployZoneY = CRClone.Core.GameConstants.DEPLOY_ZONE_Y_P1_MAX;

            if (position.y > deployZoneY)
            {
                return _isSpell;
            }

            return true;
        }

        private CardData GetCardData(int cardIndex)
        {
            var playerState = Services.Get<GameManager>().BattleSim?.Player1;
            if (playerState?.Hand == null) return null;

            int cardId = cardIndex < playerState.Hand.Length ? playerState.Hand[cardIndex] : 0;
            if (cardId <= 0) return null;

            return Services.Get<DataManager>().GetCard(cardId);
        }

        private float GetSpellRadius(CardData cardData)
        {
            if (cardData.mechanicsJson != null)
            {
                // Parse mechanicsJson for radius
            }
            return 3f;
        }

        private Vector2 ScreenToWorldPoint(Vector2 screenPos)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                return cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
            }
            return screenPos;
        }
    }
}