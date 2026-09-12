using UnityEngine;

namespace CRClone.Battle.Presentation
{
    public class SelectionRing : MonoBehaviour
    {
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private float _radius = 0.6f;
        [SerializeField] private int _segments = 32;
        [SerializeField] private Color _color = Color.yellow;
        [SerializeField] private float _width = 0.05f;
        [SerializeField] private float _pulseSpeed = 3f;
        [SerializeField] private float _pulseAmount = 0.2f;

        private Material _material;
        private float _baseRadius;

        private void Awake()
        {
            _baseRadius = _radius;
            
            if (_lineRenderer == null)
            {
                _lineRenderer = GetComponent<LineRenderer>();
            }

            if (_lineRenderer != null)
            {
                _lineRenderer.positionCount = _segments + 1;
                _lineRenderer.widthMultiplier = _width;
                _lineRenderer.loop = true;
                _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                _lineRenderer.material.color = _color;
                _material = _lineRenderer.material;
                GenerateRing();
            }
        }

        private void Start()
        {
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy) return;

            // Pulse animation
            float pulse = 1f + Mathf.Sin(Time.time * _pulseSpeed) * _pulseAmount;
            _lineRenderer.widthMultiplier = _width * pulse;
            
            // Color pulse
            if (_material != null)
            {
                float alpha = 0.5f + Mathf.Sin(Time.time * _pulseSpeed) * 0.3f;
                var c = _color;
                c.a = alpha;
                _material.color = c;
            }
        }

        private void GenerateRing()
        {
            var positions = new Vector3[_segments + 1];
            for (int i = 0; i <= _segments; i++)
            {
                float angle = i * Mathf.PI * 2f / _segments;
                positions[i] = new Vector3(Mathf.Cos(angle) * _radius, Mathf.Sin(angle) * _radius, 0);
            }
            _lineRenderer.SetPositions(positions);
        }

        public void SetRadius(float radius)
        {
            _radius = radius;
            _baseRadius = radius;
            GenerateRing();
        }

        public void SetColor(Color color)
        {
            _color = color;
            if (_lineRenderer != null && _lineRenderer.material != null)
            {
                _lineRenderer.material.color = color;
            }
        }
    }
}