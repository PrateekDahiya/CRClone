using UnityEngine;
using UnityEngine.UI;

namespace CRClone.Battle.UI
{
    public class ElixirBar : MonoBehaviour
    {
        [SerializeField] private Image[] _elixirSegments = new Image[10];
        [SerializeField] private Color _fullColor = Color.cyan;
        [SerializeField] private Color _emptyColor = new Color(0.2f, 0.2f, 0.3f);
        [SerializeField] private Color _generatingColor = Color.white;

        private PlayerState _player;
        private float _previousElixir = 0f;

        private void Start()
        {
            _player = Services.Get<GameManager>().BattleSim?.Player1;
        }

        private void Update()
        {
            if (_player == null) return;

            float elixir = _player.Elixir;
            int fullSegments = Mathf.FloorToInt(elixir);
            float partial = elixir - fullSegments;

            for (int i = 0; i < 10; i++)
            {
                if (i < fullSegments)
                {
                    _elixirSegments[i].fillAmount = 1f;
                    _elixirSegments[i].color = _fullColor;
                }
                else if (i == fullSegments && partial > 0)
                {
                    _elixirSegments[i].fillAmount = partial;
                    _elixirSegments[i].color = _generatingColor;
                }
                else
                {
                    _elixirSegments[i].fillAmount = 0f;
                    _elixirSegments[i].color = _emptyColor;
                }
            }

            // Pulse animation on elixir gain
            if (elixir > _previousElixir && fullSegments > Mathf.FloorToInt(_previousElixir))
            {
                int newSegment = fullSegments - 1;
                if (newSegment >= 0 && newSegment < 10)
                {
                    StartCoroutine(PulseSegment(newSegment));
                }
            }

            _previousElixir = elixir;
        }

        private System.Collections.IEnumerator PulseSegment(int index)
        {
            var segment = _elixirSegments[index];
            Color original = segment.color;
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                segment.color = Color.Lerp(_generatingColor, _fullColor, t);
                yield return null;
            }
            segment.color = _fullColor;
        }
    }
}