using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

// Helper structs
struct PowerUpPair
{
    public int id;
    public bool active;
    public PowerUpPair(int id, bool active)
    {
        this.id = id;
        this.active = active;
    }
}

struct TilemapPair
{
    public int id;
    public TileBase[] tiles;
    public TilemapPair(int id, TileBase[] tiles)
    {
        this.id = id;
        this.tiles = tiles;
    }
}

[RequireComponent(typeof(RaycastMover))]
public class PlayerController : MonoBehaviour
{
    //Editor properties.
    [Header("-- Gravity")]
    [ReadOnly] public float gravity;
    [Tooltip("Maximal downwards velocity")]
    public float maxGravityMultiplier = 1.5f;

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
    [Tooltip("Allows player to queue jumps before landing")]
    public float graceTime = 0.2f;

    [Header("-- Bouncing")]
    public float bounceForce;
    public float bounceDamping;
    public float bounceVelocityCutoff;
    public float cannonballTime;

    [Header("-- Shooting")]
    public GameObject padPrefab;
    public LayerMask hitLayers;
    public float shotCooldown;
    public float numPadsAllowed;
    public float offset;
    public Gradient lineGradient;
    public Material lineMaterial;
    public Shader shader;
    public AnimationCurve timeCurve;

    // Audio variables
    [Header("-- FMOD Events")]
    [Space(20)]
    [EventRef]
    public string footstepsPath;
    private EventInstance footsteps;

    [Range(0, 10)]
    public int surfaceIndex;

    //public float footRate = 0.5f;
    //private float footDelay = 0.0f;

    [Space(20)]
    [EventRef]
    public string landPath;
    private EventInstance landSound;

    [Space(20)]
    [EventRef]
    public string jumpSound;
    [EventRef]
    public string placePad;
    [EventRef]
    public string bulletTimePath;

    //private variables
    [Header("-- State")]
    [HideInInspector] public Vector2 velocity;
    private List<GameObject> padList = new List<GameObject>();
    private bool postJumpApex;
    [HideInInspector] public float horizontalMove; //binary movement input float. 0=none, 1=right, -1=left.

    private bool inBounce;
    private bool jumping;

    private bool inBulletTime;
    private LineRenderer line;
    private bool cancelBulletTime;
    private float bulletTime;
    [HideInInspector] public float bulletTimePercentage; // Public because the audio stuff uses this, can be property
    private GameObject padPreview;
    private bool playedInBulletTime;

    private float bounceCoolDown = 0.001f;

    private GameObject lastCheckpoint;
    private Vector2 checkpointPos;
    private List<TilemapPair> tilemaps;
    private List<PowerUpPair> powerUps;

    //DEBUG
    private Vector2 lastLanding;

    #region Cached components
    private RaycastMover _mover;
    private Animator _anim;
    private SpriteRenderer _spriteRenderer;
    private ControllerInput _controllerInput;

    #endregion

    #region Timers
    Timer jumpCoyoteTimer = new Timer();
    Timer shootTimer = new Timer();
    Timer cannonballTimer = new Timer();
    Timer bounceCoolDownTimer = new Timer();
    Timer jumpGraceTimer = new Timer();

    #endregion

    private float jumpGravity
    {
        get { return (-2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2); }
    }
    private float fallGravity
    {
        get { return (-2 * maxJumpHeight) / Mathf.Pow(timeToJumpLand, 2); }
    }

    private float maxGravity
    {
        get { return (fallGravity * timeToJumpLand) * maxGravityMultiplier; }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(lastLanding, 0.2f);
    }
    private void Start()
    {
        // FMOD
        footsteps = RuntimeManager.CreateInstance(footstepsPath);
        landSound = RuntimeManager.CreateInstance(landPath);

        //cache components
        _mover = this.GetComponent<RaycastMover>();
        _anim = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _controllerInput = GetComponent<ControllerInput>();


        // init Bullet Time
        inBulletTime = false;
        cancelBulletTime = false;
        bulletTime = 0.0f;
        bulletTimePercentage = 0f;
        line = gameObject.AddComponent<LineRenderer>();
        line.material = lineMaterial;
        line.colorGradient = lineGradient;
        line.startWidth = 0.05f;
        line.endWidth = 0.05f;
        line.positionCount = 2;
        padPreview = Instantiate(padPrefab);
        padPreview.transform.parent = transform;
        padPreview.SetActive(false);
        var previewComponents = padPreview.GetComponents(typeof(Component));
        foreach (var c in previewComponents)
        {
            if (c.GetType() != typeof(Transform) && c.GetType() != typeof(SpriteRenderer)) Destroy(c);
        }

        //set init checkpoint
        checkpointPos = transform.position;
        tilemaps = new List<TilemapPair>();
        powerUps = new List<PowerUpPair>();
    }

