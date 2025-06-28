using UnityEngine;

public enum RequiredForm { Big, Small, Swim }

public class Obstacle : MonoBehaviour
{
    public RequiredForm requiredForm;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player != null)
        {
           
        }
    }
}
