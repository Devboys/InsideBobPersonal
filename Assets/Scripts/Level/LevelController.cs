using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;
using FMODUnity;
using FMOD.Studio;

public class LevelController : MonoBehaviour
{
    public AnimationCurve screenTransition;
    public Vector2 levelSize;
    public TileBase diseaseTile;
    public TileBase bacteriaTile;
    // Remover Stuff
    public GameObject remover;
    public float removerMaxSpeed;
    [Range(0, 1)]
    public float speedVariance = 0.7f;

    private PlayerController player;
    private Camera mainCam;

    [HideInInspector]
    public Vector2Int levelIndex = Vector2Int.zero;

    private Tilemap currentLevel;
    
    [SerializeField]
    private List<Tilemap> allLevels = new List<Tilemap>();

    private int pillsForCurrentLevel;

    public event Action onLevelChangeEvent;
    
    [Header("-- FMOD Event")]
    [Space(20)]
    [EventRef]
    public string germDestroy;
   

    private void Awake()
    {
        player = FindObjectOfType<PlayerController>();
        mainCam = Camera.main;
        allLevels = FindObjectsOfType<Tilemap>().ToList();
        foreach (Tilemap tilemap in allLevels)
        {
            tilemap.CompressBounds();
        }
        levelIndex = GetCurrentPlayerLevelIndex();
        currentLevel = FindCurrentLevel();
        pillsForCurrentLevel = PillCountInCurrentLevel();
        StartCoroutine(TransitionCamera());
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (player)
        {
            if (player.transform.position.x > levelSize.x / 2 + levelIndex.x * levelSize.x)
                MoveToLevel(Vector2Int.right);
            else if (player.transform.position.x < -levelSize.x / 2 + levelIndex.x * levelSize.x)
                MoveToLevel(Vector2Int.left);
            else if (player.transform.position.y > levelSize.y / 2 + levelIndex.y * levelSize.y)
                MoveToLevel(Vector2Int.up);
            else if (player.transform.position.y < -levelSize.y / 2 + levelIndex.y * levelSize.y)
                MoveToLevel(Vector2Int.down);
        }
    }

    private void MoveToLevel(Vector2Int dir)
    {
        StopAllCoroutines();
        levelIndex += dir;
        currentLevel = FindCurrentLevel();
        pillsForCurrentLevel = PillCountInCurrentLevel();
        for (int i = player.padList.Count - 1; i >= 0; i--)
        {
            Destroy(player.padList[i]);
            player.padList.RemoveAt(i);
        }
        player.numPadsAllowed = 0;
        StartCoroutine(TransitionCamera());

        onLevelChangeEvent?.Invoke();
    }

    private IEnumerator TransitionCamera()
    {
        float curTime = 0;
        float endTime = screenTransition.keys[screenTransition.length - 1].time;
        Vector3 startPos = mainCam.transform.position;
        Vector3 endPos = new Vector3(levelIndex.x * levelSize.x, levelIndex.y * levelSize.y, -10);
        while (curTime < endTime)
        {
            mainCam.transform.position = Vector3.LerpUnclamped(startPos, endPos, screenTransition.Evaluate(curTime));
            yield return null;
            curTime += Time.deltaTime;
        }
        mainCam.transform.position = endPos;
    }

    private Vector2Int GetCurrentPlayerLevelIndex()
    {
        int x = (int)((player.transform.position.x - levelSize.x / 2) / levelSize.x);
        int y = (int)((player.transform.position.y - levelSize.y / 2) / levelSize.y);
        return new Vector2Int(x, y);
    }

    private Tilemap FindCurrentLevel()
    {
        Vector2 topRight = levelSize / 2 + levelIndex * levelSize;
        Vector2 bottomLeft = topRight - levelSize;
        foreach (Tilemap level in allLevels)
        {
            if (level.transform.position.x < topRight.x &&
                level.transform.position.x > bottomLeft.x &&
                level.transform.position.y < topRight.y &&
                level.transform.position.y > bottomLeft.y)
            {
                return level;
            }
        }

        return null;
    }

    private int PillCountInCurrentLevel()
    {
        if (currentLevel == null)
            return 0;

        var activeChildCount = 0;
        foreach(Transform child in currentLevel.transform.Find("Pills"))
        {
            if (child.gameObject.activeSelf) activeChildCount++;
        }

        return activeChildCount;
    }

    public void PillTaken()
    {
        pillsForCurrentLevel--;
        CheckOpen();
    } 

    public void CheckOpen()
    {
        if (pillsForCurrentLevel > 0)
            return;
        if (currentLevel.ContainsTile(diseaseTile))
            return;

        List<Vector3Int> positions = new List<Vector3Int>();
        BoundsInt bounds = currentLevel.cellBounds;
        TileBase[] allTiles = currentLevel.GetTilesBlock(bounds);

        for (int x = 0; x < bounds.size.x; x++) 
        {
            for (int y = 0; y < bounds.size.y; y++)
            {
                TileBase tile = allTiles[x + y * bounds.size.x];
                if (tile == bacteriaTile)
                {
                    positions.Add(new Vector3Int(x, y, 0) + bounds.position);
                }
            }
        }

        //currentLevel.SetTiles(positions.ToArray(), new TileBase[positions.Count]);
        SpawnRemovers(currentLevel, positions);
    }

    private void SpawnRemovers(Tilemap tilemap, List<Vector3Int> positions)
    {
        foreach (Vector3Int pos in positions)
        {
            var obj = Instantiate(remover);
            obj.transform.position = player.transform.position;
            var rc = obj.GetComponent<RemoverController>();
            rc.pos = pos;
            rc.tilemap = tilemap;
            rc.speed = removerMaxSpeed * Random.Range(1.0f - speedVariance, 1.0f);
        }
        
        RuntimeManager.PlayOneShot(germDestroy, transform.position); // Play germ destroy sound to unlock level
    }

    public void ForceUpdatePillCountForCurrentLevel()
    {
        pillsForCurrentLevel = PillCountInCurrentLevel();
    }

}
