using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplinterMonster : MonoBehaviour
{

    public Vector2 movement;
    public float multiplyTime;
    public int splitNumber;
    public float range;

    public int size = 1;
    private int initialSize;

    private Transform player;

    private Timer multiplyTimer;

    private float directionCooldown = 0;

    private float initialVolume;
    private Vector2 inititalScale;

    // Start is called before the first frame update
    void Start()
    {
        multiplyTimer = new Timer();
        multiplyTimer.StartTimer(multiplyTime);
        player = GameObject.FindGameObjectWithTag("Player").transform;
        initialVolume = Mathf.PI * Mathf.Pow(transform.localScale.x, 2);
        initialSize = size;
        inititalScale = transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        CheckSplitMonster();
        UpdateSize();
    }

    private void UpdateSize()
    {

        //var scale = Mathf.Sqrt(size / Mathf.PI);

        var radius = Mathf.Sqrt(initialVolume * size / initialSize) / Mathf.Sqrt(Mathf.PI);
        transform.localScale = new Vector2(radius, radius);
    }

    private bool PlayerInRange()
    {
        return (Vector2.Distance(transform.position, player.position) <= range);
    }

    private void CheckSplitMonster()
    {
        if (PlayerInRange())
        {
            TickTimers(Time.deltaTime * 1000);
            if (multiplyTimer.isFinished)
            {
                size++;
                multiplyTimer.StartTimer(multiplyTime);
            }
        }
        if (size >= splitNumber)
        {
            SplitMonster();
        }
    }

    private void SplitMonster()
    {
        for (int i = 0; i < splitNumber; i++)
        {
            var monster = (GameObject) Instantiate(Resources.Load("PreFabs/SplinterMonster"));
            var sm = monster.GetComponent<SplinterMonster>();
            monster.transform.position = transform.position;
            sm.multiplyTime = multiplyTime;
            sm.splitNumber = splitNumber;
            sm.range = range;
            monster.transform.localScale = inititalScale;
        }
        Die();
    }

    private void TickTimers(float deltaTime)
    {
        multiplyTimer.TickTimer(deltaTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        var pc = collision.gameObject.GetComponent<PlayerController>();
        if (pc)
        {
            if (pc.IsCannonBall())
            {
                if (--size == 0)
                {
                    Die();
                }
            }
        }
    }

    public void Die()
    {
        Destroy(gameObject);
    }

    private class Timer
    {
        float timer;

        public bool isFinished
        {
            get { return timer <= 0; }
        }

        public void StartTimer(float totalTime) { timer = totalTime; }

        public void TickTimer(float amount) { timer -= amount; }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, range);
    }

}
