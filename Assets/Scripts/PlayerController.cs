﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RaycastMover))]
public class PlayerController : MonoBehaviour
{
    //Editor properties.
    [Header("-- Gravity")]
    public float gravity;
    public float maxGravity;

    [Header("-- Ground Movement")]
    public float maxSpeed = 5;
    public float acceleration;
    public float dirChangeAcceleration;
    public float deceleration;

    [Header("-- Air movement")]
    public float airAcceleration;
    public float airDirChangeAcceleration;
    public float airDeceleration;

    [Header("-- Jumping")]
    [Tooltip("Height of apex if player does not release button")]
    public float maxJumpHeight;
    [Tooltip("Jump height if player instantly releases button")]
    public float minJumpHeight;
    [Tooltip("How long to allow for jumping after walking off edges")]
    public float jumpGraceTime = 0.1f;

    [Header("-- Bouncing")]
    public float bounceTime;
    public float bounceMultiplier;
    public float bounceAcceleration;
    public float bounceDirChangeAcceleration;
    public float bounceDeceleration;

    [Header("-- Shooting")]
    public GameObject padPrefab;
    public LayerMask hitLayers;
    public float shotCooldown;
    public float numPadsAllowed;


    //private variables
    [Header("-- State")]
    [ReadOnly] public Vector2 velocity;
    private List<GameObject> padList;

    [SerializeField] [ReadOnly] private bool inBounce;

    #region Cached components
    private RaycastMover _mover;

    #endregion

    #region Timers
    Timer jumpGraceTimer;
    Timer bounceTimer;
    Timer shootTimer;

    #endregion

    private void Start()
    {
        //cache components
        _mover = this.GetComponent<RaycastMover>();

        padList = new List<GameObject>();

        //init timers
        jumpGraceTimer = new Timer();
        bounceTimer = new Timer();
        shootTimer = new Timer();
    }

    void Update()
    {
        //Do not attempt to move downwards if already grounded
        if (_mover.IsGrounded && bounceTimer.IsFinished) velocity.y = 0;

        //Order of movement events matter. Be mindful of changes.
        HandleGravity();
        HandleHorizontalMovement();
        HandleJump();
        HandleShoot();

        _mover.Move(velocity * Time.deltaTime);
        //Apply corrected velocity changes
        velocity = _mover.velocity;


        //Start grace timer on the same frame we leave ground.
        if (_mover.HasLeftGround)
            jumpGraceTimer.StartTimer(jumpGraceTime);

        //When we land on group, we're no longer bouncing.
        if (_mover.HasLanded)
            bounceTimer.EndTimer();

        TickTimers();
    }

    #region Update Handle methods
    private void HandleGravity()
    {
        //REMEMBER TO ENABLE AUTOSYNC TRANSFORMS, OTHERWISE BOUNCINESS
        if (velocity.y > maxGravity)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        BoundValue(ref velocity.y, maxGravity);
    }

    private void HandleHorizontalMovement()
    {

        float horizontalMove = Input.GetAxisRaw("Horizontal");
        float currentAcceleration;
        float currentDeceleration;
        float currentDirChange;

        if (_mover.IsGrounded && !bounceTimer.IsFinished)
        {
            currentAcceleration = acceleration;
            currentDeceleration = deceleration;
            currentDirChange = dirChangeAcceleration;
        }
        else if (bounceTimer.IsFinished)
        {
            currentAcceleration = airAcceleration;
            currentDeceleration = airDeceleration;
            currentDirChange = airDirChangeAcceleration;
        }
        else
        {
            currentAcceleration = bounceAcceleration;
            currentDeceleration = bounceDeceleration;
            currentDirChange = bounceDirChangeAcceleration;
        }

        //Temporary approach to all these is weighted average
        if (Mathf.Abs(horizontalMove) != 0)
        {
            //Accelerate
            if (Mathf.Sign(velocity.x) == Mathf.Sign(horizontalMove))
            {
                float targetVelocity = Mathf.Sign(horizontalMove) * maxSpeed;
                velocity.x += (targetVelocity - velocity.x) * Time.deltaTime * currentAcceleration;
            }
            //Direction change
            else
            {
                float targetVelocity = Mathf.Sign(horizontalMove) * maxSpeed;
                velocity.x += (targetVelocity - velocity.x) * Time.deltaTime * currentDirChange;
            }
        }
        else
        {
            //Decelerate
            float decelerateForce = velocity.x * Time.deltaTime * currentDeceleration;

            if (Mathf.Abs(decelerateForce) > Mathf.Abs(velocity.x))
                velocity.x = 0;
            else
                velocity.x -= decelerateForce;

        }

        //BoundValue(ref velocity.x, Mathf.Sign(velocity.x) * maxSpeed);
    }

    private void HandleJump()
    {
        //assumes constant gravity
        if (Input.GetAxisRaw("Jump") != 0 && (!jumpGraceTimer.IsFinished || _mover.IsGrounded))
        {
            velocity.y = Mathf.Sqrt(2f * maxJumpHeight * -gravity);

            jumpGraceTimer.EndTimer();
        }

        if (Input.GetButtonUp("Jump") && velocity.y > Mathf.Sqrt(2f * minJumpHeight * -gravity))
        {
            velocity.y = Mathf.Sqrt(2f * minJumpHeight * -gravity);
        }
    }

    private void HandleShoot()
    {
        if (Input.GetMouseButtonDown(0) && shootTimer.IsFinished)
        {
            //calculate inverse of vector between mouse and player
            Vector2 clickPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = clickPos - (Vector2)transform.position;

            //normalize direction for ease-of-use.
            direction = direction.normalized;
            shootTimer.StartTimer(shotCooldown);

            //cast ray to calculate platform position.
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, Mathf.Infinity, hitLayers);

            if (hit)
            {
                //instantiate platform
                GameObject platform = Instantiate(padPrefab, hit.point, Quaternion.identity);
                float angle = Mathf.Atan2(hit.normal.x, hit.normal.y) * Mathf.Rad2Deg;
                platform.transform.rotation = Quaternion.Euler(new Vector3(0, 0, -angle));
                padList.Add(platform);
                if(padList.Count > numPadsAllowed)
                {
                    Destroy(padList[0]);
                    padList.RemoveAt(0);
                }
                
            }
        }

    }
    private void TickTimers()
    {
        jumpGraceTimer.TickTimer(Time.deltaTime);
        bounceTimer.TickTimer(Time.deltaTime);
        shootTimer.TickTimer(Time.deltaTime);
    }

    #endregion

    #region Public methods
    public void StartBounce(Vector2 initVelocity)
    {
        //Debug.Log("pre: " + velocity.normalized + " post: " + initVelocity);
        velocity = initVelocity * bounceMultiplier;
        bounceTimer.StartTimer(bounceTime);
    }
    #endregion

    #region Utilities
    private void BoundValue(ref float value, float max)
    {
        if (Mathf.Abs(value) > Mathf.Abs(max)) value = max;
    }

    private class Timer
    {
        private float initTime = 1; //initialized as 1 to prevent div by 0
        private float timer = 0;
        bool finishedLastCheck;

        public bool IsFinished
        {
            get { return timer <= 0; }
        }

        public float AsFraction()
        {
            if (timer < 0) return 0;

            return 1 - timer / initTime;
        }

        public bool HasJustFinished()
        {
            bool result = finishedLastCheck == IsFinished;
            finishedLastCheck = IsFinished;

            return result;
        }

        public void StartTimer(float startTime) { timer = this.initTime = startTime; }
        public void TickTimer(float amount) { timer -= amount; }
        public void EndTimer() { timer = 0; }
    }
    #endregion
}
