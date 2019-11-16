using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyZacController : MonoBehaviour
{


    public Transform player;
    public GameObject blobZac;
    public LayerMask obstacles;
    public int size;
    public float rangeMax;
    public float rangeMin;

    private float initialVolume;
    private Vector2 inititalScale;
    private int initialSize;

    private void Awake()
    {
        initialVolume = Mathf.PI * Mathf.Pow(transform.localScale.x, 2);
        initialSize = size;
        inititalScale = transform.localScale;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void SplitMonster()
    {
        for (int i = 0; i < size; i++)
        {
            var monster = Instantiate(blobZac);
            var bzController = monster.GetComponent<BlobZacController>();
            bzController.motherPoint = transform.position;
            monster.transform.position = GetBlobZacValidPosition();
            monster.SetActive(true);
        }
    }

    private Vector2 GetBlobZacValidPosition() {
        var pos = GetBlobZacPosition();
        var dir = (transform.position - pos).normalized;
        var hit = Physics2D.Raycast(transform.position, dir, rangeMax, obstacles);
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
        Debug.Log("col");
        var pc = collision.gameObject.GetComponent<PlayerController>();
        if (pc)
        {
            if (pc.IsCannonBall())
            {
                SplitMonster();
            }
        }
    }
}
