using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    private SerializedProperty _moduleLibrary;
    private SerializedProperty _maxModules;
    private SerializedProperty _useRandomSeed;
    private SerializedProperty _seed;
    private SerializedProperty _currentSeed;
    private SerializedProperty _attemptCount;
    private SerializedProperty _iterationCount;

    private void OnEnable()
    {
        _moduleLibrary = serializedObject.FindProperty("_moduleLibrary");
        _maxModules = serializedObject.FindProperty("_maxModules");
        _useRandomSeed = serializedObject.FindProperty("_useRandomSeed");
        _seed = serializedObject.FindProperty("_seed");
        _currentSeed = serializedObject.FindProperty("_currentSeed");
        _attemptCount = serializedObject.FindProperty("_attemptCount");
        _iterationCount = serializedObject.FindProperty("_iterationCount");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_moduleLibrary);
        EditorGUILayout.PropertyField(_maxModules);
        EditorGUILayout.PropertyField(_useRandomSeed);

        if (!_useRandomSeed.boolValue)
        {
            EditorGUILayout.PropertyField(_seed);
        }

        EditorGUILayout.PropertyField(_currentSeed);
        GUI.enabled = false;
        EditorGUILayout.PropertyField(_attemptCount);
        EditorGUILayout.PropertyField(_iterationCount);
        GUI.enabled = true;

        GUILayout.Space(10);

        if (GUILayout.Button("Generate Map"))
            ((MapGenerator)target).StartMapGeneration();

        serializedObject.ApplyModifiedProperties();
    }
}