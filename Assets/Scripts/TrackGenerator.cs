using UnityEngine;
using System.Collections.Generic;

public class TrackGenerator : MonoBehaviour
{
    public Transform player1;
    public Transform player2;

    public GameObject[] trackPrefabs;
    public float spawnBuffer = 20f;
    public float despawnBuffer = 30f;

    private Dictionary<GameObject, Queue<GameObject>> pool;
    private List<GameObject> activeSegments = new List<GameObject>();

    private float lastEndX = 0f;

    void Start()
    {
        pool = new Dictionary<GameObject, Queue<GameObject>>();
        foreach (var prefab in trackPrefabs)
            pool[prefab] = new Queue<GameObject>();

        for (int i = 0; i < 5; i++)
            SpawnNextSegment();
    }

    void Update()
    {
        float furthestX = Mathf.Max(player1.position.x, player2.position.x);

        if (furthestX + spawnBuffer > lastEndX)
            SpawnNextSegment();

        for (int i = activeSegments.Count - 1; i >= 0; i--)
        {
            var seg = activeSegments[i];
            float segEndX = seg.transform.position.x + GetSegmentLength(seg);
            if (segEndX < furthestX - despawnBuffer)
            {
                RecycleSegment(seg);
                activeSegments.RemoveAt(i);
            }
        }
    }

    void SpawnNextSegment()
    {
        var prefab = trackPrefabs[Random.Range(0, trackPrefabs.Length)];
        GameObject seg;

        if (pool[prefab].Count > 0)
        {
            seg = pool[prefab].Dequeue();
            seg.SetActive(true);
        }
        else
        {
            seg = Instantiate(prefab);
        }

        seg.transform.position = new Vector3(lastEndX, 0f, 0f);
        activeSegments.Add(seg);

        lastEndX += GetSegmentLength(seg);
    }

    void RecycleSegment(GameObject seg)
    {
        seg.SetActive(false);
        var prefab = trackPrefabs[FindPrefabIndex(seg)];
        pool[prefab].Enqueue(seg);
    }

    float GetSegmentLength(GameObject seg)
    {
        var collider = seg.GetComponent<BoxCollider2D>();
        if (collider != null)
            return collider.size.x * seg.transform.localScale.x;
        return 10f;
    }

    int FindPrefabIndex(GameObject instance)
    {
        for (int i = 0; i < trackPrefabs.Length; i++)
        {
            if (trackPrefabs[i].name == instance.name.Replace("(Clone)", ""))
                return i;
        }
        return 0;
    }
}
