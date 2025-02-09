using UnityEditor;
using UnityEngine;

public class AssetBundleBuilder
{
    private static string TAG = "AssetBundleBuilder";

    [MenuItem("Assets/Build AssetBundles")]
    static void BuildAllAssetBundles()
    {
        string assetBundleDirectory = "Assets/AssetBundles";

        if (!System.IO.Directory.Exists(assetBundleDirectory)) {
            System.IO.Directory.CreateDirectory(assetBundleDirectory);
        }

        BuildPipeline.BuildAssetBundles(assetBundleDirectory, BuildAssetBundleOptions.None, BuildTarget.WebGL);

        KLog.I(TAG, "BuildAllAssetBundles complete");
    }
}
