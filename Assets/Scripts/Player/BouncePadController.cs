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
    public float dotCutoff = 0.05f;

    private Collider2D _col;
    public ContactFilter2D contacts;

    private void Start()
    {
        dotCutoff = Mathf.Abs(dotCutoff);
        _col = GetComponent<Collider2D>();

        Collider2D[] overlapResults = new Collider2D[1000];
        _col.OverlapCollider(contacts, overlapResults);

        if (overlapResults[0] != null && !fixedDirection)
        {
            PlayerController player = overlapResults[0].GetComponent<PlayerController>();
            Vector2 dir = new Vector2(player.velocity.x, player.maxSpeed).normalized;
            player.StartBounce(dir);

            Debug.Log((player.maxSpeed - player.velocity.x) / player.maxSpeed);

            //Play bounce sound.
            RuntimeManager.PlayOneShot(bounceSound, transform.position);
        }

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            Vector2 reflectedVelocity = Vector2.Reflect(player.velocity, transform.up).normalized;
            float dot = Vector2.Dot(player.velocity.normalized, reflectedVelocity);

            if (fixedDirection)
            {
                Vector2 dir = direction.normalized;
                dir.x = Mathf.Sign(transform.up.x) * dir.x;
                player.StartBounce(dir);

                //Play bounce sound.
                RuntimeManager.PlayOneShot(bounceSound, transform.position);
            }
            else if(dot < 1 - dotCutoff) 
            {
                player.StartBounce(reflectedVelocity);

                //Play bounce sound.
                RuntimeManager.PlayOneShot(bounceSound, transform.position);
            }
            
        }
    }

}
