using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ClientBuilder : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Openverse/Build Client")]
    static void BuildAllAssetBundles()
    {
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
