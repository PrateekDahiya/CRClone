using UnityEngine;

namespace CRClone.Battle.Presentation
{
    public class TeslaRetraction : MonoBehaviour
    {
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private Vector3 _retractedPosition = new Vector3(0, -2f, 0);
        [SerializeField] private float _retractionSpeed = 10f;
        [SerializeField] private ParticleSystem _retractParticles;
        [SerializeField] private ParticleSystem _extendParticles;

        private Vector3 _extendedPosition;
        private bool _isRetracted = false;
        private bool _targetRetracted = false;

        private void Awake()
        {
            if (_visualRoot == null)
                _visualRoot = transform;
            
            _extendedPosition = _visualRoot.localPosition;
        }

        private void Update()
        {
            if (_visualRoot == null) return;

            var targetPos = _targetRetracted ? _retractedPosition : _extendedPosition;
            _visualRoot.localPosition = Vector3.MoveTowards(_visualRoot.localPosition, targetPos, _retractionSpeed * Time.deltaTime);

            bool wasRetracted = _isRetracted;
            _isRetracted = Vector3.Distance(_visualRoot.localPosition, _retractedPosition) < 0.1f;

            if (_isRetracted != wasRetracted)
            {
                if (_isRetracted && _retractParticles != null)
                    _retractParticles.Play();
                else if (!_isRetracted && _extendParticles != null)
                    _extendParticles.Play();
            }
        }

        public void SetRetracted(bool retracted)
        {
            _targetRetracted = retracted;
        }

        public bool IsRetracted => _isRetracted;
    }
}