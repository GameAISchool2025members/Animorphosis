using UnityEngine;

public class ZoneChecker : MonoBehaviour
{
    [SerializeField] private LayerMask waterLayer;
    [SerializeField] private LayerMask mouseLayer;
    [SerializeField] private LayerMask jumpLayer;
    [SerializeField] private LayerMask breakLayer;
    [SerializeField] private PlayerController playerController;

    private Collider2D myCollider;
    private ContactFilter2D filter;
    private Collider2D[] hits = new Collider2D[5];

    void Awake()
    {
        myCollider = GetComponent<Collider2D>();
        filter = new ContactFilter2D();
        filter.useLayerMask = true;
    }

    void Update()
    {
        var form = playerController.currentAnimal.form;
        int count;

        filter.layerMask = waterLayer;
        count = myCollider.Overlap(filter, hits);
        if (form == AnimalForm.Frog && count > 0)
        {
            Debug.Log("I water zone as Frog");
            return;
        }

        filter.layerMask = mouseLayer;
        count = myCollider.Overlap(filter, hits);
        if (form == AnimalForm.Mouse && count > 0)
        {
            Debug.Log("I mouse zone as Mouse");
            return;
        }

        filter.layerMask = jumpLayer;
        count = myCollider.Overlap(filter, hits);
        if (form == AnimalForm.Cat && count > 0)
        {
            Debug.Log("I jump zone as Cat");
        }

        filter.layerMask = breakLayer;
        count = myCollider.Overlap(filter, hits);
        if (form == AnimalForm.Cow && count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                var br = hits[i].GetComponent<BreakableObject>();
                if (br != null)
                {
                    br.Break();
                    
                }
            }
        }
    }
}
