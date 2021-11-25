using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class SlyScriptMenus : MonoBehaviour
{
    [MenuItem("SlyScript/Create Script")]
    static void LogSelectedTransformName()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (path == "")
        {
            path = "Assets";
        }
        else if (Path.GetExtension(path) != "")
        {
            path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
        }
        AssetDatabase.CreateAsset(new SlyScript(), AssetDatabase.GenerateUniqueAssetPath(path + "/NewSlyScript.asset"));
    }
}
