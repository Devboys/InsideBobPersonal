using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
using System;

// Helper structs
struct GameObjectPair
{
    public int id;
    public bool active;
    public GameObjectPair(int id, bool active)
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

public struct RemoverInfo
{
    public Tilemap tilemap;
    public Vector3 startPos;
    public Vector3Int pos;
    public Vector3 velocity;
    public RemoverInfo(Tilemap tilemap, Vector3 startPos, Vector3Int pos, Vector3 velocity)
    {
        this.tilemap = tilemap;
        this.startPos = startPos;
        this.pos = pos;
        this.velocity = velocity;
    }

}

[RequireComponent(typeof(RaycastMover))]
public class PlayerController : MonoBehaviour
{
    //Editor properties.
    [Header("-- Properties")]
    public float maxHP = 100f;
    [ReadOnly]public float health;
    [ReadOnly] public bool isDead;

    public GameObject bobplosionPrefab;

    public float spikeDamageInitial = 10f;
    [Tooltip("Damage pr Second")]
    public float spikeDamageStay = 100f;

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
    public GameObject padPreviewPrefab;
    public LayerMask hitLayers;
    public float shotCooldown;
    public int numPadsAllowed;
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

    [Space(20)]
    [EventRef]
    public string landPath;
    private EventInstance landSound;
    [EventRef]
    public string bulletTimePath;

    [Space(20)]
    [EventRef]
    public string jumpSound;
    [EventRef]
    public string placePad;
    [EventRef]
    public string deathSwirl;
    [EventRef]
    public string deathSpin;
    [EventRef]
    public string deathLand;
    
    

    // Variable Enable Movement
    [HideInInspector]
    public bool canMove;

    private int totalPills;
    [HideInInspector] public int totalPillsPickedUp {
    get { return totalPills; }
    set {
            totalPills = value;
            OnPillPickup?.Invoke(); //invoke any subscriptions to event (if not null)
        }
    }
    public event Action OnPillPickup;
    public event Action OnPadPickup;

    public event Action OnRespawnEvent;
    

    public KeyCode[] restartButtons;
    
    //private variables
    [Header("-- State")]
    [HideInInspector] public Vector2 velocity;
    [HideInInspector] public List<GameObject> padList = new List<GameObject>();
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

    // Checkpoint stuff
    private GameObject lastCheckpoint;
    private Vector2 checkpointPos;
    private List<TilemapPair> tilemaps;
    private List<GameObjectPair> pills;
    private List<GameObjectPair> powerUps;
    private List<RemoverInfo> removers;
    private int totalPillCounter;
    private int totalBounceCounter;

    public bool playerHasDied;


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
        // Init properties
        health = maxHP;

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
        padPreview = Instantiate(padPreviewPrefab);
        padPreview.transform.parent = transform;
        padPreview.SetActive(false);
        var previewComponents = padPreview.GetComponents(typeof(Component));
        foreach (var c in previewComponents)
        {
            if (c.GetType() != typeof(Transform) && c.GetType() != typeof(SpriteRenderer)) Destroy(c);
        }

        //set init checkpoint
        tilemaps = new List<TilemapPair>();
        pills = new List<GameObjectPair>();
        powerUps = new List<GameObjectPair>();
        removers = new List<RemoverInfo>();
        SetCheckpoint(gameObject); // checkpointPos = transform.position;

        // Can move variable
        canMove = true;

        playerHasDied = false;
    }

    void Update()
    {
        //Do not attempt to move downwards if already grounded
        if (_mover.IsGrounded && !inBounce) velocity.y = 0;

        //Order of movement events matter. Be mindful of changes.
        HandleGravity();
        if (canMove)
        {
            HandleHorizontalMovement();
            HandleJump();
            UpdateBulletTime();
            HandleShoot();
        }
        else {
            if (_mover.IsGrounded) velocity.x = 0;
        }

        foreach (KeyCode button in restartButtons)
        {
            if (Input.GetKeyUp(button))
                Implode();
        }

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
        if (isDead)
            return;
        
        _anim.SetBool("IsInCannonball", IsCannonBall());
        _anim.SetBool("Grounded", _mover.IsGrounded);
        Vector2 movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (canMove)
        {
            if (movement.x > 0.1f)
            {
                _spriteRenderer.flipX = false;
            }
            else if (movement.x < -0.1f)
            {
                _spriteRenderer.flipX = true;
            }

            _anim.SetFloat("Horizontal Speed", Mathf.Abs(movement.x));
        }
        else
        {
            _anim.SetFloat("Horizontal Speed", 0);
        }
        _anim.SetFloat("Vertical Speed", Mathf.Abs(movement.y));
    }

    public void PlayFootSound()
    {
        footsteps.setParameterByName("SurfaceIndex", surfaceIndex);
        footsteps.start();
    }

    public void PlayDeathLandSound()
    {
        landSound.start(); // Play landing sound for event on last frame of death animation
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
        if (isDead)
            return;
        
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
        if (isDead)
            return;
        
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
        if (isDead)
            return;
        
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
        if (isDead)
            return;
        
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
        if (isDead || numPadsAllowed <= 0)
            return;
        
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
        if (isDead)
            return;
        
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
        if (isDead)
            return;
        
        if (!cancelBulletTime)
            PlacePadInDirection(dir);

        cancelBulletTime = false;
        bulletTime = 0.0f;
        bulletTimePercentage = 0;
    }

