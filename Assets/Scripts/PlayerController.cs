﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

[RequireComponent(typeof(RaycastMover))]
public class PlayerController : MonoBehaviour
{
    //Editor properties.
    [Header("-- Gravity")]
    public float gravity = -20;
    public float maxGravity;

    [Header("-- Ground Movement")]
    public float maxSpeed = 5;
    public float groundDamping;

    [Header("-- Air movement")]
    public float airDamping;


    [Header("-- Jumping")]
    [Tooltip("Height of apex if player does not release button")]
    public float maxJumpHeight = 3f;
    [Tooltip("Jump height if player instantly releases button")]
    public float minJumpHeight = 0.5f;
    [Tooltip("The time it takes to reach the apex of the jump-arc")]
    public float timeToJumpApex = 0.5f;
    [Tooltip("The time it takes to land after hitting the apex of the jump-arc ")]
    public float timeToJumpLand = 0.2f;
    [Tooltip("How long to allow for jumping after walking off edges")]
    public float coyoteTime = 0.1f;

    [Header("-- Bouncing")]
    public float bounceForce;
    public float bounceDamping;
    public float bounceVelocityCutoff;
    public float cannonballVelocity;

    [Header("-- Shooting")]
    public GameObject padPrefab;
    public LayerMask hitLayers;
    public float shotCooldown;
    public float numPadsAllowed;
    public Gradient lineGradient;
    public Shader shader;
    public AnimationCurve timeCurve;
    
    //Audio variables
    [Header("-- FMOD Events")] 
    [Space(20)] 
    [EventRef]
    public string footsteps;
    
    public float footRate = 0.5f;
    public float footDelay = 0.0f;
    
    [Space(20)] 
    [EventRef] 
    public string jumpSound;
    
    [EventRef] 
    public string landSound;
    

    //private variables
    [Header("-- State")]
    [ReadOnly] public Vector2 velocity;
    private List<GameObject> padList;
    private bool postJumpApex;

    [SerializeField] [ReadOnly] private bool inBounce;
    private float initBounceX;

    private bool inBulletTime;
    private LineRenderer line;
    private bool cancelBulletTime;
    private float bulletTime;
    private float bulletTimePercentage;
    private GameObject padPreview;


    //DEBUG
    private Vector2 lastLanding;

    #region Cached components
    private RaycastMover _mover;

    #endregion

    #region Timers
    Timer jumpCoyoteTimer;
    Timer bounceTimer;
    Timer shootTimer;

    #endregion

    private float jumpGravity
    {
        get { return (-2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2); }
    }
    private float fallGravity
    {
        get { return (-2 * maxJumpHeight) / Mathf.Pow(timeToJumpLand, 2); }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(lastLanding, 0.2f);
    }
    private void Start()
    {
        //cache components
        _mover = this.GetComponent<RaycastMover>();

        padList = new List<GameObject>();

        //init timers
        jumpCoyoteTimer = new Timer();
        bounceTimer = new Timer();
        shootTimer = new Timer();

        // init Bullet Time
        inBulletTime = false;
        cancelBulletTime = false;
        bulletTime = 0.0f;
        bulletTimePercentage = 0f;
        line = gameObject.AddComponent<LineRenderer>();
        line.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
        line.colorGradient = lineGradient;
        line.startWidth = 0.05f;
        line.endWidth = 0.05f;
        line.positionCount = 2;
        padPreview = Instantiate(padPrefab);
        padPreview.transform.parent = transform;
        padPreview.SetActive(false);
        var previewComponents = padPreview.GetComponents(typeof(Component));
        foreach (var c in previewComponents) {
            if(c.GetType() != typeof(Transform) && c.GetType() != typeof(SpriteRenderer)) Destroy(c);
        }
}

    //DEBUG TEST VARIABLES, DELETE WHEN JUMP ALGORITHM IS DONE
    private float maxY;
    private float initX;
    private float totalX;
    private bool preApex = true;
    public bool isCannonBall;

