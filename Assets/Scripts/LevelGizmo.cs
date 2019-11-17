using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class LevelGizmo : MonoBehaviour
{
    public Color outlineColor;
    public Vector2 levelSize;
    public Vector2Int amountOfLevels;
    private int iterations = 3;

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
}
