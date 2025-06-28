using UnityEngine;

public class PlayerInWater : MonoBehaviour
{

    bool isInWater = false;
    Rigidbody2D rb;
    public float normalDrag = 0f;
    public float waterDrag = 2f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        normalDrag = rb.linearDamping;
    }


    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("TriggerEnter " + other.name);
        if (other.CompareTag("Water"))
            isInWater = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log("TriggerExit " + other.name);
        if (other.CompareTag("Water"))
            isInWater = false;
    }


    void Update()
    {
        rb.gravityScale = isInWater ? 0.5f : 2f;
        rb.linearDamping = isInWater ? waterDrag : normalDrag;
    }

}
