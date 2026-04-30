using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(InstrumentInteractable))]
public class InstrumentInteractableEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        InstrumentInteractable instrument = (InstrumentInteractable)target;

        if (GUILayout.Button("¡¢º¥≤‚ ‘¥•∑¢"))
        {
            instrument.TestTriggerInInspector();
        }
    }

}