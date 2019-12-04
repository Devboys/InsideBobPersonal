using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RemoverController : MonoBehaviour
{
    //[HideInInspector]
    public Tilemap tilemap;
    //[HideInInspector]
    public Vector3Int pos;
    //[HideInInspector]
    public float speed;
    //[HideInInspector]
    public Vector3 endPos;

    [HideInInspector]
    public RemoverInfo info;

    private Rigidbody2D rb;
    private float lastDist;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (info.tilemap == null)
        {
            endPos = tilemap.CellToWorld(pos) + tilemap.cellSize / 2; // We add half a cell size to center the pos
            var dir = (endPos - transform.position).normalized;
            rb.velocity = dir * speed;
        }
        else
        {
            tilemap = info.tilemap;
            pos = info.pos;
            transform.position = info.startPos;
            endPos = tilemap.CellToWorld(info.pos) + tilemap.cellSize / 2;
            rb.velocity = info.velocity;
        }
        lastDist = Vector2.Distance(endPos, transform.position);
    }

    // Update is called once per frame
    void Update()
    {
       var dist = Vector2.Distance(endPos, transform.position);
       if (dist > lastDist) DestroyBacteria();
       lastDist = dist;
    }

    private void DestroyBacteria()
    {
        tilemap.SetTile(pos, null);
        Destroy(gameObject);
    }
}
