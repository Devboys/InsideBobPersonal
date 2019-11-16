using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlobZacController : MonoBehaviour
{
    [HideInInspector]
    public GameObject mother;
    [HideInInspector]
    public float blobSpeed;
    [HideInInspector]
    public float blobBurstSpeed;
    [HideInInspector]
    public Vector3 startPos;

    private Rigidbody2D rb;
    private Vector3 motherPoint;
    private EnemyZacController motherController;

    private bool moveToStart;
    private float lastDist;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        motherController = mother.GetComponent<EnemyZacController>();
        motherPoint = mother.transform.position;
    }

    // Start is called before the first frame update
    void Start()
    {
        /*
        var dir = (motherPoint - transform.position).normalized;
        rb.velocity = dir * blobSpeed;
        */

        var dir = (startPos - transform.position).normalized;
        rb.velocity = dir * blobBurstSpeed;

        moveToStart = true;
        lastDist = Vector2.Distance(startPos, transform.position);

    }

    // Update is called once per frame
    void Update()
    {

        if (moveToStart)
        {
            var dist = Vector2.Distance(startPos, transform.position);
            if (dist > lastDist) MoveToMother();
            lastDist = dist;
        }
        else {
            if (motherPoint != mother.transform.position) MoveToMother();
        }
    }

    private void MoveToMother()
    {
        moveToStart = false;
        motherPoint = mother.transform.position;
        var dir = (motherPoint - transform.position).normalized;
        rb.velocity = dir * blobSpeed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!moveToStart)
        {
            var pc = collision.gameObject.GetComponent<PlayerController>();
            if (pc)
            {
                if (pc.IsCannonBall())
                {
                    motherController.AbsorbBlob(gameObject, true);
                }
            }
            if (collision.gameObject == mother)
            {
                motherController.AbsorbBlob(gameObject);
            }
        }
    }


}
