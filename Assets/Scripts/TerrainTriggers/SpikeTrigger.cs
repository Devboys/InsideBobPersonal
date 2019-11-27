using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpikeTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();

            player.Die();
        }
    }
}
