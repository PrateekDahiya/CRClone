using UnityEngine;

namespace CRClone.Battle.Presentation
{
    public class RiverRenderer : MonoBehaviour
    {
        [SerializeField] private Material _riverMaterial;
        [SerializeField] private float _flowSpeed = 1f;
        [SerializeField] private float _waveAmplitude = 0.1f;
        [SerializeField] private float _waveFrequency = 2f;

        private MeshRenderer _meshRenderer;
        private Vector2 _uvOffset = Vector2.zero;

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshRenderer == null)
            {
                var mf = gameObject.AddComponent<MeshFilter>();
                mf.mesh = CreateRiverMesh();
                _meshRenderer = gameObject.AddComponent<MeshRenderer>();
                _meshRenderer.material = _riverMaterial ?? CreateDefaultMaterial();
            }
        }

        private void Update()
        {
            // Animate UV for flowing water
            _uvOffset.x += _flowSpeed * Time.deltaTime;
            if (_meshRenderer != null && _meshRenderer.material != null)
            {
                _meshRenderer.material.mainTextureOffset = _uvOffset;
            }
        }

        private Mesh CreateRiverMesh()
        {
            // River spans arena width (18 tiles) and height (4 tiles)
            float width = 18f;
            float height = 4f;
            float yCenter = 16f; // Between tiles 14-18

            var mesh = new Mesh();
            var vertices = new Vector3[4];
            vertices[0] = new Vector3(0, yCenter - height/2, 0);
            vertices[1] = new Vector3(width, yCenter - height/2, 0);
            vertices[2] = new Vector3(0, yCenter + height/2, 0);
            vertices[3] = new Vector3(width, yCenter + height/2, 0);

            var uvs = new Vector2[4];
            uvs[0] = new Vector2(0, 0);
            uvs[1] = new Vector2(1, 0);
            uvs[2] = new Vector2(0, 1);
            uvs[3] = new Vector2(1, 1);

            var triangles = new int[6] { 0, 2, 1, 1, 2, 3 };

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            return mesh;
        }

        private Material CreateDefaultMaterial()
        {
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(0.2f, 0.4f, 0.8f, 0.8f);
            return mat;
        }
    }
}