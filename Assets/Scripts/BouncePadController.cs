using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class BouncePadController : MonoBehaviour
{
    public bool fixedDirection = true;
    
    //Audio Variables
    [Header("-- FMOD Events")] 
    [Space(20)] 
    [EventRef]
    public string bounceSound;

    public Vector2 direction;
    
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if (fixedDirection)
            {
                Vector2 dir = direction.normalized;
                dir.x = Mathf.Sign(transform.up.x) * dir.x;
                player.StartBounce(dir);
            }
            else
            {
                player.StartBounce(Vector2.Reflect(player.velocity, transform.up).normalized);
            }
            
            //Play bounce sound.
            RuntimeManager.PlayOneShot(bounceSound, transform.position);
        }
    }

}
