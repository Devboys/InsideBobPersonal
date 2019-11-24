using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBounce : MonoBehaviour
{
    public Vector2 movement;
    public LayerMask platformLayer;

    private Rigidbody2D rb;
    private CircleCollider2D col;
    private float magnitude;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();
        rb.velocity = movement;
        magnitude = movement.magnitude;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var pc = collision.gameObject.GetComponent<PlayerController>();
        if (pc)
        {
            if (pc.IsCannonBall()) Die();
        }

        Vector2[] rayDir = new Vector2[] { Vector2.up, Vector2.left, Vector2.down, Vector2.right };
        foreach (Vector2 dir in rayDir)
        {
            RaycastHit2D[] hitInfos = new RaycastHit2D[1];
            col.Raycast(dir, hitInfos, col.radius + 0.1f, platformLayer);
            if (hitInfos.Length > 0 && hitInfos[0].point != Vector2.zero)
            {
                Debug.Log(hitInfos[0].point);
                rb.velocity = Vector2.Reflect(rb.velocity, -dir).normalized * magnitude;
            }
        }
    }

    public void Die()
    {
        Destroy(gameObject);
    }
}
