using UnityEngine;

public class BreakableObject : MonoBehaviour
{

    public GameObject brokenEffectPrefab;


    public GameObject brokenVersionPrefab;

    public bool spawnBrokenModel = false;

    public void Break()
    {
  
        if (brokenEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                brokenEffectPrefab,
                transform.position,
                Quaternion.identity
            );
       
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
                Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
            else
                Destroy(effect, 2f);
        }

        if (spawnBrokenModel && brokenVersionPrefab != null)
        {
            Instantiate(brokenVersionPrefab, transform.position, transform.rotation);
        }

        
        Destroy(gameObject);
    }


    //private void OnTriggerEnter2D(Collider2D collision)
    //{
    //    Break();
    //}
}
