using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformController : MonoBehaviour
{

    public Vector2 direction = new Vector2(0, 1);
    public float force = 1;
    private List<Rigidbody2D> objects = new List<Rigidbody2D>();
    private float lastJumpInput = 0;

    // Start is called before the first frame update
    void Start()
    {
        direction = direction.normalized;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, direction.normalized);
        Gizmos.DrawCube(transform.position + (Vector3)direction.normalized, new Vector3(0.2f, 0.2f, 0.2f));
    }

    public void Update()
    {
        float curJumpInput = Input.GetAxisRaw("BobJump");

        if (curJumpInput != 0 && lastJumpInput == 0)
        {
            foreach (Rigidbody2D rigidbody in objects)
            {
                rigidbody.velocity = direction * force;

                PlayerController pc = rigidbody.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.SetUncontrollable();
                }
            }
            //Debug.Log(objects.Count);
            objects.Clear();
        }

        lastJumpInput = curJumpInput;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        objects.Add(collision.rigidbody);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        objects.Remove(collision.rigidbody);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        objects.Add(collision.attachedRigidbody);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        objects.Remove(collision.attachedRigidbody);
    }
}
