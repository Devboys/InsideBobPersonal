using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class ControllerInput : MonoBehaviour
{
    [Range(0, 1)]
    public float enterBulletTimeDeadzone;
    [Range(0, 1)]
    public float exitBulletTimeDeadzone;

    public bool useBulletTimeButton;

    public bool doBulletTime;
    private bool cancelBulletTime;
    private bool lastBulletTimeInput;
    private Vector2 lastRightStickInput = Vector2.right;
    private PlayerController player;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    void Update()
    {
        bool bulletTimeInput = Input.GetAxisRaw("BulletTime") > 0.5f;
        bool bulletTimeCancelInput = Input.GetAxisRaw("BulletTimeCancel") > 0.5f;

        Vector2 direction = new Vector2(Input.GetAxis("HorizontalRight"), Input.GetAxis("VerticalRight"));

        if (direction.magnitude > enterBulletTimeDeadzone)
        {
            lastRightStickInput = direction;
        }

        if (useBulletTimeButton)
        {
            HandleBulletTimeButton(direction, bulletTimeInput, bulletTimeCancelInput);
        }
        else
        {
            HandleBulletTime(direction, bulletTimeInput);
        }
    }

    private void HandleBulletTimeButton(Vector2 direction, bool bulletTimeInput, bool bulletTimeCancelInput)
    {
        player.BulletTime(!cancelBulletTime && bulletTimeInput, lastRightStickInput);

        if (!bulletTimeInput)
        {
            if (!cancelBulletTime && lastBulletTimeInput)
                player.ShootController(direction);
            cancelBulletTime = false;
        }

        if (bulletTimeCancelInput)
        {
            player.CancelBulletTime();
            cancelBulletTime = true;
        }

        lastBulletTimeInput = bulletTimeInput;
    }

    private void HandleBulletTime(Vector2 direction, bool bulletTimeCancelInput)
    {

        if (direction.magnitude > enterBulletTimeDeadzone)
        {
            doBulletTime = true;
        }
        else if (direction.magnitude < exitBulletTimeDeadzone && doBulletTime)
        {
            if (!cancelBulletTime)
                player.ShootController(lastRightStickInput);
            doBulletTime = false;
            cancelBulletTime = false;
        }

        if(!Input.GetMouseButton(0) && !Input.GetMouseButton(1)) player.BulletTime(!cancelBulletTime && doBulletTime, lastRightStickInput);

        if (doBulletTime && bulletTimeCancelInput)
        {
            player.CancelBulletTime();
            cancelBulletTime = true;
        }
    }
}