    private void PlacePadInDirection(Vector2 direction)
    {
        if (isDead || numPadsAllowed <= 0)
            return;
        
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
            /*if (padList.Count > numPadsAllowed)
            {
                Destroy(padList[0]);
                padList.RemoveAt(0);
            }*/

            numPadsAllowed--;
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
            // Reset HP and set checkpoint location
            ResetHP();
            lastCheckpoint = gameObject;
            checkpointPos = gameObject.transform.position;
            //  Save Tilemap state
            tilemaps.Clear();
            var cTilemaps = FindObjectsOfType<Tilemap>();
            for(int i = 0; i < cTilemaps.Length; i++)
            {
                tilemaps.Add(new TilemapPair(cTilemaps[i].gameObject.GetInstanceID(), cTilemaps[i].GetTilesBlock(cTilemaps[i].cellBounds)));
            }
            // Save Pill state
            pills.Clear();
            var cPills = Resources.FindObjectsOfTypeAll<PillHandler>();
            for (int i = 0; i < cPills.Length; i++)
            {
                pills.Add(new GameObjectPair(cPills[i].gameObject.GetInstanceID(), cPills[i].gameObject.activeSelf));
            }
            // Save PowerUp state
            powerUps.Clear();
            var cPowerUps = Resources.FindObjectsOfTypeAll<PowerupHandler>();
            for (int i = 0; i < cPowerUps.Length; i++)
            {
                powerUps.Add(new GameObjectPair(cPowerUps[i].gameObject.GetInstanceID(), cPowerUps[i].gameObject.activeSelf));
            }
            // Set Remover state
            removers.Clear();
            var cRemovers = FindObjectsOfType<RemoverController>();
            for (int i = 0; i < cRemovers.Length; i++)
            {
                var r = cRemovers[i];
                removers.Add(new RemoverInfo(r.tilemap, r.gameObject.transform.position, r.pos, r.gameObject.GetComponent<Rigidbody2D>().velocity));
            }
            // Set pill counter state & bouncepads allowed
            totalPillCounter = totalPillsPickedUp;
            totalBounceCounter = numPadsAllowed;
        }
    }

    public void TakeDamage(float damage) {
        // TODO: SOUND (Take Damage sound like ouch, uh) idk
        GetHP(-damage);
    }

    public void GetHP(float hp) {
        health += hp;
        if (health <= 0) {
            health = 0;
            Die();
        }
    }

    public void ResetHP() {
        health = maxHP;
    }

    public void Die()
    {
        if (!Application.isPlaying || isDead)
            return;

        // TODO: SOUND (Die sound?)
        

        //reset velocity
        velocity = Vector2.zero;
        
        isDead = true;
        CancelBulletTime();
        _anim.SetTrigger("Die");
    }

    public void Implode() // Called when Respawn button is clicked
    {
        RuntimeManager.PlayOneShot(deathSwirl, transform.position); // Play death swirl sound
        
        velocity.x = velocity.y = 0;
        _anim.SetTrigger("Implode");
    }

    public void SpawnBobplosion()
    {
       RuntimeManager.PlayOneShot(deathSpin, transform.position); // Play sound for particle explosion
        
        var obj = Instantiate(bobplosionPrefab);
        obj.transform.position = transform.position;
    }

    public void Respawn() // Currently only called from animation event
    {
        velocity.x = velocity.y = 0;
        ResetHP();
        isDead = false;
        //Remove pads
        foreach (GameObject pad in padList)
        {
            Destroy(pad);
        }
        var levelC = FindObjectOfType<LevelController>();
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
                        cTilemaps[j].SetTiles(pos, tiles);
                        break;
                    }
                }
            }
            var cPills = Resources.FindObjectsOfTypeAll<PillHandler>();
            for (int i = 0; i < pills.Count; i++)
            {
                for (int j = 0; j < cPills.Length; j++)
                {
                    if (pills[i].id == cPills[j].gameObject.GetInstanceID())
                    {
                        var powerUp = cPills[j].gameObject;
                        powerUp.SetActive(pills[i].active);
                    }
                }
            }
            var cPowerUps = Resources.FindObjectsOfTypeAll<PowerupHandler>();
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
            var cRemovers = FindObjectsOfType<RemoverController>();
            foreach (var remover in cRemovers) {
                Destroy(remover.gameObject);
            }
            foreach (RemoverInfo info in removers)
            {
                var obj = Instantiate(levelC.remover);
                obj.GetComponent<RemoverController>().info = info;
            }

            //PLAYER HAS DIED IS A HACKY WAY OF MAKING SURE THE PILL SOUND DOESNT PLAY ON RESPAWN
            playerHasDied = true;

            totalPillsPickedUp = totalPillCounter;
            numPadsAllowed = totalBounceCounter;
            playerHasDied = false;
        }

        //'respawn' at checkpoint
        
        RuntimeManager.PlayOneShot(deathSwirl, transform.position); // Play death swirl sound
        
        _anim.SetTrigger("Respawn");
        _mover.MoveTo(checkpointPos);
        levelC.ForceUpdatePillCountForCurrentLevel();
        
        OnRespawnEvent?.Invoke();
        
    }

    private Vector3Int[] EnumeratorToArray(BoundsInt.PositionEnumerator enumerator) {
        List<Vector3Int> positions = new List<Vector3Int>();
        while (enumerator.MoveNext())
        {
            positions.Add(enumerator.Current);
        }

        return positions.ToArray();
    }

    public void AddPlatform(int pads)
    {
        numPadsAllowed += pads;
        OnPadPickup?.Invoke();
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
