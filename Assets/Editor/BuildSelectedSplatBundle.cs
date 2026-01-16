using UnityEditor;
using UnityEngine;
using System.IO;

public class BuildSelectedSplatBundle
{
    [MenuItem("Tools/Splats/Build Bundle From Selected Splat Asset")]
    public static void BuildBundleFromSelected()
    {
        var selected = Selection.activeObject;
        if (selected == null)
        {
            Debug.LogError("[SPLAT BUNDLE] No asset selected. Select a GaussianSplatAsset in the Project window.");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(selected);
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogError("[SPLAT BUNDLE] Could not get asset path.");
            return;
        }

        string baseName   = Path.GetFileNameWithoutExtension(assetPath);
        string bundleName = baseName.ToLower() + "_splat";

        string outputPath = "Assets/AssetBundles";
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        AssetBundleBuild build = new AssetBundleBuild
        {
            assetBundleName = bundleName,
            assetNames      = new[] { assetPath }
        };

        BuildPipeline.BuildAssetBundles(
            outputPath,
            new[] { build },
            BuildAssetBundleOptions.None,
            BuildTarget.StandaloneWindows64
        );

        Debug.Log($"[SPLAT BUNDLE] Built bundle '{bundleName}' with asset '{assetPath}'");
    }
}
