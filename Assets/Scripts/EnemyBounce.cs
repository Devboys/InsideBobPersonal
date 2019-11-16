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
        var pc = collision.gameObject.GetComponent<OldPlayerController>();
        if (pc)
        {
            if (pc.IsCannonBall()) Die();
        }

        RaycastHit2D[] hitInfos = new RaycastHit2D[1];
        collision.Raycast(rb.velocity, hitInfos, Mathf.Infinity, platformLayer);

        if (hitInfos.Length > 0)
        {
            rb.velocity = Vector2.Reflect(rb.velocity, hitInfos[0].normal);
        }
    }

    public void Die()
    {
        Destroy(gameObject);
    }
}
