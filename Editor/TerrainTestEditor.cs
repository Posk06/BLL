using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Unity.VisualScripting;

[CustomEditor(typeof(ChunkLoader))]
public class TerraintestEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        ChunkLoader loader = (ChunkLoader)target;

        if (GUILayout.Button("Regenerate Terrain"))
        {
            {
                var method = loader.GetType().GetMethod("regenerateTerrain", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (method != null)
                {
                    method.Invoke(loader, null);
                }
                else
                {
                    Debug.LogError("Could not find regenerateTerrain method on ChunkLoader");
                }
            }
        }
    }
}