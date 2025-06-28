using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(EdgeCollider2D))]
[RequireComponent(typeof(WaterTriggerHandler))]
public class InteractableWater : MonoBehaviour
{
    [Header("Springs")]
    [SerializeField] private float _springConstant = 1.4f;
    [SerializeField] private float _damping = 1.1f;
    [SerializeField] private float _spread = 6.5f;
    [SerializeField, Range(1, 10)] private int _wavePropagationIterations = 8;
    [SerializeField, Range(0f, 20f)] private float _speedMult = 5.5f;

    [Header("Force")]
    public float ForceMultiplier = 0.2f;
    [Range(1f, 50f)] public float MaxForce = 5f;

    [Header("Collision")]
    [SerializeField, Range(1f, 10f)] private float _playerCollisionRadiusMult = 4.15f;

    [Header("Mesh Generation")]
    [Range(2, 500)] public int NumOfXVertices = 70;
    public float Width = 10f;
    public float Height = 4f;
    public Material WaterMaterial;
    private const int NUM_OF_Y_VERTICES = 2;

    [Header("Gizmo")]
    public Color GizmoColor = Color.white;

    private Mesh _mesh;
    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    private Vector3[] _vertices;
    private int[] _topVerticesIndex;

    private EdgeCollider2D _coll;
    private Stopwatch _stopWatch;

    private class WaterPoint
    {
        public float velocity, pos, targetHeight;
    }
    private List<WaterPoint> _waterPoints = new List<WaterPoint>();

    private void Start()
    {
        _coll = GetComponent<EdgeCollider2D>();
        GenerateMesh();
        CreateWaterPoints();
    }

    private void Reset()
    {
        _coll = GetComponent<EdgeCollider2D>();
        _coll.isTrigger = true;
    }

    public void ResetEdgeCollider()
    {
        _coll = GetComponent<EdgeCollider2D>();
        Vector2[] newPoints = new Vector2[2];
        Vector2 firstPoint = new Vector2(_vertices[_topVerticesIndex[0]].x, _vertices[_topVerticesIndex[0]].y);
        newPoints[0] = firstPoint;
        Vector2 secondPoint = new Vector2(_vertices[_topVerticesIndex[_topVerticesIndex.Length - 1]].x, _vertices[_topVerticesIndex[_topVerticesIndex.Length - 1]].y);
        newPoints[1] = secondPoint;
        _coll.offset = Vector2.zero;
        _coll.points = newPoints;
    }

    private void CreateWaterPoints()
    {
        _waterPoints.Clear();
        for (int i = 0; i < _topVerticesIndex.Length; i++)
        {
            _waterPoints.Add(new WaterPoint
            {
                pos = _vertices[_topVerticesIndex[i]].y,
                targetHeight = _vertices[_topVerticesIndex[i]].y,
            });
        }
    }

    private void FixedUpdate()
    {
        for (int i = 1; i < _waterPoints.Count - 1; i++)
        {
            WaterPoint point = _waterPoints[i];
            float x = point.pos - point.targetHeight;
            float acceleration = -_springConstant * x - _damping * point.velocity;
            point.pos += point.velocity * _speedMult * Time.fixedDeltaTime;
            _vertices[_topVerticesIndex[i]].y = point.pos;
            point.velocity += acceleration * _speedMult * Time.fixedDeltaTime;
        }

        for (int j = 0; j < _wavePropagationIterations; j++)
        {
            for (int i = 1; i < _waterPoints.Count - 1; i++)
            {
                float leftDelta = _spread * (_waterPoints[i].pos - _waterPoints[i - 1].pos) * _speedMult * Time.fixedDeltaTime;
                _waterPoints[i - 1].velocity += leftDelta;
                float rightDelta = _spread * (_waterPoints[i].pos - _waterPoints[i + 1].pos) * _speedMult * Time.fixedDeltaTime;
                _waterPoints[i + 1].velocity += rightDelta;
            }
        }

        _mesh.vertices = _vertices;
    }

    public void Splash(Collider2D collision, float force)
    {
        float radius = collision.bounds.extents.x * _playerCollisionRadiusMult;
        Vector2 center = collision.transform.position;
        for (int i = 0; i < _topVerticesIndex.Length; i++)
        {
            Vector2 vertexWorldPos = transform.TransformPoint(_vertices[_topVerticesIndex[i]]);
            if (IsPointInsideCircle(vertexWorldPos, center, radius))
            {
                _waterPoints[i].velocity = force;
            }
        }
    }

    private bool IsPointInsideCircle(Vector2 point, Vector2 center, float radius)
    {
        float distanceSquared = (point - center).sqrMagnitude;
        return distanceSquared <= radius * radius;
    }

    public void GenerateMesh()
    {
        _mesh = new Mesh();
        _vertices = new Vector3[NumOfXVertices * NUM_OF_Y_VERTICES];
        _topVerticesIndex = new int[NumOfXVertices];
        for (int y = 0; y < NUM_OF_Y_VERTICES; y++)
        {
            for (int x = 0; x < NumOfXVertices; x++)
            {
                float xPos = (x / (float)(NumOfXVertices - 1)) * Width - Width / 2;
                float yPos = (y / (float)(NUM_OF_Y_VERTICES - 1)) * Height - Height / 2;
                _vertices[y * NumOfXVertices + x] = new Vector3(xPos, yPos, 0f);
                if (y == NUM_OF_Y_VERTICES - 1)
                {
                    _topVerticesIndex[x] = y * NumOfXVertices + x;
                }
            }
        }

        int[] triangles = new int[(NumOfXVertices - 1) * (NUM_OF_Y_VERTICES - 1) * 6];
        int index = 0;
        for (int y = 0; y < NUM_OF_Y_VERTICES - 1; y++)
        {
            for (int x = 0; x < NumOfXVertices - 1; x++)
            {
                int bottomLeft = y * NumOfXVertices + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + NumOfXVertices;
                int topRight = topLeft + 1;
                triangles[index++] = bottomLeft;
                triangles[index++] = topLeft;
                triangles[index++] = bottomRight;
                triangles[index++] = bottomRight;
                triangles[index++] = topLeft;
                triangles[index++] = topRight;
            }
        }

        Vector2[] uvs = new Vector2[_vertices.Length];
        for (int i = 0; i < _vertices.Length; i++)
        {
            uvs[i] = new Vector2((_vertices[i].x + Width / 2) / Width, (_vertices[i].y + Height / 2) / Height);
        }

        if (_meshRenderer == null)
            _meshRenderer = GetComponent<MeshRenderer>();
        if (_meshFilter == null)
            _meshFilter = GetComponent<MeshFilter>();

        _meshRenderer.material = WaterMaterial;
        _mesh.vertices = _vertices;
        _mesh.triangles = triangles;
        _mesh.uv = uvs;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
        _meshFilter.mesh = _mesh;
    }
}
