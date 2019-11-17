using System.Collections;
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
    public float acceleration;
    public float dirChangeAcceleration;
    public float deceleration;

    [Header("-- Air movement")]
    public float airAcceleration;
    public float airDirChangeAcceleration;
    public float airDeceleration;

    [Header("-- Jumping")]
    [Tooltip("Height of apex if player does not release button")]
    public float maxJumpHeight = 3f;
    [Tooltip("Jump height if player instantly releases button")]
    public float minJumpHeight = 0.5f;
    [Tooltip("Distance covered before jump apex at max speed")]
    public float JumpDistance1 = 5;
    [Tooltip("Distance covered after jump apex at max speed")]
    public float JumpDistance2 = 2;
    [Tooltip("How long to allow for jumping after walking off edges")]
    public float edgeGraceTime = 0.1f;
    [Tooltip("How early to allow for registering next jump before landing")]
    public float bunnyGraceTime = 0.2f;

    [Header("-- Bouncing")]
    public float bounceTime;
    public float bounceMultiplier;
    public float bounceAcceleration;
    public float bounceDirChangeAcceleration;
    public float bounceDeceleration;
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

    private bool inBulletTime;
    private LineRenderer line;
    private bool cancelBulletTime;
    private float bulletTime;
    public float bulletTimePercentage;
    private GameObject padPreview;

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

    void Update()
    {
        //Do not attempt to move downwards if already grounded
        if (_mover.IsGrounded && bounceTimer.IsFinished) velocity.y = 0;

        //Order of movement events matter. Be mindful of changes.
        HandleGravity();
        HandleHorizontalMovement();
        HandleJumpVariableGravity();
        UpdateBulletTime();
        HandleShoot();
        PlayFootSound();

        _mover.Move(velocity * Time.deltaTime);
        //Apply corrected velocity changes
        velocity = _mover.velocity;

        //Start grace timer on the same frame we leave ground.
        if (_mover.HasLeftGround)
        {
            jumpGraceTimer.StartTimer(edgeGraceTime);
            maxY = 0;
            initX = transform.position.x;
            totalX = transform.position.x;
            preApex = true;
            
            //Play jump sound.
            RuntimeManager.PlayOneShot(jumpSound, transform.position);
        }

        if (transform.position.y > maxY) maxY = transform.position.y;

        if(velocity.y < 0 && preApex)
        {
            postJumpApex = false;
            preApex = false;
            initX = transform.position.x - initX;
        }

        //When we land on group, we're no longer bouncing.
        if (_mover.HasLanded)
        {
            bounceTimer.EndTimer();
            
            totalX = transform.position.x - totalX;
            
            //Play landing sound.
            RuntimeManager.PlayOneShot(landSound, transform.position);
        }
        isCannonBall = IsCannonBall();
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
        velocity.y += gravity * Time.deltaTime;
        //BoundValue(ref velocity.y, maxGravity);
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

    private void HandleJumpVariableGravity()
    {
        if (Input.GetButtonDown("Jump") && (!jumpGraceTimer.IsFinished || _mover.IsGrounded))
        {
            float initVelocity = (2 * maxJumpHeight * maxSpeed) / JumpDistance1;
            float jumpGravity = (-2 * maxJumpHeight * Mathf.Pow(maxSpeed, 2)) / Mathf.Pow(JumpDistance1, 2);

            gravity = jumpGravity;
            velocity.y = initVelocity;
        }

        if(velocity.y < 0 && !postJumpApex)
        {
            //gravity = (-2 * maxJumpHeight * Mathf.Pow(maxSpeed, 2)) / Mathf.Pow(JumpDistance2, 2);
            gravity = (-2 * maxJumpHeight * Mathf.Pow(maxSpeed, 2)) / Mathf.Pow(JumpDistance2, 2);
            postJumpApex = true;
        }
    }
    private void HandleJumpConstantGravity()
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

    private void UpdateBulletTime()
    {
        if (Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !cancelBulletTime)
        {
            if (!inBulletTime)
            {
                EnterBulletTime();
                
                //Trigger FMOD ReverbStop parameter
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

    public float IsInBulletTime() {
        return bulletTimePercentage;
    }

    #endregion

    public bool IsCannonBall()
    {
        return velocity.magnitude > cannonballVelocity;
    }
}
