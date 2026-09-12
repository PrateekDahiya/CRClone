using System;
using UnityEngine;
using UnityEngine.UI;

namespace CRClone.UI
{
    public enum DeviceBreakpoint
    {
        MobilePortrait,
        MobileLandscape,
        Tablet,
        Desktop,
        LargeDesktop
    }

    public class ResponsiveLayout : MonoBehaviour
    {
        public static ResponsiveLayout Instance { get; private set; }

        [Header("Breakpoints")]
        [SerializeField] private int _mobilePortraitMax = 767;
        [SerializeField] private int _mobileLandscapeMax = 1023;
        [SerializeField] private int _tabletMax = 1365;
        [SerializeField] private int _desktopMax = 1919;

        [Header("Scale Factors")]
        [SerializeField] private float _mobilePortraitScale = 0.8f;
        [SerializeField] private float _mobileLandscapeScale = 1.0f;
        [SerializeField] private float _tabletScale = 1.1f;
        [SerializeField] private float _desktopScale = 1.2f;
        [SerializeField] private float _largeDesktopScale = 1.3f;

        [Header("Safe Areas")]
        [SerializeField] private bool _useSafeArea = true;
        [SerializeField] private RectTransform _safeAreaContainer;

        private DeviceBreakpoint _currentBreakpoint;
        private float _currentScale;
        private Rect _safeArea;
        private CanvasScaler _canvasScaler;

        public DeviceBreakpoint CurrentBreakpoint => _currentBreakpoint;
        public float CurrentScale => _currentScale;
        public Rect SafeArea => _safeArea;
        public bool IsMobile => _currentBreakpoint == DeviceBreakpoint.MobilePortrait || _currentBreakpoint == DeviceBreakpoint.MobileLandscape;
        public bool IsTablet => _currentBreakpoint == DeviceBreakpoint.Tablet;
        public bool IsDesktop => _currentBreakpoint == DeviceBreakpoint.Desktop || _currentBreakpoint == DeviceBreakpoint.LargeDesktop;

        public event Action<DeviceBreakpoint> OnBreakpointChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _canvasScaler = GetComponent<CanvasScaler>();
            if (_canvasScaler == null) _canvasScaler = gameObject.AddComponent<CanvasScaler>();

            UpdateBreakpoint();
            UpdateSafeArea();
            ApplyLayout();
        }

        private void Update()
        {
            DeviceBreakpoint previousBreakpoint = _currentBreakpoint;
            UpdateBreakpoint();
            UpdateSafeArea();

            if (_currentBreakpoint != previousBreakpoint)
            {
                ApplyLayout();
                OnBreakpointChanged?.Invoke(_currentBreakpoint);
            }
        }

        private void UpdateBreakpoint()
        {
            int width = Screen.width;

            if (width <= _mobilePortraitMax)
                _currentBreakpoint = DeviceBreakpoint.MobilePortrait;
            else if (width <= _mobileLandscapeMax)
                _currentBreakpoint = DeviceBreakpoint.MobileLandscape;
            else if (width <= _tabletMax)
                _currentBreakpoint = DeviceBreakpoint.Tablet;
            else if (width <= _desktopMax)
                _currentBreakpoint = DeviceBreakpoint.Desktop;
            else
                _currentBreakpoint = DeviceBreakpoint.LargeDesktop;

            _currentScale = _currentBreakpoint switch
            {
                DeviceBreakpoint.MobilePortrait => _mobilePortraitScale,
                DeviceBreakpoint.MobileLandscape => _mobileLandscapeScale,
                DeviceBreakpoint.Tablet => _tabletScale,
                DeviceBreakpoint.Desktop => _desktopScale,
                DeviceBreakpoint.LargeDesktop => _largeDesktopScale,
                _ => 1f
            };
        }

        private void UpdateSafeArea()
        {
            if (!_useSafeArea) return;

            Rect newSafeArea = Screen.safeArea;
            if (newSafeArea != _safeArea)
            {
                _safeArea = newSafeArea;
                ApplySafeArea();
            }
        }

        private void ApplyLayout()
        {
            if (_canvasScaler != null)
            {
                _canvasScaler.matchWidthOrHeight = 0.5f;
                _canvasScaler.referenceResolution = new Vector2(1920, 1080) * _currentScale;
            }
        }

        private void ApplySafeArea()
        {
            if (_safeAreaContainer == null) return;

            Vector2 anchorMin = Vector2.zero;
            Vector2 anchorMax = Vector2.one;

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            anchorMin.x = _safeArea.xMin / screenWidth;
            anchorMin.y = _safeArea.yMin / screenHeight;
            anchorMax.x = _safeArea.xMax / screenWidth;
            anchorMax.y = _safeArea.yMax / screenHeight;

            _safeAreaContainer.anchorMin = anchorMin;
            _safeAreaContainer.anchorMax = anchorMax;
            _safeAreaContainer.offsetMin = Vector2.zero;
            _safeAreaContainer.offsetMax = Vector2.zero;
        }

        public void SetSafeAreaContainer(RectTransform container)
        {
            _safeAreaContainer = container;
            ApplySafeArea();
        }

        public float GetScaledSize(float baseSize)
        {
            return baseSize * _currentScale;
        }

        public Vector2 GetScaledSize(Vector2 baseSize)
        {
            return baseSize * _currentScale;
        }

        public int GetScaledInt(int baseValue)
        {
            return Mathf.RoundToInt(baseValue * _currentScale);
        }

