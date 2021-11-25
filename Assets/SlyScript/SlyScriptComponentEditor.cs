using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SlyScriptComponent))]
public class SlyScriptComponentEditor : Editor
{
    SerializedProperty m_myScript;

    void OnEnable()
    {
        // Fetch the objects from the MyScript script to display in the inspector
        m_myScript = serializedObject.FindProperty("Script");
    }

    public override void OnInspectorGUI()
    {
        SlyScriptComponent script = (SlyScriptComponent)target;
        if(!script.hasCompiled)
        {
            script.Script.Compile();
            script.hasCompiled = true;
        }
        if(script.Script == null)
        {
            EditorGUILayout.PropertyField(m_myScript);
            EditorGUILayout.LabelField("Please add a sly script here!");
        } else
        {
            EditorGUILayout.PropertyField(m_myScript);
            foreach(SlyVariable var in script.Script.GetVariables())
            {
                EditorGUILayout.LabelField(var.name);
                switch (var.type)
                {
                    case SlyVariable.SlyObjectType.String:
                        var.value = EditorGUILayout.TextField(var.value);
                        break;
                    case SlyVariable.SlyObjectType.Integer:
                        int value = 0;
                        if(var.value.Length > 0)
                        {
                            value = int.Parse(var.value);
                        }
                        var.value = EditorGUILayout.IntField(value) + "";
                        break;
                    case SlyVariable.SlyObjectType.SlyObject:
                        EditorGUILayout.LabelField("Sly objects cant be edited in the inspector (yet), please make it private!");
                        break;
                    case SlyVariable.SlyObjectType.Undefined:
                        EditorGUILayout.LabelField("Undefined variable found in script! Please fix and recompile!!!");
                        break;
                }
            }
        }
        serializedObject.ApplyModifiedProperties();
    }
}
