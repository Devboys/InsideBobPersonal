using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelGizmo : MonoBehaviour
{
    public Color outlineColor = Color.white;
    public Vector2 levelSize = new Vector2(50, 28);
    public Vector2Int amountOfLevels = new Vector2Int(10, 10);

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
#endif
}
