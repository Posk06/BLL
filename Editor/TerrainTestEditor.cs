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
            {
                var method = generator.GetType().GetMethod("generateTerrain", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (method != null)
                {
                    method.Invoke(generator, null);
                    EditorUtility.SetDirty(generator);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
                    UnityEditor.SceneView.RepaintAll();
                }
                else
                {
                    Debug.LogError("Could not find generateTerrain method on ProcedualGenerator");
                }
            }
        }
    }
}