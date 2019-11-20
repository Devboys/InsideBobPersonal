using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelGizmo : MonoBehaviour
{
    public Color outlineColor = Color.white;
    public Vector2 levelSize = new Vector2(50, 28);
    public Vector2Int amountOfLevels = new Vector2Int(10, 10);
    public TileBase oldTile;
    public TileBase newTile;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (IsMeOrChildRecursively(transform) && levelSize != Vector2.zero)
        {
            Gizmos.color = outlineColor;
            for (int x = -amountOfLevels.x; x < amountOfLevels.x; x++)
            {
                for (int y = -amountOfLevels.y; y < amountOfLevels.y; y++)
                {
                    Gizmos.DrawWireCube(new Vector3(x * levelSize.x, y * levelSize.y, 0), levelSize);
                }
            }
        }
    }

    private bool IsMeOrChildRecursively(Transform t)
    {
        if (Selection.activeTransform == t)
        {
            return true;
        }
        else
        {
            foreach (Transform child in t)
            {
                if (IsMeOrChildRecursively(child))
                {
                    return true;
                }
            }
        }
        return false;
    }

    [ContextMenu("Replace tiles")]
    public void ReplaceTile()
    {
        if (!oldTile)
            return;
        if (!newTile)
            return;

        foreach (Transform child in transform)
        {
            Tilemap t = child.GetComponent<Tilemap>();
            if (t)
            {
                for (int x = t.cellBounds.xMin; x < t.cellBounds.size.x; x++)
                {
                    for (int y = t.cellBounds.yMin; y < t.cellBounds.size.y; y++)
                    {
                        if (t.GetTile(new Vector3Int(x, y, 0)) == oldTile)
                            t.SetTile(new Vector3Int(x, y, 0), newTile);
                    }
                }
            }
        }
    }
#endif
}