    void FixedUpdate()
    {
        //Do not attempt to move downwards if already grounded
        if (_mover.IsGrounded && !inBounce) velocity.y = 0;

        //Order of movement events matter. Be mindful of changes.
        HandleGravity();
        HandleHorizontalMovement();
        HandleJumpVariableGravity();
        UpdateBulletTime();
        HandleShoot();
        PlayFootSound();

        _mover.Move(velocity * Time.fixedDeltaTime);
        //Apply corrected velocity changes
        velocity = _mover.velocity;

        //Start grace timer on the same frame we leave ground.
        if (_mover.HasLeftGround)
        {
            jumpCoyoteTimer.StartTimer(coyoteTime);
        }
        if (_mover.HasLanded)
        {
            lastLanding = transform.position;
            inBounce = false;
        }
        TickTimers();
    }

    private void PlayFootSound()
    {
        //Checks if player is moving, grounded, and triggers footstep sounds.
        if (Mathf.Abs(velocity.x) > 0.1f && _mover.IsGrounded && Time.time > footDelay)
        {
            footDelay = Time.time + footRate;
            
            RuntimeManager.PlayOneShot(footsteps, transform.position);
        }
    }

    #region Update Handle methods
    private void HandleGravity()
    {
        //REMEMBER TO ENABLE AUTOSYNC TRANSFORMS, OTHERWISE BOUNCINESS
        velocity.y += gravity * Time.fixedDeltaTime;
        //BoundValue(ref velocity.y, maxGravity);
    }
    private void HandleHorizontalMovement()
    {
        float horizontalMove = Input.GetAxisRaw("Horizontal");
        float targetVelocity = horizontalMove * maxSpeed;

        if (!inBounce)
        {
            //regular ground/air movement
            if (_mover.IsGrounded)
            {
                //ground movement
                velocity.x += (targetVelocity - velocity.x) * Time.fixedDeltaTime * groundDamping;
            }
            else
            {
                //air damping movement
                velocity.x += (targetVelocity - velocity.x) * Time.fixedDeltaTime * airDamping;
            }
        }
        else
        {
            if (velocity.x > bounceVelocityCutoff)
            {
                //bounceDamp
                velocity.x += (targetVelocity - velocity.x) * Time.fixedDeltaTime * bounceDamping;
            }
            else if (velocity.x > maxSpeed)//is this the right cutoff for 2nd phase???
            {
                //lerp between bounceDamp and airDamp
                float dif = (bounceVelocityCutoff - velocity.x) / bounceVelocityCutoff - maxSpeed;
                float lerpedDamping = Mathf.Lerp(bounceDamping, airDamping, 1 - dif);

                velocity.x += (targetVelocity - velocity.x) * Time.fixedDeltaTime * lerpedDamping;
            }
            else
            {
                inBounce = false; //full regular air damping
            }

        }
    }

    private void HandleJumpVariableGravity()
    {
        if (Input.GetButtonDown("Jump") && (!jumpCoyoteTimer.IsFinished || _mover.IsGrounded))
        {

            //float jumpGravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
            float jumpVelocity = Mathf.Abs(jumpGravity) * timeToJumpApex;
            gravity = jumpGravity;
            velocity.y = jumpVelocity;
            postJumpApex = false;

            jumpCoyoteTimer.EndTimer();
        }

        float minJumpVel = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
        if (Input.GetButtonUp("Jump") && velocity.y > minJumpVel)
        {
            velocity.y = minJumpVel;
        }

        else if (velocity.y < 0 && !postJumpApex)
        {
            //float jumpGravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToJumpLand, 2);
            gravity = fallGravity;
            postJumpApex = true;
        }

    }

