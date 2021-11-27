using System;
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
        SlyManager.registerScriptComponent((SlyScriptComponent)target);
    }

    public override void OnInspectorGUI()
    {
        SlyScriptComponent script = (SlyScriptComponent)target;
        if(script.Script == null)
        {
            EditorGUILayout.PropertyField(m_myScript);
            EditorGUILayout.LabelField("Please add a sly script here!");
        } else
        {
            EditorGUILayout.PropertyField(m_myScript);
            if (!script.hasCompiled)
            {
                script.Script.Compile();
                script.hasCompiled = true;
            }
            if (script.Script.compiledClass.variables == null)
            {
                script.Script.Compile();
            }
            if (script.instance == null)
            {
                script.instance = new SlyInstance(script.Script.compiledClass);
            }
            if(script.instance.type.name.Equals("Undefined", StringComparison.OrdinalIgnoreCase))
            {
                script.instance = null;
            }
            if(Application.isPlaying)
            {
                EditorGUILayout.LabelField("In playmode!");
                drawInspectorFor(script.runtimeInstance);
                
            } else
            {
                drawInspectorFor(script.instance);
            }
        }
        serializedObject.ApplyModifiedProperties();
    }

    public void drawInspectorFor(SlyInstance instance)
    {
        if(instance != null) { 
            if (instance.variables != null)
            {
                foreach (SlyVariable var in instance.variables)
                {
                    EditorGUILayout.LabelField(var.name);
                    switch (var.type)
                    {
                        case SlyObjectType.TypeString:
                            var.value = EditorGUILayout.TextField(var.value);
                            break;
                        case SlyObjectType.Typeint:
                            int value = 0;
                            if (var.value.Length > 0)
                            {
                                value = int.Parse(var.value);
                            }
                            var.value = EditorGUILayout.IntField(value) + "";
                            break;
                        case SlyObjectType.TypeSlyObject:
                            EditorGUILayout.LabelField("Sly objects cant be edited in the inspector (yet), please make it private!");
                            break;
                        case SlyObjectType.TypeUndefined:
                            EditorGUILayout.LabelField("Undefined variable found in script! Please fix and recompile!!!");
                            break;
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("Please fix the compile errors on this script first!!");
            }
        } else
        {
            EditorGUILayout.LabelField("Compiling....");
        }
    }
}
