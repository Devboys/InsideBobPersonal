using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageTrigger : MonoBehaviour
{
    [SerializeField]
    public LinkedMessage linkedMessage;
    public bool reuseable;

    private bool used;

    private void Awake()
    {
        used = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!reuseable && used) return;
        if (other.gameObject.CompareTag("Player")) {
            linkedMessage.ShowMessage();
            used = true;
        }
    }
}
