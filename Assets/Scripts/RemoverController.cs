using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using FMODUnity;
using FMOD.Studio;

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
    
    
    [Space(20)]
    [EventRef]
    public string germRemove;

    private Rigidbody2D rb;
    private ParticleSystem ps;
    private float lastDist;

    bool bacteriaIsDestroyed; // To prevent DestroyBacteria method from spamming FMOD with requests (hack)

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ps = GetComponent<ParticleSystem>();
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

        bacteriaIsDestroyed = false;
    }

    // Update is called once per frame
    void Update()
    {
       var dist = Vector2.Distance(endPos, transform.position);
       if (dist > lastDist && !bacteriaIsDestroyed) DestroyBacteria();
       lastDist = dist;
       if (!ps.IsAlive())
       {
           Destroy(gameObject);
       }
    }

    private void DestroyBacteria()
    {
        bacteriaIsDestroyed = true;
        RuntimeManager.PlayOneShot(germRemove, transform.position); // Play destroy germs sound
        
        tilemap.SetTile(pos, null);
        ps.Stop();
        
        
    }
}
