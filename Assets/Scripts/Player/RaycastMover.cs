using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * SCRIPT REQUIREMENTS:
 * - RigidBody2D with Kinematic body type
 * - Auto Sync Transforms enabled in Physics2D settings.
 */

[RequireComponent(typeof(Collider2D))]
//[RequireComponent(typeof(Rigidbody2D))] //SHOULD(must?) BE KINEMATIC
public class RaycastMover : MonoBehaviour
{

    [Header("Collision Checks")]
    [Tooltip("The layers that are considered in collisions")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private int numHorizontalRays = 3;
    [SerializeField] private int numVerticalRays = 3;
    [SerializeField] public float skinWidth = 0.02f;

    [Tooltip("Distance considered to be 'next to wall' from left/right raycast origins")]
    [SerializeField] private float wallCheckWidth;

    [Header("Pad Checking")]
    [SerializeField] private LayerMask padMask;
    [SerializeField] private int numHoriPadRays = 3;
    [SerializeField] private int numVertPadRays = 3;

    [HideInInspector]
    public Vector2 velocity
    {
        get;
        private set;
    }

    //component cache.
    private BoxCollider2D _boxCollider;

    //ray variables
    private RayCastOrigins rayOrigins;
    private float rayHeight;
    private float rayWidth;

    //wall check
    [ReadOnly] [SerializeField] private bool wallOnRight;
    [ReadOnly] [SerializeField] private bool wallOnLeft;

    //collision state
    private CharacterCollisionState2D collisionState;
    private CharacterCollisionState2D padCheckState;

    #region Read only properties
    //Collision state Read-Only properties
    [HideInInspector] public bool IsGrounded { get { return collisionState.below; } }
    [HideInInspector] public bool CollidedLeft { get { return collisionState.left; } }
    [HideInInspector] public bool CollidedRight { get { return collisionState.right; } }
    [HideInInspector] public bool CollidedAbove { get { return collisionState.above; } }
    [HideInInspector] public bool IsRightOfWall { get { return wallOnRight; } }
    [HideInInspector] public bool IsLeftOfWall { get { return wallOnLeft; } }
    [HideInInspector]
    public bool HasLanded
    {
        get { return collisionState.becameGroundedThisFrame; }
    }
    [HideInInspector]
    public bool HasLeftGround
    {
        get { return collisionState.leftGroundThisFrame; }
    }
    [HideInInspector]
    public bool HasCollidedHorizontal
    {
        get { return (collisionState.left || collisionState.right); }
    }
    #endregion

    [HideInInspector] public Vector2 lastGroundedPosition;

    public void Awake()
    {
        _boxCollider = this.GetComponent<BoxCollider2D>();
        rayOrigins = new RayCastOrigins();

        RecalculateDistanceBetweenRays();
        collisionState = new CharacterCollisionState2D();
        padCheckState = new CharacterCollisionState2D();
    }

    public void Move(Vector2 deltaMovement)
    {
        collisionState.wasGroundedLastFrame = collisionState.below;
        collisionState.Reset();
        padCheckState.Reset();

        PrimeRayCastOrigins();

        CheckPads(deltaMovement);

        CheckIsNextToWall();

        //Do movement & update collisionState for the current frame.
        if (deltaMovement.x != 0f)
            MoveHorizontal(ref deltaMovement);
        if (deltaMovement.y != 0f)
            MoveVertical(ref deltaMovement);

        //update post-movement collisonState.
        if (!collisionState.wasGroundedLastFrame && collisionState.below && !padCheckState.below)
            collisionState.becameGroundedThisFrame = true;
        if (collisionState.wasGroundedLastFrame && !collisionState.below)
            collisionState.leftGroundThisFrame = true;

        transform.Translate(deltaMovement, Space.World);
        velocity = deltaMovement / Time.deltaTime;
    }

    private void CheckIsNextToWall()
    {
        float rayDistance = wallCheckWidth;

        wallOnRight = wallOnLeft = false;

        for (int i = 0; i < numVerticalRays; i++)
        {
            Vector2 rightRay = rayOrigins.bottomRight;
            rightRay.y += i * rayHeight;
            Vector2 leftRay = rayOrigins.bottomLeft;
            leftRay.y += i * rayHeight;

            RaycastHit2D rightHit = Physics2D.Raycast(rightRay, Vector2.right, rayDistance, groundMask);
            RaycastHit2D leftHit = Physics2D.Raycast(leftRay, Vector2.left, rayDistance, groundMask);

            if (rightHit)
            {
                wallOnRight = true;
                break;
            }

            if (leftHit)
            {
                wallOnLeft = true;
                break;
            }
        }
    }

    public void MoveTo(Vector2 position)
    {
        transform.position = position;
        velocity = Vector2.zero;
        collisionState.Reset();
    }

    #region movement methods
    private void MoveHorizontal(ref Vector2 deltaMovement)
    {
        bool isGoingRight = deltaMovement.x > 0;
        float rayDistance = Mathf.Abs(deltaMovement.x) + skinWidth;
        Vector2 rayDirection = isGoingRight ? Vector2.right : Vector2.left;
        Vector2 initialRayOrigin = isGoingRight ? rayOrigins.bottomRight : rayOrigins.bottomLeft;

        for (int i = 0; i < numVerticalRays; i++)
        {
            Vector2 ray = initialRayOrigin;
            ray.y += rayHeight * i;

            RaycastHit2D rayHit = Physics2D.Raycast(ray, rayDirection, rayDistance, groundMask);

            if (rayHit)
            {
                deltaMovement.x = rayHit.point.x - ray.x;
                rayDistance = Mathf.Abs(deltaMovement.x);

                if (isGoingRight)
                {
                    deltaMovement.x -= skinWidth;
                    collisionState.right = true;
                }
                else
                {
                    deltaMovement.x += skinWidth;
                    collisionState.left = true;
                }
            }
        }
    }

    private void MoveVertical(ref Vector2 deltaMovement)
    {
        bool isGoingUp = deltaMovement.y > 0;
        float rayDistance = Mathf.Abs(deltaMovement.y) + skinWidth;
        Vector2 rayDirection = isGoingUp ? Vector2.up : Vector2.down;
        Vector2 initialRayOrigin = isGoingUp ? rayOrigins.topLeft : rayOrigins.bottomLeft;

        initialRayOrigin.x += deltaMovement.x;

        for (int i = 0; i < numHorizontalRays; i++)
        {
            Vector2 rayOrigin = initialRayOrigin;
            rayOrigin.x += rayWidth * i;

            RaycastHit2D rayHit = Physics2D.Raycast(rayOrigin, rayDirection, rayDistance, groundMask);

            if (rayHit)
            {
                deltaMovement.y = rayHit.point.y - rayOrigin.y;
                rayDistance = Mathf.Abs(deltaMovement.y);

                if (isGoingUp)
                {
                    deltaMovement.y -= skinWidth;
                    collisionState.above = true;
                }
                else
                {

                    deltaMovement.y += skinWidth;
                    if(padCheckState.below == false)
                        collisionState.below = true;

                    //record last grounded position on rayhit for more accuracy than transform
                    if (padCheckState.below == false)
                    {
                        
                        lastGroundedPosition = rayHit.point;
                    }
                }
            }
        }
    }

    private void CheckPads(in Vector2 deltaMovement)
    {
        //check Vertical
        bool isGoingUp = deltaMovement.y > 0;
        float rayDistance = Mathf.Abs(deltaMovement.y) + skinWidth;
        Vector2 rayDirection = isGoingUp ? Vector2.up : Vector2.down;
        Vector2 initialRayOrigin = isGoingUp ? rayOrigins.topLeft : rayOrigins.bottomLeft;

        initialRayOrigin.x += deltaMovement.x;

        float rayOffset = _boxCollider.size.y * Mathf.Abs(transform.localScale.y) - (2f * skinWidth);

        //we 'back-up' rayOrigin a bit to handle when the player is inside the pad collider at checktime
        //this is because raycasts do not hit colliders they originate within.
        initialRayOrigin.y += isGoingUp ? -rayOffset : rayOffset;
        rayDistance += rayOffset;

        for (int i = 0; i < numHorizontalRays; i++)
        {
            Vector2 rayOrigin = initialRayOrigin;
            rayOrigin.x += rayWidth * i;

            RaycastHit2D rayHit = Physics2D.Raycast(rayOrigin, rayDirection, rayDistance, padMask);
            

            if (rayHit)
            {
                if (isGoingUp)
                {
                    padCheckState.above = true;
                }
                else
                {
                    padCheckState.below = true;
                }
            }
        }

    }
    #endregion

    #region utility methods
    private void RecalculateDistanceBetweenRays()
    {
        float colliderUsableWidth = _boxCollider.size.x * Mathf.Abs(transform.localScale.x) - (2f * skinWidth);
        rayWidth = colliderUsableWidth / (numHorizontalRays - 1);

        float colliderUsableHeight = _boxCollider.size.y * Mathf.Abs(transform.localScale.y) - (2f * skinWidth);
        rayHeight = colliderUsableHeight / (numVerticalRays - 1);
    }

    private void PrimeRayCastOrigins()
    {
        Bounds modifiedBounds = _boxCollider.bounds;
        //shrink modified bounds by 1 skinwidth on each side.
        modifiedBounds.Expand(-2f * skinWidth);

        //every raycast origin lies on the modified bounds.
        rayOrigins.topLeft = new Vector2(modifiedBounds.min.x, modifiedBounds.max.y);
        rayOrigins.topRight = new Vector2(modifiedBounds.max.x, modifiedBounds.max.y);
        rayOrigins.bottomLeft = new Vector2(modifiedBounds.min.x, modifiedBounds.min.y);
        rayOrigins.bottomRight = new Vector2(modifiedBounds.max.x, modifiedBounds.min.y);
    }
    #endregion

    #region inner types
    struct RayCastOrigins
    {
        public Vector2 topLeft;
        public Vector2 topRight;
        public Vector2 bottomRight;
        public Vector2 bottomLeft;
    }

    class CharacterCollisionState2D
    {
        public bool right;
        public bool left;
        public bool above;
        public bool below;
        public bool becameGroundedThisFrame;
        public bool wasGroundedLastFrame;
        public bool leftGroundThisFrame;

        public bool movingDownSlope;
        public float slopeAngle;

        public void Reset()
        {
            right = left = above = below = becameGroundedThisFrame = movingDownSlope = leftGroundThisFrame = false;
            slopeAngle = 0f;
        }
    }

    #endregion
}