    void Update()
    {
        //Do not attempt to move downwards if already grounded
        if (_mover.IsGrounded && !inBounce) velocity.y = 0;

        //Order of movement events matter. Be mindful of changes.
        HandleGravity();
        HandleHorizontalMovement();
        HandleJump();
        /*
        if (!_controllerInput || !_controllerInput.enabled)
        {
            UpdateBulletTime();
            HandleShoot();
        }
        */

        UpdateBulletTime();
        HandleShoot();

        //PlayFootSound();

        _mover.Move(velocity * Time.deltaTime);

        //Apply corrected velocity changes
        velocity = _mover.velocity;

        //Start grace timer on the same frame we leave ground.
        if (_mover.HasLeftGround)
        {
            jumpCoyoteTimer.StartTimer(coyoteTime);
        }
        if (_mover.HasLanded) //is true only on the single frame in which the player landed
        {
            lastLanding = transform.position;
            inBounce = false;
            jumping = false;

            //Play landing sound
            //RuntimeManager.PlayOneShot(landSound, transform.position);
            landSound.setParameterByName("SurfaceIndex", surfaceIndex);
            landSound.start();
        }
        UpdateAnimation();
        TickTimers();
    }

    private void UpdateAnimation()
    {
        _anim.SetBool("IsInCannonball", IsCannonBall());
        _anim.SetBool("Grounded", _mover.IsGrounded);
        Vector2 movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (movement.x > 0.1f)
        {
            _spriteRenderer.flipX = false;
        }
        else if (movement.x < -0.1f)
        {
            _spriteRenderer.flipX = true;
        }

        _anim.SetFloat("Horizontal Speed", Mathf.Abs(movement.x));
        _anim.SetFloat("Vertical Speed", Mathf.Abs(movement.y));
    }

    /*private void PlayFootSound()
    {
        //Checks if player is moving, grounded, and triggers footstep sounds
        if (Mathf.Abs(velocity.x) > 0.1f && _mover.IsGrounded && Time.time > footDelay)
        {
            footDelay = Time.time + footRate;

            footsteps.setParameterByName("SurfaceIndex", surfaceIndex);
            footsteps.start();
        }
    }*/

    public void PlayFootSound()
    {
        footsteps.setParameterByName("SurfaceIndex", surfaceIndex);
        footsteps.start();
    }

    private void OnDisable()
    {
        footsteps.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        landSound.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    }


    #region Update Handle methods
    private void HandleGravity()
    {
        //REMEMBER TO ENABLE AUTOSYNC TRANSFORMS, OTHERWISE BOUNCINESS
        velocity.y += gravity * Time.deltaTime;

        if (velocity.y < maxGravity)
        {
            velocity.y = maxGravity;
        }
    }
    private void HandleHorizontalMovement()
    {
        horizontalMove = Input.GetAxisRaw("Horizontal");
        float targetVelocity = horizontalMove * maxSpeed;

        if (!inBounce)
        {
            //regular ground/air movement
            if (_mover.IsGrounded)
            {
                //ground movement
                velocity.x += (targetVelocity - velocity.x) * Time.deltaTime * groundDamping;
            }
            else
            {
                //air damping movement
                velocity.x += (targetVelocity - velocity.x) * Time.deltaTime * airDamping;
            }
        }
        else
        {
            if (Mathf.Abs(velocity.x) > bounceVelocityCutoff)
            {
                //bounceDamp
                velocity.x += (targetVelocity - velocity.x) * Time.deltaTime * bounceDamping;
            }
            else if (Mathf.Abs(velocity.x) > maxSpeed)
            {
                //lerp between bounceDamping and airDamping
                float dif = (bounceVelocityCutoff - Mathf.Abs(velocity.x)) / (bounceVelocityCutoff - maxSpeed);
                float lerpedDamping = Mathf.Lerp(bounceDamping, airDamping, 1 - dif);

                velocity.x += (targetVelocity - velocity.x) * Time.deltaTime * lerpedDamping;
            }
            else 
            {
                inBounce = false; //full regular air damping
            }

        }
    }

    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump"))
        {
            jumpGraceTimer.StartTimer(graceTime);
        }

