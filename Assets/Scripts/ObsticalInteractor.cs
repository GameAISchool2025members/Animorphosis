using UnityEngine;
using UnityEngine.UI;

public class ObsticalInteractor : MonoBehaviour
{
    public bool isInteracting;

    [SerializeField] private LayerMask leaveLayer;
    [SerializeField] private LayerMask waterLayer;
    [SerializeField] private LayerMask mouseLayer;
    [SerializeField] private LayerMask jumpLayer;

    private PlayerController playerController;

    private bool insideWaterLayer;
    private bool insideMouseLayer;
    private bool insideJumpLayer;
    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        int mask = 1 << collision.gameObject.layer;

        if ((leaveLayer.value & mask) != 0 && playerController.currentAnimal.form == AnimalForm.Cow)
        {
            var breakable = collision.gameObject.GetComponent<BreakableObject>();
            if (breakable != null)
            {
                breakable.Break();
                return;
            }
        }
        else if ((waterLayer.value & mask) != 0 && playerController.currentAnimal.form == AnimalForm.Frog || playerController.currentAnimal.form == AnimalForm.Seagull)
        {
            Debug.Log("yes");
            insideWaterLayer = true;
            DisableColliders(collision);

            Collider2D[] cols = collision.gameObject.GetComponents<Collider2D>();
            foreach (var col in cols)
                col.enabled = false;
        }
        else if ((mouseLayer.value & mask) != 0 && playerController.currentAnimal.form == AnimalForm.Mouse)
        {
            Debug.Log("yes");
            insideMouseLayer = true;
            DisableColliders(collision);

            Collider2D[] cols = collision.gameObject.GetComponents<Collider2D>();
            foreach (var col in cols)
                col.enabled = false;
        }
        else if ((jumpLayer.value & mask) != 0 && playerController.currentAnimal.form == AnimalForm.Cat || playerController.currentAnimal.form == AnimalForm.Seagull)
        {
            Debug.Log("yes");
            insideJumpLayer = true;
            DisableColliders(collision);
            Collider2D[] cols = collision.gameObject.GetComponents<Collider2D>();
            foreach (var col in cols)
                col.enabled = false;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        int mask = 1 << collision.gameObject.layer;
        if ((waterLayer.value & mask) != 0)
            insideWaterLayer = false;
        if ((mouseLayer.value & mask) != 0)
            insideMouseLayer = false;
        if ((jumpLayer.value & mask) != 0)
            insideJumpLayer = false;
    }


    //private void OnTriggerExit2D(Collider2D collision)
    //{
    //    int layerMaskValue = 1 << collision.gameObject.layer;

    //    if ((leaveLayer.value & layerMaskValue) != 0 ||
    //        (waterLayer.value & layerMaskValue) != 0 ||
    //        (mouseLayer.value & layerMaskValue) != 0)
    //    {
    //        isInteracting = false;
    //        Debug.Log("Exited interaction layer");
    //    }
    //}
    private void DisableColliders(Collision2D collision)
    {
        var cols = collision.gameObject.GetComponents<Collider2D>();
        foreach (var col in cols)
            col.enabled = false;
    }

    private void Update()
    {
        if (isInteracting)
        {
           
        }
    }

    void WaterInteract()
    {

    }
    void BreakWallInteract()
    {

    }
    void GoThrowInteract()
    {

    }
}
