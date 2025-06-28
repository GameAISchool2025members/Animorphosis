
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

using System.Diagnostics;


[BurstCompile]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(EdgeCollider2D))]
[RequireComponent(typeof(WaterTriggerHandlerJOBS))]
public class InteractableWaterJOBS : MonoBehaviour
{
    [Header("Springs")]
    [SerializeField] private float _spriteConstant = 1.4f;
    [SerializeField] private float _damping = 1.1f;
    [SerializeField] private float _spread = 6.5f;
    [SerializeField, Range(1, 10)] private int _wavePropogationIterations = 8;
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

    private NativeArray<float> _velocities;
    private NativeArray<float> _positions;
    private NativeArray<float> _targetHeights;


    private Stopwatch _stopWatch;

    private void Start()
    {
        _coll = GetComponent<EdgeCollider2D>();

        GenerateMesh();
        CreateNativeArrays();

     
        _stopWatch = new Stopwatch();
    }

    private void Reset()
    {
        _coll = GetComponent<EdgeCollider2D>();
        _coll.isTrigger = true;
    }

    private void OnDestroy()
    {
        if (_positions.IsCreated) _positions.Dispose();
        if (_velocities.IsCreated) _velocities.Dispose();
        if (_targetHeights.IsCreated) _targetHeights.Dispose();
    }

    [BurstCompile]
    private void FixedUpdate()
    {
        _stopWatch.Start();

        var springJob = new SpringJob
        {
            velocities = _velocities,
            positions = _positions,
            targetHeights = _targetHeights,
            springConstant = _spriteConstant,
            damping = _damping,
            deltaTime = Time.fixedDeltaTime,
            speedMultiplier = _speedMult,
        };

        JobHandle springHandle = springJob.Schedule(_positions.Length, 8);

        var wavePropagationJob = new WavePropagationJob
        {
            wavePropogationIterations = _wavePropogationIterations,
            velocities = _velocities,
            positions = _positions,
            spread = _spread,
            deltaTime = Time.fixedDeltaTime,
            speedMultiplier = _speedMult,
        };

        JobHandle waveHandle = wavePropagationJob.Schedule(springHandle);
        waveHandle.Complete();

        // Update mesh vertices based on new _positions
        for (int i = 0; i < _positions.Length; i++)
        {
            _vertices[_topVerticesIndex[i]].y = _positions[i];
        }

        _mesh.vertices = _vertices;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        _stopWatch.Stop();
        UnityEngine.Debug.Log("JOBS completion time: " + _stopWatch.ElapsedTicks + " ticks");
        _stopWatch.Reset();
    }

    public void Splash(Collider2D collision, float force)
    {
        float radius = collision.bounds.extents.x * _playerCollisionRadiusMult;
        Vector2 center = collision.transform.position;

        for (int i = 0; i < _positions.Length; i++)
        {
            Vector2 vertexWorldPos = transform.TransformPoint(new Vector2(_vertices[_topVerticesIndex[i]].x, _positions[i]));

            if (IsPointInsideCircle(vertexWorldPos, center, radius))
            {
                _velocities[i] = force * ForceMultiplier;
            }
        }
    }

    private bool IsPointInsideCircle(Vector2 point, Vector2 center, float radius)
    {
        float distanceSquared = (point - center).sqrMagnitude;
        return distanceSquared <= radius * radius;
    }

    public void ResetEdgeCollider()
    {
        _coll = GetComponent<EdgeCollider2D>();

        Vector2[] newPoints = new Vector2[2];
        newPoints[0] = new Vector2(_vertices[_topVerticesIndex[0]].x, _vertices[_topVerticesIndex[0]].y);
        newPoints[1] = new Vector2(_vertices[_topVerticesIndex[_topVerticesIndex.Length - 1]].x, _vertices[_topVerticesIndex[_topVerticesIndex.Length - 1]].y);

        _coll.offset = Vector2.zero;
        _coll.points = newPoints;
    }

    public void GenerateMesh()
    {
        _mesh = new Mesh();

        // Build vertices
        _vertices = new Vector3[NumOfXVertices * NUM_OF_Y_VERTICES];
        _topVerticesIndex = new int[NumOfXVertices];
        for (int y = 0; y < NUM_OF_Y_VERTICES; y++)
        {
            for (int x = 0; x < NumOfXVertices; x++)
            {
                float xPos = (x / (float)(NumOfXVertices - 1)) * Width - Width / 2f;
                float yPos = (y / (float)(NUM_OF_Y_VERTICES - 1)) * Height - Height / 2f;
                _vertices[y * NumOfXVertices + x] = new Vector3(xPos, yPos, 0f);

                if (y == NUM_OF_Y_VERTICES - 1)
                    _topVerticesIndex[x] = y * NumOfXVertices + x;
            }
        }

        // Build triangles
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

                // First triangle
                triangles[index++] = bottomLeft;
                triangles[index++] = topLeft;
                triangles[index++] = bottomRight;

                // Second triangle
                triangles[index++] = bottomRight;
                triangles[index++] = topLeft;
                triangles[index++] = topRight;
            }
        }

        // UVs
        Vector2[] uvs = new Vector2[_vertices.Length];
        for (int i = 0; i < _vertices.Length; i++)
        {
            uvs[i] = new Vector2((_vertices[i].x + Width / 2f) / Width,
                                 (_vertices[i].y + Height / 2f) / Height);
        }

        _meshRenderer = GetComponent<MeshRenderer>();
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer.material = WaterMaterial;

        _mesh.vertices = _vertices;
        _mesh.triangles = triangles;
        _mesh.uv = uvs;

        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        _meshFilter.mesh = _mesh;
    }

    private void CreateNativeArrays()
    {
        int count = _topVerticesIndex.Length;
        _positions = new NativeArray<float>(count, Allocator.Persistent);
        _velocities = new NativeArray<float>(count, Allocator.Persistent);
        _targetHeights = new NativeArray<float>(count, Allocator.Persistent);

        for (int i = 0; i < count; i++)
        {
            _positions[i] = _vertices[_topVerticesIndex[i]].y;
            _targetHeights[i] = _vertices[_topVerticesIndex[i]].y;
            _velocities[i] = 0f;
        }
    }
}

[BurstCompile]
public struct SpringJob : IJobParallelFor
{
    public NativeArray<float> velocities;
    public NativeArray<float> positions;
    public NativeArray<float> targetHeights;
    public float springConstant;
    public float damping;
    public float deltaTime;
    public float speedMultiplier;

    public void Execute(int index)
    {
        // Do not process the endpoints
        if (index == 0 || index == positions.Length - 1)
            return;

        float x = positions[index] - targetHeights[index];
        float acceleration = -springConstant * x - damping * velocities[index];
        positions[index] += velocities[index] * speedMultiplier * deltaTime;
        velocities[index] += acceleration * speedMultiplier * deltaTime;
    }
}

[BurstCompile]
public struct WavePropagationJob : IJob
{
    public int wavePropogationIterations;
    public NativeArray<float> velocities;
    public NativeArray<float> positions;
    public float spread;
    public float deltaTime;
    public float speedMultiplier;

    public void Execute()
    {
        int count = positions.Length;
        for (int j = 0; j < wavePropogationIterations; j++)
        {
            for (int i = 1; i < count - 1; i++)
            {
                float leftDelta = spread * (positions[i] - positions[i - 1]) * speedMultiplier * deltaTime;
                velocities[i - 1] += leftDelta;

                float rightDelta = spread * (positions[i] - positions[i + 1]) * speedMultiplier * deltaTime;
                velocities[i + 1] += rightDelta;
            }
        }
    }
}
