using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileCollider : MonoBehaviour
{
    public HashSet<TileBase> collidingTiles = new HashSet<TileBase>();
    public HashSet<TileBase> lastCollidingTiles = new HashSet<TileBase>();

    public Tilemap tilemap;

    public LevelController levelController;

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (tilemap)
        {
            foreach (ContactPoint2D contactPoint in collision.contacts)
            {
                Vector3 hitPosition = new Vector3(contactPoint.point.x - 0.01f * contactPoint.normal.x, contactPoint.point.y - 0.01f * contactPoint.normal.y, 0);
                var pos = tilemap.WorldToCell(hitPosition);
                TileBase t = tilemap.GetTile(pos);
                
                if (t)
                {
                    collidingTiles.Add(t);
                    if (!lastCollidingTiles.Contains(t))
                    {
                        lastCollidingTiles.Add(t);
                        Debug.Log(t.name + " " + tilemap.WorldToCell(hitPosition));
                        OnTileCollisionEnter(pos, t);
                    }
                    else
                    {
                        OnTileCollisoinStay(t.name);
                    }
                }
            }
        }

        CheckTileCollisoinExit();

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
            CheckTileCollisoinExit();
        }
    }

    private void OnTileCollisionEnter(Vector3Int pos, TileBase tile)
    {
        switch (tile.name)
        {
            case "Spike":
                Debug.Log("Hit Spike");
                break;
            case "PowerUP":
                Debug.Log("Hit PowerUP");
                tilemap.SetTile(pos, null);
                RemoveSpikes("Spike");
                break;
            case "Ground":
            default:
                break;
        }
    }

    private void RemoveSpikes(string name) {
        Vector2Int levelIndex = levelController.levelIndex;
        Vector2 levelSize = levelController.levelSize;
        float xInit = -levelSize.x / 2;
        float yInit = -levelSize.y / 2;

        for (float i = xInit + levelIndex.x * levelSize.x; i < xInit + levelIndex.x * levelSize.x + levelSize.x; i += tilemap.cellSize.x) {
            for (float j = yInit + levelIndex.y * levelSize.y; j < yInit + levelIndex.y * levelSize.y + levelSize.y; j += tilemap.cellSize.y) {
                var pos = tilemap.WorldToCell(new Vector3(i, j, transform.position.z));
                var tile = tilemap.GetTile(pos);
                if (tile && tile.name == name) {
                    tilemap.SetTile(pos, null);
                }
            }
        }
    }

    private void OnTileCollisoinStay(string name)
    {

    }

    private void OnTileCollisionExit(string name)
    {

    }

    private void CheckTileCollisoinExit()
    {
        foreach (TileBase tile in lastCollidingTiles)
        {
            if (!collidingTiles.Contains(tile))
            {
                OnTileCollisionExit(tile.name);
            }
        }
    }

}
