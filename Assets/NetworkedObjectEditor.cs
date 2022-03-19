#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NetworkedObject))]
public class NetworkedObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GUILayout.Label("Watched variables:", EditorStyles.boldLabel);
        foreach(WatchableProperty wap in ((NetworkedObject)target).watchableProperties)
        {
            wap.isWatched = EditorGUILayout.Toggle(wap.component.Name + "/" + wap.property.Name, wap.isWatched);
        }
    }
}
#endif