        if (!jumpGraceTimer.IsFinished && (!jumpCoyoteTimer.IsFinished || _mover.IsGrounded))
        {
            float jumpVelocity = Mathf.Abs(jumpGravity) * timeToJumpApex;
            gravity = jumpGravity;
            velocity.y = jumpVelocity;
            postJumpApex = false;
            jumping = true;

            jumpCoyoteTimer.EndTimer();

            RuntimeManager.PlayOneShot(jumpSound, transform.position);
        }

        float minJumpVel = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
        if (!Input.GetButton("Jump") && velocity.y > minJumpVel && jumping)
        {
            velocity.y = minJumpVel;
        }

        else if (velocity.y < 0 && !postJumpApex)
        {
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
            else
            {
                var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                var dir = (mousePos - transform.position).normalized;
                DrawBulletLine(dir);
            }
        }
        else if(!_controllerInput.doBulletTime)
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
    }

    public void BulletTime(bool bulletTimeStatus, Vector2 dir)
    {
        if (bulletTimeStatus)
        {
            if (!inBulletTime)
            {
                EnterBulletTime();
            }
            else
            {
                DrawBulletLine(dir);
            }
        }
        else
        {
            if (inBulletTime)
            {
                ExitBulletTime();
            }

            Time.timeScale = 1f;
        }
    }

    public void CancelBulletTime()
    {
        ExitBulletTime();
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

        if (currentTime == timeCurve.keys[timeCurve.length - 1].time)
        {
            if (!playedInBulletTime)
            {
                RuntimeManager.PlayOneShot(bulletTimePath, transform.position);
                playedInBulletTime = true;
            }

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
        playedInBulletTime = false;
        padPreview.SetActive(false);
    }

    private void DrawBulletLine(Vector2 dir)
    {
        //dir.z = 0;

        var hit = Physics2D.Raycast(transform.position, dir, Mathf.Infinity, hitLayers);
        if (hit)
        {
            padPreview.SetActive(true);
            line.SetPosition(0, transform.position);
            line.SetPosition(1, hit.point);

            padPreview.transform.position = hit.point + hit.normal * offset;
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
        if (Input.GetMouseButtonUp(0) && !Input.GetMouseButton(1))
        {
            //calculate inverse of vector between mouse and player
            Vector2 clickPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = clickPos - (Vector2)transform.position;

            if (!cancelBulletTime)
                PlacePadInDirection(direction);

            cancelBulletTime = false;
            bulletTime = 0.0f;
            bulletTimePercentage = 0;
        }
    }

    public void ShootController(Vector2 dir)
    {
        if (!cancelBulletTime)
            PlacePadInDirection(dir);

        cancelBulletTime = false;
        bulletTime = 0.0f;
        bulletTimePercentage = 0;
    }

    private void PlacePadInDirection(Vector2 direction)
    {
        shootTimer.StartTimer(shotCooldown);

        //normalize direction for ease-of-use.
        direction = direction.normalized;

        //cast ray to calculate platform position.
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, Mathf.Infinity, hitLayers);

        if (hit && shootTimer.IsFinished)
        {
            //instantiate platform
            GameObject platform = Instantiate(padPrefab, hit.point + hit.normal * offset, Quaternion.identity);
            float angle = Mathf.Atan2(hit.normal.x, hit.normal.y) * Mathf.Rad2Deg;
            platform.transform.rotation = Quaternion.Euler(new Vector3(0, 0, -angle));

            float dot = Mathf.Abs(Vector2.Dot(hit.normal, Vector2.up));
            if (dot > 0.95f && dot < 1.05f)
            {
                //bouncepad is horizontal, so regular bounce
                platform.GetComponent<BouncePadController>().fixedDirection = false;
            }
            else
            {
                //bouncepad is vertical, so upwards velocity only
                platform.GetComponent<BouncePadController>().fixedDirection = true;
            }

            padList.Add(platform);
            if (padList.Count > numPadsAllowed)
            {
                Destroy(padList[0]);
                padList.RemoveAt(0);
            }

            // Play sound for placing bounce pads
            RuntimeManager.PlayOneShot(placePad, transform.position);

        }
    }
    private void TickTimers()
    {
        jumpCoyoteTimer.TickTimer(Time.deltaTime);
        shootTimer.TickTimer(Time.deltaTime);
        cannonballTimer.TickTimer(Time.deltaTime);
        bounceCoolDownTimer.TickTimer(Time.deltaTime);
        jumpGraceTimer.TickTimer(Time.deltaTime);
    }

    #endregion

    #region Public methods
    public void StartBounce(Vector2 initVelocity)
    {
        if (!bounceCoolDownTimer.IsFinished) return;

        inBounce = true;
        jumping = false;
        velocity = initVelocity * bounceForce;
        gravity = fallGravity;

        cannonballTimer.StartTimer(cannonballTime);
        bounceCoolDownTimer.StartTimer(bounceCoolDown);
        jumpCoyoteTimer.EndTimer();
    }

    public bool IsCannonBall()
    {
        return !cannonballTimer.IsFinished;
    }

    public void SetCheckpoint(GameObject gameObject)
    {
        if(!lastCheckpoint || lastCheckpoint.GetInstanceID() != gameObject.GetInstanceID())
        {
            lastCheckpoint = gameObject;
            checkpointPos = gameObject.transform.position;
            tilemaps.Clear();
            var cTilemaps = FindObjectsOfType<Tilemap>();
            for(int i = 0; i < cTilemaps.Length; i++)
            {
                tilemaps.Add(new TilemapPair(cTilemaps[i].gameObject.GetInstanceID(), cTilemaps[i].GetTilesBlock(cTilemaps[i].cellBounds)));
            }
            powerUps.Clear();
            var cPowerUps = Resources.FindObjectsOfTypeAll<PowerUpHandler>();
            for (int i = 0; i < cPowerUps.Length; i++)
            {
                powerUps.Add(new PowerUpPair(cPowerUps[i].gameObject.GetInstanceID(), cPowerUps[i].gameObject.activeSelf));
            }
        }
    }

    public void Die()
    {
        //Refresh tilemap       
        if (tilemaps != null)
        {
            var cTilemaps = FindObjectsOfType<Tilemap>();
            for (int i = 0; i < tilemaps.Count; i++) {
                for(int j = 0; j < cTilemaps.Length; j++)
                {
                    if(tilemaps[i].id == cTilemaps[j].gameObject.GetInstanceID())
                    {
                        TileBase[] tiles = tilemaps[i].tiles;
                        var pos = EnumeratorToArray(cTilemaps[j].cellBounds.allPositionsWithin);
                        cTilemaps[i].SetTiles(pos, tiles);
                        break;
                    }
                }
            }
            var cPowerUps = Resources.FindObjectsOfTypeAll<PowerUpHandler>();
            for (int i = 0; i < powerUps.Count; i++)
            {
                for (int j = 0; j < cPowerUps.Length; j++)
                {
                    if (powerUps[i].id == cPowerUps[j].gameObject.GetInstanceID())
                    {
                        var powerUp = cPowerUps[j].gameObject;
                        powerUp.SetActive(powerUps[i].active);
                    }
                }
            }
        }

        //reset velocity
        velocity = Vector2.zero;

        //'respawn' at checkpoint
        _mover.MoveTo(checkpointPos);

        

    }

    private Vector3Int[] EnumeratorToArray(BoundsInt.PositionEnumerator enumerator) {
        List<Vector3Int> positions = new List<Vector3Int>();
        while (enumerator.MoveNext())
        {
            positions.Add(enumerator.Current);
        }

        return positions.ToArray();
    } 
    #endregion

    #region Utilities

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

    public float IsInBulletTime()
    {
        return bulletTimePercentage;
    }

    #endregion
}
