using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlayerController))]
public class PlayerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        PlayerController pController = (PlayerController)target;
        base.OnInspectorGUI();

        if (GUILayout.Button("Die"))
        {
            pController.Die();
        }
    }
}
