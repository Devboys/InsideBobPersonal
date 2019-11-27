using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MoveToViewEditor : EditorWindow
{
    [MenuItem("GameObject/Move To View 2D")]
    static void MoveToView2D(MenuCommand command)
    {
        GameObject curSelection = Selection.activeGameObject;
        if (curSelection)
        {
            Vector3 newPos = SceneView.lastActiveSceneView.camera.transform.position;
            newPos.z = 0;
            curSelection.transform.position = newPos;
        }
    }
}