    private void UpdateBulletTime()
    {
        if (Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !cancelBulletTime)
        {
            if (!inBulletTime)
            {
                EnterBulletTime();
            }
            else {
                DrawBulletLine();
            }
        }
        else
        {
            if (inBulletTime)
            {
                if (Input.GetMouseButton(1))
                {
                    cancelBulletTime = true;
                }
                ExitBulletTime();
            }

            Time.timeScale = 1f;
        }

        /*Vector2 rightInput = new Vector2(Input.GetAxisRaw("RightHorizontal"), Input.GetAxisRaw("RightVertical"));
        if (rightInput.magnitude > 0.1f && Input.GetAxisRaw("Cancel") == 0 && !cancelBulletTime)
        {
            if (!inBulletTime)
            {
                EnterBulletTime();
            }
            else
            {
                DrawBulletLine(rightInput);
            }
        }
        else
        {
            if (inBulletTime)
            {
                if (Input.GetAxisRaw("Cancel") != 0)
                {
                    cancelBulletTime = true;
                }
                ExitBulletTime();
            }
        }*/
    }

    private void EnterBulletTime()
    {
        var endTime = timeCurve.keys[timeCurve.length - 1].time;
        bulletTime = (bulletTime + Time.unscaledDeltaTime);

        var currentTime = bulletTime < endTime ? bulletTime : endTime;

        float endBulletTimeValue = timeCurve.keys[timeCurve.length - 1].value;

        float curValue = timeCurve.Evaluate(currentTime);
        Time.timeScale = curValue;
        bulletTimePercentage = (1 - curValue) * (1 / (1 - endBulletTimeValue));

        if (currentTime == timeCurve.keys[timeCurve.length - 1].time) {
            bulletTime = 0.0f;
            inBulletTime = true;
            line.enabled = true;
        }
    }

    private void ExitBulletTime()
    {
        bulletTimePercentage = 0;
        inBulletTime = false;
        Time.timeScale = 1.0f;
        line.enabled = false;
        padPreview.SetActive(false);
    }

    private void DrawBulletLine()
    {
        var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var dir = (mousePos - transform.position).normalized;
        DrawBulletLine(dir);
    }

    private void DrawBulletLine(Vector2 dir)
    {
        //dir.z = 0;

        var hit = Physics2D.Raycast(transform.position, dir, int.MaxValue, hitLayers);
        if (hit)
        {
            padPreview.SetActive(true);
            line.SetPosition(0, transform.position);
            line.SetPosition(1, hit.point);

            padPreview.transform.position = hit.point;
            Quaternion rot = Quaternion.FromToRotation(Vector2.up, hit.normal);
            padPreview.transform.rotation = rot;
        }
        else
        {
            padPreview.SetActive(false);
            line.SetPosition(0, transform.position);
            line.SetPosition(1, dir * 100);
        }
    }

    private void HandleShoot()
    {
        if (Input.GetMouseButtonUp(0) && shootTimer.IsFinished && !cancelBulletTime)
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
        if (Input.GetMouseButtonUp(0))
        {
            cancelBulletTime = false;
            bulletTime = 0.0f;
            bulletTimePercentage = 0;
        }

        /*// Controller input
        Vector2 rightInput = new Vector2(Input.GetAxisRaw("RightHorizontal"), Input.GetAxisRaw("RightVertical"));
        if (rightInput.magnitude > 0.1f && shootTimer.IsFinished && !cancelBulletTime)
        {
            Vector2 direction = rightInput;

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
                if (padList.Count > numPadsAllowed)
                {
                    Destroy(padList[0]);
                    padList.RemoveAt(0);
                }

            }
        }
        if (Input.GetAxisRaw("Cancel") != 0)
        {
            cancelBulletTime = false;
            bulletTime = 0.0f;
            bulletTimePercentage = 0;
        }*/

    }
    private void TickTimers()
    {
        jumpCoyoteTimer.TickTimer(Time.fixedDeltaTime);
        bounceTimer.TickTimer(Time.fixedDeltaTime);
        shootTimer.TickTimer(Time.fixedDeltaTime);
    }

    #endregion

    #region Public methods
    public void StartBounce(Vector2 initVelocity)
    {
        inBounce = true;
        velocity = initVelocity * bounceForce;
        gravity = fallGravity;
        initBounceX = velocity.x;
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

    public float IsInBulletTime() {
        return bulletTimePercentage;
    }

    #endregion

    public bool IsCannonBall()
    {
        return velocity.magnitude > cannonballVelocity;
    }
}
