using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Unity.VisualScripting;

[CustomEditor(typeof(BiomeData))]
public class BiomeDataEditor : Editor
{
    private ReorderableList reorderableList;

    private void OnEnable()
    {
        reorderableList = new ReorderableList(serializedObject, serializedObject.FindProperty("biomes"), true, true, true, true);

        reorderableList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Biomes");
        };

        reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            SerializedProperty element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
            SerializedProperty nameProp = element.FindPropertyRelative("name");
            SerializedProperty idProp = element.FindPropertyRelative("id");
            SerializedProperty elProp = element.FindPropertyRelative("elevation");
            SerializedProperty moistProp = element.FindPropertyRelative("moisture");
            SerializedProperty contProp = element.FindPropertyRelative("continentaless");
            SerializedProperty colorProp = element.FindPropertyRelative("color");
            SerializedProperty treeProp = element.FindPropertyRelative("tree");
            SerializedProperty textureProp = element.FindPropertyRelative("texture");

            rect.y += 2;
            float lineHeight = EditorGUIUtility.singleLineHeight;
            rect.height = lineHeight;

            // Name
            EditorGUI.PropertyField(rect, nameProp);
            rect.y += lineHeight + 2;

            // ID
            EditorGUI.PropertyField(rect, idProp);
            rect.y += lineHeight + 2;

            // Elevation
            EditorGUI.PropertyField(rect, elProp);
            rect.y += lineHeight + 2;

            // Moisture
            EditorGUI.PropertyField(rect, moistProp);
            rect.y += lineHeight + 2;

            // Continentaless
            EditorGUI.PropertyField(rect, contProp);
            rect.y += lineHeight + 2;

            EditorGUI.PropertyField(rect, colorProp);
            rect.y += lineHeight + 2;

            // Tree
            EditorGUI.PropertyField(rect, treeProp);
            rect.y += lineHeight + 2;

            // Txture
            EditorGUI.PropertyField(rect, textureProp);
            rect.y += lineHeight + 2;
        };

        reorderableList.elementHeightCallback = (int index) =>
        {
            return 8 * (EditorGUIUtility.singleLineHeight + 2) + 6;
        };

        reorderableList.onAddCallback = (ReorderableList list) =>
        {
            int index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;
            SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("name").stringValue = "New Biome";
            element.FindPropertyRelative("id").intValue = index;
            element.FindPropertyRelative("elevation").enumValueIndex = 0;
            element.FindPropertyRelative("moisture").enumValueIndex = 0;
            element.FindPropertyRelative("continentaless").enumValueIndex = 0;
            element.FindPropertyRelative("color").colorValue = new Color(0,0,0);
            element.FindPropertyRelative("tree").objectReferenceValue = null;
            element.FindPropertyRelative("texture").objectReferenceValue = null;
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        reorderableList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }
}