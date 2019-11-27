using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBounceGravity : MonoBehaviour
{

    public enum Direction {
        Right, Left
    }

    public Vector2 movement;
    public LayerMask platformLayer;

    public Direction direction;
    private Rigidbody2D rb;
    private CircleCollider2D col;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        direction = movement.x < 0 ? Direction.Left : Direction.Right;
        col = GetComponent<CircleCollider2D>();

    }

    void LateUpdate()
    {
        RaycastHit2D downHit = Physics2D.Raycast(new Vector2(transform.position.x, transform.position.y - col.radius * transform.localScale.y), -transform.up, 0.05f, platformLayer);
        if (downHit) UpdateMovement();
    }

    private void UpdateMovement()
    {

        var xVel = direction == Direction.Left ? -Mathf.Abs(movement.x) : Mathf.Abs(movement.x);
        var yVel = movement.y;
        rb.velocity = new Vector2(xVel, yVel);
    }

    private void SwitchDirection()
    {
        if (direction == Direction.Right)
        {
            direction = Direction.Left;
        }
        else
        {
            direction = Direction.Right;
        }
        var xVel = direction == Direction.Left ? -Mathf.Abs(movement.x) : Mathf.Abs(movement.x);
        rb.velocity = new Vector2(xVel, rb.velocity.y);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        var pc = collision.gameObject.GetComponent<PlayerController>();
        if (pc)
        {
            if (pc.IsCannonBall()) Die();
        }
        if (collision.GetContact(0).point.x < transform.position.x - 0.1f && direction == Direction.Left) SwitchDirection();
        else if (collision.GetContact(0).point.x > transform.position.x + 0.01f && direction == Direction.Right) SwitchDirection();
    }

    public void Die()
    {
        Destroy(gameObject);
    }
}
