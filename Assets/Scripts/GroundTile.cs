using UnityEngine;

public class GroundTile : MonoBehaviour
{
    public float tileLength = 20f;
    public Transform player;
    public GameObject obstaclePrefab;

    public Transform[] obstacleSpawnPoints; // Positions on the tile to potentially spawn obstacles

    void Update()
    {
        if (player.position.x - transform.position.x > tileLength)
        {
            RepositionTile();
        }
    }

    void RepositionTile()
    {
        transform.position += Vector3.right * tileLength * 3;
        SpawnObstacle();
    }

    void SpawnObstacle()
    {
        foreach (Transform spawnPoint in obstacleSpawnPoints)
        {
            if (Random.value < 0.5f) // 50% chance to spawn at each point
            {
                Instantiate(obstaclePrefab, spawnPoint.position, Quaternion.identity);
            }
        }
    }
}
