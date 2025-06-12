using ExportCharacterTool;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LocalContext))]
public sealed class LocalContextEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw default inspector fields (for all serialized fields)
        DrawDefaultInspector();

        // Cast target to LocalContext
        LocalContext context = (LocalContext)target;

        // Add space
        EditorGUILayout.Space();

        // Draw Save button
        if (GUILayout.Button("Save"))
        {
            context.Save();
            Debug.Log("LocalContext saved successfully.");
        }

        // Draw Load button
        if (GUILayout.Button("Load"))
        {
            context.Load();
            Debug.Log("LocalContext loaded successfully.");
        }
    }
}