using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileCollider : MonoBehaviour
{
    public HashSet<TileBase> collidingTiles = new HashSet<TileBase>();
    public HashSet<TileBase> lastCollidingTiles = new HashSet<TileBase>();

    private void OnCollisionStay2D(Collision2D collision)
    {
        Tilemap tilemap = collision.collider.GetComponent<Tilemap>();
        if (tilemap)
        {
            foreach (ContactPoint2D contactPoint in collision.contacts)
            {
                Vector3 hitPosition = new Vector3(contactPoint.point.x - 0.01f * contactPoint.normal.x, contactPoint.point.y - 0.01f * contactPoint.normal.y, 0);
                TileBase t = tilemap.GetTile(tilemap.WorldToCell(hitPosition));

                if (t)
                {
                    collidingTiles.Add(t);

                    if (!lastCollidingTiles.Contains(t))
                    {
                        lastCollidingTiles.Add(t);
                        OnTileCollisionEnter(t.name);
                    }
                    else
                    {
                        OnTileCollisoinStay(t.name);
                    }
                }
            }
        }

        foreach (TileBase tile in lastCollidingTiles)
        {
            if (!collidingTiles.Contains(tile))
            {
                OnTileCollisionExit(tile.name);
            }
        }

        var temp = lastCollidingTiles;
        lastCollidingTiles = collidingTiles;
        temp.Clear();
        collidingTiles = temp;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        Tilemap tilemap = collision.collider.GetComponent<Tilemap>();
        if (tilemap)
        {
            foreach (TileBase tile in lastCollidingTiles)
            {
                OnTileCollisionExit(tile.name);
            }
            lastCollidingTiles.Clear();
        }
    }

    private void OnTileCollisionEnter(string name)
    {
        Debug.Log("Entered collision with " + name);
        switch (name)
        {
            case "Spike":

                break;
            case "Ground":
            default:
                break;
        }
    }

    private void OnTileCollisoinStay(string name)
    {

    }

    private void OnTileCollisionExit(string name)
    {
        Debug.Log("Exited collision with " + name);
    }
}
