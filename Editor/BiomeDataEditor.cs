using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

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
            SerializedProperty tempProp = element.FindPropertyRelative("temperature");
            SerializedProperty moistProp = element.FindPropertyRelative("moisture");
            SerializedProperty freqProp = element.FindPropertyRelative("frequency");
            SerializedProperty ampProp = element.FindPropertyRelative("amplitude");
            SerializedProperty lacProp = element.FindPropertyRelative("lacunarity");
            SerializedProperty gainProp = element.FindPropertyRelative("gain");
            SerializedProperty octProp = element.FindPropertyRelative("octaves");
            SerializedProperty texProp = element.FindPropertyRelative("groundTexture");

            rect.y += 2;
            float lineHeight = EditorGUIUtility.singleLineHeight;
            rect.height = lineHeight;

            // Name
            EditorGUI.PropertyField(rect, nameProp);
            rect.y += lineHeight + 2;

            // ID
            EditorGUI.PropertyField(rect, idProp);
            rect.y += lineHeight + 2;

            // Temperature
            EditorGUI.PropertyField(rect, tempProp);
            rect.y += lineHeight + 2;

            // Moisture
            EditorGUI.PropertyField(rect, moistProp);
            rect.y += lineHeight + 2;

            // Frequency
            EditorGUI.PropertyField(rect, freqProp);
            rect.y += lineHeight + 2;

            // Amplitude
            EditorGUI.PropertyField(rect, ampProp);
            rect.y += lineHeight + 2;

            // Lacunarity
            EditorGUI.PropertyField(rect, lacProp);
            rect.y += lineHeight + 2;

            // Gain
            EditorGUI.PropertyField(rect, gainProp);
            rect.y += lineHeight + 2;

            // Octaves
            EditorGUI.PropertyField(rect, octProp);
            rect.y += lineHeight + 2;

            // Ground Texture
            EditorGUI.PropertyField(rect, texProp);
        };

        reorderableList.elementHeightCallback = (int index) =>
        {
            return 10 * (EditorGUIUtility.singleLineHeight + 2) + 4;
        };

        reorderableList.onAddCallback = (ReorderableList list) =>
        {
            int index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;
            SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("name").stringValue = "New Biome";
            element.FindPropertyRelative("id").intValue = index;
            element.FindPropertyRelative("temperature").enumValueIndex = 0;
            element.FindPropertyRelative("moisture").enumValueIndex = 0;
            element.FindPropertyRelative("frequency").floatValue = 0.01f;
            element.FindPropertyRelative("amplitude").floatValue = 1f;
            element.FindPropertyRelative("lacunarity").floatValue = 2f;
            element.FindPropertyRelative("gain").floatValue = 0.5f;
            element.FindPropertyRelative("octaves").intValue = 4;
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        reorderableList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }
}