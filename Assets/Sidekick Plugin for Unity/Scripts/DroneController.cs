//------------------------------------------------------------------------------
// Written by Animation Prep Studio
// www.mocapfusion.com
//------------------------------------------------------------------------------
using UnityEngine;

public class DroneController : MonoBehaviour
{
    public GameObject deathParticles;
    public AudioClip deathClip;
    public AudioClip attackClip;
    public AudioClip[] impactClips;

    public AudioSource droneAudio;
    
    void Awake()
    {
        GetComponent<AudioSource>().pitch = Random.Range(0.9f, 1.1f);
        speed = Random.Range(5.0f, 20.0f);
    }
    
    
    public float speed = 15.0f;
    public float attackDistance = 0.25f;
    
    public float health = 100f; 
    public float damage = 10f;
    
    private void OnParticleCollision(GameObject collision)
    {
        health -= damage;

        if (health <= 0)
        {
            Death(deathClip);
            DragonController.Instance.AddScore();
            return;
        }
        
        droneAudio.PlayOneShot(impactClips[Random.Range(0, impactClips.Length)]);
    }
    
    void Update()
    {
        var dir = (DragonController.Instance.attackTarget.position - transform.position).normalized;
        transform.position += dir * Time.deltaTime * speed;

        if (Vector3.Distance(DragonController.Instance.attackTarget.position, transform.position) <= attackDistance)
        {
            //END GAME
            DragonController.Instance.ReceiveDamage((int)health / 2);
            
            Death(attackClip);
        }
    }

    public void Death(AudioClip clip)
    {
        var particles = Instantiate(deathParticles);
        particles.transform.position = transform.position;
        Destroy(particles.gameObject, 10);
        
        DragonController.Instance.deathAudio.PlayOneShot(clip);
        
        Destroy(this.gameObject);
    }
}
