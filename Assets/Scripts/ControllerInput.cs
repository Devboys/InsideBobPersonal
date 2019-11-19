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

    private bool doBulletTime;
    private bool cancelBulletTime;
    private float lastBulletTimeInput;
    private Vector2 lastRightStickInput = Vector2.right;
    private PlayerController player;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    void Update()
    {
        float bulletTimeInput = Input.GetAxisRaw("BulletTime");
        float bulletTimeCancelInput = Input.GetAxisRaw("BulletTimeCancel");

        Vector2 direction = new Vector2(Input.GetAxis("HorizontalRight"), Input.GetAxis("VerticalRight"));

        if (direction.magnitude > enterBulletTimeDeadzone)
        {
            lastRightStickInput = direction;
        }

        if (useBulletTimeButton)
        {
            player.BulletTime(!cancelBulletTime && bulletTimeInput > 0.5f, lastRightStickInput);

            if (bulletTimeInput < 0.5f)
            {
                if (!cancelBulletTime && lastBulletTimeInput > 0.5f)
                    player.ShootController(direction);
                cancelBulletTime = false;
            }

            if (bulletTimeCancelInput > 0.5f)
            {
                player.CancelBulletTime();
                cancelBulletTime = true;
            }

            lastBulletTimeInput = bulletTimeInput;
        }
        else
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

            player.BulletTime(!cancelBulletTime && doBulletTime, lastRightStickInput);

            if (bulletTimeInput > 0.5f)
            {
                player.CancelBulletTime();
                cancelBulletTime = true;
            }
        }
    }
}
