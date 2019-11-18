using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour
{

    public Vector2 movement;
    private Rigidbody2D rb;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        HashSet<int> lastTiles = new HashSet<int>();
        HashSet<int> currentTiles = new HashSet<int>();

        var temp = lastTiles;
        lastTiles = currentTiles;
        temp.Clear();
        currentTiles = temp;
        


        rb.velocity = movement;
    }
}
