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

    [Tooltip("Minimum angle that the bounce pad will send you in (relative to straight along the ground)")]
    [Range(0, 90)]
    public float minimumBounceAngle = 45;

    public GameObject placeParticlePrefab;


    private void Awake()
    {
        Instantiate(placeParticlePrefab, this.transform);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            Vector2 dir;

            if (fixedDirection)
            {
                dir = direction;
                dir.x = Mathf.Sign(transform.up.x) * dir.x;
            }
            else
            {
                Vector2 reflectedVelocity = Vector2.Reflect(player.velocity, transform.up).normalized;
                dir = reflectedVelocity;

                if(dir.magnitude == 0)
                {
                    dir = Vector2.up;
                }

                if (Vector2.Angle(transform.up, reflectedVelocity) > minimumBounceAngle)
                {
                    //correct velocity to be within bounds
                    dir = Vector2FromAngle(minimumBounceAngle);
                    dir.x *= Mathf.Sign(player.velocity.x);
                    dir.y *= Mathf.Sign(transform.up.y);

                    if (player.horizontalMove == 0)
                    {
                        //bounce is directly upwards if 
                        dir = transform.up;
                    }
                }
            }

            player.StartBounce(dir.normalized);

            //Play bounce sound.
            RuntimeManager.PlayOneShot(bounceSound, transform.position);
        }
    }


    /// <summary>
    /// generate a normalized vector, rotated a given angle
    /// </summary>
    private Vector2 Vector2FromAngle(float angle)
    {
        angle *= Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

}
