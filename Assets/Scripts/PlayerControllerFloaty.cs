using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControllerFloaty : MonoBehaviour
{
    public enum MovementState
    {
        Grounded, InAir
    }

    public float speed = 4;
    public float jumpForce = 2;

    public MovementState movementState;

    public float jumpCooldown;
    private Timer jumpTimer;

    private Rigidbody2D rb;
    public Vector2 playerVelocity;

    [Range (50,100)]
    public float dampening = 90;

    public LayerMask platformLayer;

    private Vector2 oldVelocity;

    private bool onPlatform = false;
    private Rigidbody2D platform;

    private CapsuleCollider2D col;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerVelocity = new Vector2(0, 0);
        jumpTimer = new Timer();
        col = GetComponent<CapsuleCollider2D>();
    }

    // Update is called once per frame
    void Update()
    {
        TickTimers(Time.deltaTime * 1000);

        UpdateMovementState();

        UpdatePlayerVelocity();

        HandleMovement();

        HandleJump();

        oldVelocity = rb.velocity;

        Debug.Log(rb.velocity);
    }

    private void FixedUpdate()
    {
        ApplyPlatformMovement();
    }

    private void UpdateMovementState()
    {
        RaycastHit2D hitInfo = Physics2D.Raycast(new Vector2(transform.position.x, transform.position.y - col.size.y / 2 * transform.localScale.y), -transform.up, 0.05f, platformLayer);
        movementState = hitInfo ? MovementState.Grounded : MovementState.InAir;
    }


    private void TickTimers(float deltaTime)
    {
        jumpTimer.TickTimer(deltaTime);
    }

    private void HandleMovement()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        Vector2 rightMovement = transform.right * speed * horizontalInput;
        if (movementState == MovementState.InAir && horizontalInput != 0)
        {
            var test = playerVelocity.x * dampening / 100 + (speed * 2 * horizontalInput) * (100 - dampening) / 100;

            rb.velocity = new Vector2(rb.velocity.x - playerVelocity.x + test, rb.velocity.y);

            playerVelocity.x = test;
        }
        else if (CheckIfValidXMovement(rightMovement))
        {
            var test = playerVelocity.x * dampening / 100 + (rightMovement.x) * (100 - dampening) / 100;

            rb.velocity = new Vector2(rb.velocity.x - playerVelocity.x + test, rb.velocity.y);

            playerVelocity.x = test;
        }
        else if (movementState == MovementState.Grounded) {
            var test = rb.velocity.x * dampening / 100;

            rb.velocity = new Vector2(test, rb.velocity.y);

            playerVelocity.x = test;
        }
    }

    private Vector2 GetExternalVelocity() {
        RaycastHit2D hitInfo = Physics2D.Raycast(new Vector2(transform.position.x, transform.position.y - col.size.y / 2 * transform.localScale.y), -transform.up, 0.05f, platformLayer);
        if (hitInfo)
        {
            return hitInfo.collider.GetComponent<Rigidbody2D>().velocity;
        }
        return new Vector2();
    }

    private void ApplyPlatformMovement() {
        RaycastHit2D hitInfo = Physics2D.Raycast(new Vector2(transform.position.x, transform.position.y - col.size.y / 2 * transform.localScale.y), -transform.up, 0.05f, platformLayer);
        if (hitInfo && !onPlatform) {
            onPlatform = true;
            platform = hitInfo.collider.GetComponent<Rigidbody2D>();
        }
        if (onPlatform)
        {
            rb.position = (rb.position + platform.velocity * Time.fixedDeltaTime);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (platform && platform != collision.rigidbody) {
            onPlatform = false;
            platform = null;
        }
    }

    private void UpdatePlayerVelocity()
    {
        var diffVel = rb.velocity - oldVelocity;
        var right = playerVelocity.x > 0;
        if (right)
        {
            if (diffVel.x < 0) playerVelocity.x += diffVel.x;
            if (diffVel.x > 0) playerVelocity.x -= diffVel.x;
        }
        else
        {
            if (diffVel.x > 0) playerVelocity.x += diffVel.x;
            if (diffVel.x < 0) playerVelocity.x -= diffVel.x;
        }
    }

    private void HandleJump()
    {
        float jumpInput = Input.GetAxisRaw("Jump");
        if (jumpInput != 0 && movementState == MovementState.Grounded && jumpTimer.isFinished)
        {
            Vector2 jumpMovement = transform.up * jumpForce * jumpInput;
            rb.velocity = new Vector2(rb.velocity.x, jumpMovement.y);

            jumpTimer.StartTimer(jumpCooldown);
        }
    }

    private bool CheckIfValidXMovement(Vector2 movement)
    {
        if (movement.x == 0) return false;
        if (movement.x > 0)
        {
            if (playerVelocity.x <= speed) return true;
        }
        else
        {
            if (playerVelocity.x >= -speed) return true;
        }
        return false;
    }

    private class Timer
    {
        float timer;

        public bool isFinished
        {
            get { return timer <= 0; }
        }

        public void StartTimer(float totalTime) { timer = totalTime; }

        public void TickTimer(float amount) { timer -= amount; }
    }
}
