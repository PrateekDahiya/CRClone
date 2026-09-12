using UnityEngine;
using UnityEngine.UI;

namespace CRClone.Battle.Presentation
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private float _width = 2f;
        [SerializeField] private float _height = 0.2f;
        [SerializeField] private float _yOffset = 1.5f;

        private RectTransform _rectTransform;
        private Canvas _canvas;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            
            if (_fillImage == null)
            {
                CreateDefaultHealthBar();
            }
        }

        private void CreateDefaultHealthBar()
        {
            // Create background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(transform);
            _backgroundImage = bgGO.AddComponent<Image>();
            _backgroundImage.color = Color.black;
            _backgroundImage.rectTransform.sizeDelta = new Vector2(_width, _height);
            _backgroundImage.rectTransform.anchoredPosition = Vector2.zero;

            // Create fill
            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(transform);
            _fillImage = fillGO.AddComponent<Image>();
            _fillImage.color = Color.green;
            _fillImage.rectTransform.sizeDelta = new Vector2(_width, _height);
            _fillImage.rectTransform.anchoredPosition = Vector2.zero;
            _fillImage.type = Image.Type.Filled;
            _fillImage.fillMethod = Image.FillMethod.Horizontal;
            _fillImage.fillOrigin = 0;
        }

        public void SetHealth(int current, int max)
        {
            if (max <= 0) return;
            
            float percent = Mathf.Clamp01((float)current / max);
            
            if (_fillImage != null)
            {
                _fillImage.fillAmount = percent;
                
                // Color based on health percentage
                if (percent > 0.6f)
                    _fillImage.color = Color.green;
                else if (percent > 0.3f)
                    _fillImage.color = Color.yellow;
                else
                    _fillImage.color = Color.red;
            }
        }

        public void SetOffset(float yOffset)
        {
            _yOffset = yOffset;
            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = new Vector2(0, _yOffset);
            }
        }
    }
}