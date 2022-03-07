using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientBuilder : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Openverse/Build Client")]
    static void BuildAllAssetBundles()
    {
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
                                        BuildAssetBundleOptions.None,
                                        BuildTarget.StandaloneWindows);
    }
#endif
}
