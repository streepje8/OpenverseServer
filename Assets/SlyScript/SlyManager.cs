using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SlyManager : MonoBehaviour
{
    private static List<SlyScriptComponent> scriptComponents = new List<SlyScriptComponent>();

    public static void recompileAll()
    {
        foreach(SlyScriptComponent ssc in scriptComponents)
        {
            if(ssc.Script != null) {
                ssc.Script.Compile();
                if (ssc.instance == null)
                {
                    ssc.instance = new SlyInstance(ssc.Script.compiledClass);
                }
                ssc.instance.recompile(ssc.Script.compiledClass);
            } 
        }
    }

    public static void recompileAllExceptSelf(SlyScript self)
    {
        List<Scene> scenes = new List<Scene>();
        for(int i = 0; i < SceneManager.sceneCountInBuildSettings; i++) {
            scenes.Add(SceneManager.GetSceneByBuildIndex(i));
        }
        foreach(Scene scene in scenes)
        {
            GameObject[] rootObjectsInScene = scene.GetRootGameObjects();
            for (int i = 0; i < rootObjectsInScene.Length; i++)
            {
                SlyScriptComponent[] allComponents = rootObjectsInScene[i].GetComponentsInChildren<SlyScriptComponent>(true);
                for (int j = 0; j < allComponents.Length; j++)
                {
                    SlyScriptComponent ssc = allComponents[j];
                    if (ssc.Script != null)
                    {
                        if (ssc.Script != self)
                        {
                            ssc.Script.Compile();
                        }
                        if (ssc.instance == null)
                        {
                            ssc.instance = new SlyInstance(ssc.Script.compiledClass);
                        }
                        ssc.instance.recompile(ssc.Script.compiledClass);
                    }
                }
            }
        }
            
    }

    public static void registerScriptComponent(SlyScriptComponent ssc)
    {
        if (!scriptComponents.Contains(ssc))
        {
            scriptComponents.Add(ssc);
        }
    }
    public static void deregisterScriptComponent(SlyScriptComponent target)
    {
        if(scriptComponents.Contains(target))
        {
            scriptComponents.Remove(target);
        }
    }
}
