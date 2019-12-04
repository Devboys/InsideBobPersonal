using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileCollider : MonoBehaviour
{
    public Tilemap tilemap;

    public Tilemap[] spikeTilemaps;

    public LevelController levelController;

    public GameObject remover;
    public float removerMaxSpeed;
    [Tooltip("The variance in speed by percentage. Range: 0 -> 1")]
    [Range(0, 1)]
    public float speedVariance = 0.5f;

    private PlayerController playerController;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var tilemap = collision.gameObject.GetComponent<Tilemap>();
        if (tilemap) {
            foreach (Tilemap map in spikeTilemaps) {
                if (map == tilemap) playerController.TakeDamage(playerController.spikeDamageInitial); //GetComponent<PlayerController>().Die();
            }
        }

        var handler = collision.gameObject.GetComponent<PowerUpHandler>();
        if (handler) {
            RemoveSpikes(handler.tilemap);
            //Destroy(collision.gameObject);
            collision.gameObject.SetActive(false);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        var tilemap = collision.gameObject.GetComponent<Tilemap>();
        if (tilemap)
        {
            foreach (Tilemap map in spikeTilemaps)
            {
                if (map == tilemap) playerController.TakeDamage(playerController.spikeDamageStay * Time.fixedDeltaTime); //GetComponent<PlayerController>().Die();
            }
        }
    }

    private void RemoveSpikes(Tilemap map)
    {
        float hit = Time.time;

        Vector2Int levelIndex = levelController.levelIndex;
        Vector2 levelSize = levelController.levelSize;
        float xInit = -levelSize.x / 2;
        float yInit = -levelSize.y / 2;

        List<Vector3Int> positions = new List<Vector3Int>();
        for (float i = xInit + levelIndex.x * levelSize.x; i < xInit + levelIndex.x * levelSize.x + levelSize.x; i++)
        {
            for (float j = yInit + levelIndex.y * levelSize.y; j < yInit + levelIndex.y * levelSize.y + levelSize.y; j++)
            {
                var pos = map.WorldToCell(new Vector3(i, j, transform.position.z));
                if (map.GetTile(pos) != null) positions.Add(pos);//map.SetTile(pos, null);
            }
        }

        SpawnRemovers(map, positions);

        //map.SetTiles(positions.ToArray(), new TileBase[positions.Count]);
    }

    private void SpawnRemovers(Tilemap tilemap, List<Vector3Int> positions) {
        foreach (Vector3Int pos in positions) {
            var obj = Instantiate(remover);
            obj.transform.position = transform.position;
            var rc = obj.GetComponent<RemoverController>();
            rc.pos = pos;
            rc.tilemap = tilemap;
            rc.speed = removerMaxSpeed * Random.Range(1.0f - speedVariance, 1.0f);
        }
    }


}