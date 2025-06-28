using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Vector3 offset;

    [SerializeField] private float followSpeed = 0.1f;

    void LateUpdate()
    {
        if (player == null) return;
        Vector3 playerPos = transform.position;
        playerPos.x = player.position.x;
        transform.position = Vector3.Lerp(transform.position, playerPos + offset, followSpeed);
    }
}
