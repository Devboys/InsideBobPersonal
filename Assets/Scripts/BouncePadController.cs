using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BouncePadController : MonoBehaviour
{
    public bool fixedDirection = true;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if (fixedDirection)
                player.StartBounce(transform.up.normalized);
            else 
            {
                player.StartBounce(Vector2.Reflect(player.velocity, transform.up).normalized);
            }
        }
    }

}
