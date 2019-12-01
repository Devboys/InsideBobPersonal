using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageTrigger : MonoBehaviour
{
    [SerializeField]
    public LinkedMessage linkedMessage;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player")) {
            linkedMessage.ShowMessage();
        }
    }
}
