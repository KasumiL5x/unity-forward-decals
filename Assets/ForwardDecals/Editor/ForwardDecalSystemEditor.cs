using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ForwardDecalSystem))]
public class ForwardDecalSystemEditor : Editor {
  public override void OnInspectorGUI() {
    var sys = (ForwardDecalSystem)target;

    EditorGUILayout.PropertyField(serializedObject.FindProperty("printDebug"));

    // This class doesn't support being edited while in play mode due to command buffers being assigned on rebuild.
    GUI.enabled = !Application.isPlaying;

    EditorGUILayout.PropertyField(serializedObject.FindProperty("allCameras"), new GUIContent("Decal Cameras"), true);

    var decalList = serializedObject.FindProperty("allDecals");
    EditorGUILayout.PropertyField(decalList, new GUIContent("Initial Decals"), true);

    EditorGUILayout.PropertyField(serializedObject.FindProperty("updateStaticInEditor"));

    GUILayout.Space(15.0f);
    if(GUILayout.Button("Force Rebuild", GUILayout.Height(30.0f))) {
      sys.Rebuild();
    }

    if(GUILayout.Button("Reassign Buffers", GUILayout.Height(30.0f))) {
      sys.ReassignBuffers();
    }

    GUI.enabled = true;

    serializedObject.ApplyModifiedProperties();
  }
}
