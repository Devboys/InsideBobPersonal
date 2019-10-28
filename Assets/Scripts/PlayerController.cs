using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    public float speed;
    public float jumpForce = 2;

    public enum MovementState {
        Grounded, InAir
    }

    public MovementState movementState;

    private Rigidbody2D rb;
    private RaycastHit2D[] hitBuffer;
    private ContactFilter2D filter;

    public float jumpCooldown = 10;
    public float controllableCooldown = 10;
    public float airDamping = 0.2f;
    private Timer jumpTimer = new Timer();
    private Timer controllableTimer = new Timer();

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        hitBuffer = new RaycastHit2D[16];
        filter = new ContactFilter2D();
    }

    // Update is called once per frame
    void Update()
    {
        TickTimers(Time.deltaTime * 1000);

        UpdateMovementState();

        HandleMovement();

        HandleJump();
    }

    private void HandleMovement()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");

        Vector2 rightMovement = transform.right * speed * horizontalInput;

        if (controllableTimer.isFinished)
        {
            if (movementState == MovementState.Grounded)
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
                rb.AddForce(rightMovement, ForceMode2D.Impulse);
            }
            else
            {
                if(CheckIfValidXMovement(rightMovement)) rb.AddForce(rightMovement, ForceMode2D.Force);
            }
        }
        else
        {
            rb.AddForce(rightMovement * airDamping);
        }
    }

    private bool CheckIfValidXMovement(Vector2 movement)
    {
        if (movement.x > 0)
        {
            if (rb.velocity.x < speed) return true;
        }
        else
        {
            if (rb.velocity.x > -speed) return true;
        }
        return false;
    }

    private void HandleJump()
    {
        if (movementState == MovementState.Grounded && jumpTimer.isFinished)
        {
            float jumpInput = Input.GetAxisRaw("Jump");
            Vector2 jumpMovement = transform.up * jumpForce * jumpInput;
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y + jumpMovement.y);

            jumpTimer.StartTimer(jumpCooldown);
        }
    }

    private void UpdateMovementState()
    {
        int grounded = rb.Cast(-Vector3.up, filter, hitBuffer, 0.05f);

        if (grounded > 0)
        {
            movementState = MovementState.Grounded;
        }
        else
        {
            movementState = MovementState.InAir;
        }
    }

    private void TickTimers(float deltaTime)
    {
        controllableTimer.TickTimer(deltaTime);

        jumpTimer.TickTimer(deltaTime);
    }

    public void SetUncontrollable()
    {
        controllableTimer.StartTimer(controllableCooldown);
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
