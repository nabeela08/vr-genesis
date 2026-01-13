using UnityEditor;
using UnityEngine;

public class BuildSelectedSplatBundle
{
    [MenuItem("Tools/Splats/Build Bundle From Selected Splat Asset")]
    public static void BuildBundleFromSelected()
    {
        var selected = Selection.activeObject;
        if (selected == null)
        {
            Debug.LogError("[SPLAT BUNDLE] No asset selected. Select a Gaussian Splat Asset in the Project window.");
            return;
        }

        // Get path to the selected asset
        string assetPath = AssetDatabase.GetAssetPath(selected);
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogError("[SPLAT BUNDLE] Could not get asset path.");
            return;
        }

        string bundleName = "neptune_splat";

        string outputPath = "Assets/AssetBundles";
        if (!System.IO.Directory.Exists(outputPath))
        {
            System.IO.Directory.CreateDirectory(outputPath);
        }

        AssetBundleBuild build = new AssetBundleBuild
        {
            assetBundleName = bundleName,
            assetNames = new[] { assetPath }
        };

        // Build for Windows for now (editor + Quest link testing)
        BuildPipeline.BuildAssetBundles(
            outputPath,
            new[] { build },
            BuildAssetBundleOptions.None,
            BuildTarget.StandaloneWindows64
        );

        Debug.Log($"[SPLAT BUNDLE] Built bundle '{bundleName}' with asset '{assetPath}' to: {outputPath}");
    }
}
