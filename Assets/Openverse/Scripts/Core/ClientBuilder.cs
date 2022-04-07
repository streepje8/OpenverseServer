using System.Collections;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientBuilder : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Openverse/Build Client")]
    static void BuildAllAssetBundles()
    {
#if UNITY_EDITOR
        if (!SceneManager.GetSceneByPath(AssetDatabase.GetAssetPathsFromAssetBundle("clientscene")[0]).IsValid())
        {
            EditorSceneManager.CloseScene(EditorSceneManager.GetActiveScene(), true);
            EditorSceneManager.OpenScene(AssetDatabase.GetAssetPathsFromAssetBundle("clientscene")[0]);
        }
#endif
        foreach (GameObject go in SceneManager.GetSceneByPath(AssetDatabase.GetAssetPathsFromAssetBundle("clientscene")[0]).GetRootGameObjects())
        {
            for (int i = 0; i < 10; i++)
            {
                AllowedComponents.ScanAndRemoveInvalidScripts(go);
            }
        }
        string assetBundleDirectory = "Assets/OpenverseBuilds";
        if (!Directory.Exists(assetBundleDirectory))
        {
            Directory.CreateDirectory(assetBundleDirectory);
        }
        BuildPipeline.BuildAssetBundles(assetBundleDirectory,
                                        BuildAssetBundleOptions.ForceRebuildAssetBundle,
                                        BuildTarget.StandaloneWindows);
#if UNITY_EDITOR
        EditorSceneManager.CloseScene(EditorSceneManager.GetActiveScene(), true);
        EditorSceneManager.OpenScene(AssetDatabase.GetAssetPathsFromAssetBundle("serverscene")[0]);
#endif
    }
#endif
}
