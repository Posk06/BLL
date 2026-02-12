using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Unity.VisualScripting;

[CustomEditor(typeof(ProcedualGenerator))]
public class TerraintestEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        ProcedualGenerator generator = (ProcedualGenerator)target;

        if (GUILayout.Button("Generate Test Biome"))
        {
            Debug.Log("TODO: Generate Test Biome");
        }
    }
}