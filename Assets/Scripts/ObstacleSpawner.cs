using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    
    public GameObject[] obstaclePrefabs;
    public float spawnInterval = 5f;
    public float minHorizontalSpacing = 3f;
    public Transform player;

    private float timer;
    private float lastSpawnX;

    void Start()
    {
        if (player != null)
            lastSpawnX = transform.position.x;
    }

    void Update()
    {
        if (player == null) return;

        transform.position = new Vector3(player.position.x + 7f, transform.position.y, 0f);
        timer += Time.deltaTime;

        if (timer >= spawnInterval && transform.position.x - lastSpawnX >= minHorizontalSpacing)
        {
            SpawnObstacle();
            timer = 0f;
            lastSpawnX = transform.position.x;
        }
    }

    void SpawnObstacle()
    {
        if (obstaclePrefabs.Length == 0) return;

        GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
        Vector3 spawnPos = new Vector3(transform.position.x, transform.position.y, 0f);
        Instantiate(prefab, spawnPos, Quaternion.identity);
    }
}
