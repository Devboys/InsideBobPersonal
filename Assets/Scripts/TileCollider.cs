using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileCollider : MonoBehaviour
{
    public Tilemap tilemap;

    public Tilemap[] spikeTilemaps;

    public LevelController levelController;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var tilemap = collision.gameObject.GetComponent<Tilemap>();
        if (tilemap) {
            foreach (Tilemap map in spikeTilemaps) {
                if (map == tilemap) GetComponent<PlayerController>().Die();
            }
        }

        var handler = collision.gameObject.GetComponent<PowerUpHandler>();
        if (handler) {
            RemoveSpikes(handler.tilemap);
            Destroy(collision.gameObject);
        }
    }

    private void RemoveSpikes(Tilemap map)
    {
        Vector2Int levelIndex = levelController.levelIndex;
        Vector2 levelSize = levelController.levelSize;
        float xInit = -levelSize.x / 2;
        float yInit = -levelSize.y / 2;

        for (float i = xInit + levelIndex.x * levelSize.x; i < xInit + levelIndex.x * levelSize.x + levelSize.x; i += map.cellSize.x)
        {
            for (float j = yInit + levelIndex.y * levelSize.y; j < yInit + levelIndex.y * levelSize.y + levelSize.y; j += map.cellSize.y)
            {
                var pos = map.WorldToCell(new Vector3(i, j, transform.position.z));
                map.SetTile(pos, null);
            }
        }
    }
}