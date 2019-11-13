using UnityEditor;

[CustomEditor(typeof(PlayerController))]
public class PlayerControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        PlayerController pc = (PlayerController)target;

        //Example custom calculated field
        EditorGUILayout.LabelField("ExampleField ", (pc.maxGravity + pc.maxGravity).ToString());
    }
}