        #if UNITY_EDITOR
        public void SimulateBreakpoint(DeviceBreakpoint breakpoint)
        {
            _currentBreakpoint = breakpoint;
            _currentScale = breakpoint switch
            {
                DeviceBreakpoint.MobilePortrait => _mobilePortraitScale,
                DeviceBreakpoint.MobileLandscape => _mobileLandscapeScale,
                DeviceBreakpoint.Tablet => _tabletScale,
                DeviceBreakpoint.Desktop => _desktopScale,
                DeviceBreakpoint.LargeDesktop => _largeDesktopScale,
                _ => 1f
            };
            ApplyLayout();
            OnBreakpointChanged?.Invoke(_currentBreakpoint);
        }

        public void SimulateSafeArea(Rect safeArea)
        {
            _safeArea = safeArea;
            ApplySafeArea();
        }
        #endif
    }

    [RequireComponent(typeof(RectTransform))]
    public class ResponsiveElement : MonoBehaviour
    {
        [Header("Responsive Settings")]
        [SerializeField] private bool _scaleWithBreakpoint = true;
        [SerializeField] private bool _adjustAnchorsForSafeArea = false;
        [SerializeField] private Vector2 _baseSize = Vector2.zero;
        [SerializeField] private Vector2 _mobilePortraitSize = Vector2.zero;
        [SerializeField] private Vector2 _mobileLandscapeSize = Vector2.zero;
        [SerializeField] private Vector2 _tabletSize = Vector2.zero;
        [SerializeField] private Vector2 _desktopSize = Vector2.zero;
        [SerializeField] private Vector2 _largeDesktopSize = Vector2.zero;

        private RectTransform _rectTransform;
        private Vector2 _originalSize;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _originalSize = _baseSize != Vector2.zero ? _baseSize : _rectTransform.sizeDelta;

            if (ResponsiveLayout.Instance != null)
            {
                ResponsiveLayout.Instance.OnBreakpointChanged += OnBreakpointChanged;
                ApplySizeForBreakpoint(ResponsiveLayout.Instance.CurrentBreakpoint);
            }
        }

        private void OnDestroy()
        {
            if (ResponsiveLayout.Instance != null)
            {
                ResponsiveLayout.Instance.OnBreakpointChanged -= OnBreakpointChanged;
            }
        }

        private void OnBreakpointChanged(DeviceBreakpoint breakpoint)
        {
            ApplySizeForBreakpoint(breakpoint);
        }

        private void ApplySizeForBreakpoint(DeviceBreakpoint breakpoint)
        {
            if (!_scaleWithBreakpoint) return;

            Vector2 targetSize = breakpoint switch
            {
                DeviceBreakpoint.MobilePortrait => _mobilePortraitSize != Vector2.zero ? _mobilePortraitSize : _originalSize * ResponsiveLayout.Instance.CurrentScale,
                DeviceBreakpoint.MobileLandscape => _mobileLandscapeSize != Vector2.zero ? _mobileLandscapeSize : _originalSize * ResponsiveLayout.Instance.CurrentScale,
                DeviceBreakpoint.Tablet => _tabletSize != Vector2.zero ? _tabletSize : _originalSize * ResponsiveLayout.Instance.CurrentScale,
                DeviceBreakpoint.Desktop => _desktopSize != Vector2.zero ? _desktopSize : _originalSize * ResponsiveLayout.Instance.CurrentScale,
                DeviceBreakpoint.LargeDesktop => _largeDesktopSize != Vector2.zero ? _largeDesktopSize : _originalSize * ResponsiveLayout.Instance.CurrentScale,
                _ => _originalSize
            };

            _rectTransform.sizeDelta = targetSize;
        }
    }

    public class SafeAreaHandler : MonoBehaviour
    {
        [SerializeField] private RectTransform _targetRect;
        [SerializeField] private bool _applyTop = true;
        [SerializeField] private bool _applyBottom = true;
        [SerializeField] private bool _applyLeft = false;
        [SerializeField] private bool _applyRight = false;

        private void Awake()
        {
            if (_targetRect == null) _targetRect = GetComponent<RectTransform>();

            if (ResponsiveLayout.Instance != null)
            {
                ResponsiveLayout.Instance.OnBreakpointChanged += OnBreakpointChanged;
                ApplySafeArea();
            }
        }

        private void OnDestroy()
        {
            if (ResponsiveLayout.Instance != null)
            {
                ResponsiveLayout.Instance.OnBreakpointChanged -= OnBreakpointChanged;
            }
        }

        private void OnBreakpointChanged(DeviceBreakpoint breakpoint)
        {
            ApplySafeArea();
        }

        private void ApplySafeArea()
        {
            if (ResponsiveLayout.Instance == null || _targetRect == null) return;

            Rect safeArea = ResponsiveLayout.Instance.SafeArea;
            Vector2 anchorMin = _targetRect.anchorMin;
            Vector2 anchorMax = _targetRect.anchorMax;

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            if (_applyTop) anchorMax.y = safeArea.yMax / screenHeight;
            if (_applyBottom) anchorMin.y = safeArea.yMin / screenHeight;
            if (_applyLeft) anchorMin.x = safeArea.xMin / screenWidth;
            if (_applyRight) anchorMax.x = safeArea.xMax / screenWidth;

            _targetRect.anchorMin = anchorMin;
            _targetRect.anchorMax = anchorMax;
        }
    }
}