using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyZacController : MonoBehaviour
{
    public GameObject blobZac;
    public LayerMask obstacles;
    public int size;
    public float rangeMax;
    public float rangeMin;
    public float blobSpeed;
    public float blobBurstSpeed;

    private List<GameObject> blobs;

    private float initialVolume;
    private int initialSize;
    private int absorbedBlobs;

    private bool dead;

    private void Awake()
    {
        blobs = new List<GameObject>();
        initialVolume = Mathf.PI * Mathf.Pow(transform.localScale.x, 2);
        initialSize = size + 1;
    }

    // Start is called before the first frame update
    void Start()
    {
        dead = false;
        absorbedBlobs = size;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateSize();

        if (dead && blobs.Count == 0) {
            dead = false;
            if (absorbedBlobs == 0) Destroy(gameObject);
        }
    }

    private void UpdateSize()
    {
        var radius = Mathf.Sqrt(initialVolume * (absorbedBlobs + 1) / initialSize) / Mathf.Sqrt(Mathf.PI);
        transform.localScale = new Vector2(radius, radius);
    }

    public void AbsorbBlob(GameObject blob, bool died = false) {
        blobs.Remove(blob);
        if(!died) absorbedBlobs++;
        Destroy(blob);
    }

    private void SplitMonster()
    {
        for (int i = 0; i < absorbedBlobs; i++)
        {
            var monster = Instantiate(blobZac);
            var bzController = monster.GetComponent<BlobZacController>();
            bzController.mother = gameObject;
            bzController.blobSpeed = blobSpeed;
            bzController.blobBurstSpeed = blobBurstSpeed;
            bzController.startPos = GetBlobZacValidPosition();
            monster.SetActive(true);
            monster.transform.position = transform.position;
            blobs.Add(monster);
        }
        dead = true;
        absorbedBlobs = 0;
    }

    private Vector2 GetBlobZacValidPosition() {
        var pos = GetBlobZacPosition();
        var dir = (pos - transform.position).normalized;
        var hit = Physics2D.Raycast(transform.position, dir, Vector2.Distance(pos, transform.position), obstacles);
        if (hit) return GetBlobZacValidPosition();
        return pos;
    }

    private Vector3 GetBlobZacPosition() {
        var r = Random.Range(rangeMin, rangeMax);
        var a = Random.Range(0, Mathf.PI * 2);
        var o = transform.position;

        var x = o.x + r * Mathf.Cos(a);
        var y = o.y + r * Mathf.Sin(a);

        return new Vector2(x, y);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {

        var pc = collision.gameObject.GetComponent<PlayerController>();
        if (!dead && pc)
        {
            if (pc.IsCannonBall())
            {
                SplitMonster();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, rangeMax);
        Gizmos.DrawWireSphere(transform.position, rangeMin);
    }

}
