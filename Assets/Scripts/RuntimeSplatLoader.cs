using System.Collections;
using System.Reflection;
using GaussianSplatting.Runtime;
using UnityEngine;
using UnityEngine.Networking;

public class RuntimeSplatLoader : MonoBehaviour
{
    [Header("Platform Splat Bundle URL")]
    [SerializeField] private string splatBundleUrl;  

    [Header("Renderer")]
    [SerializeField] private GaussianSplatRenderer splatRenderer;

    public IEnumerator LoadSplatFromPlatform(string accessToken)
    {
        if (splatRenderer == null)
        {
            splatRenderer = GetComponent<GaussianSplatRenderer>();
        }

        if (splatRenderer == null)
        {
            Debug.LogError("[SPLAT] No GaussianSplatRenderer found on this GameObject.");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(splatBundleUrl))
        {
            Debug.LogError("[SPLAT] splatBundleUrl is empty.");
            yield break;
        }

        Debug.Log("[SPLAT] Downloading splat bundle from: " + splatBundleUrl);

        using (UnityWebRequest req = UnityWebRequestAssetBundle.GetAssetBundle(splatBundleUrl))
        {
            // Same auth as GLB
            req.SetRequestHeader("Authorization", "Bearer " + accessToken);

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[SPLAT] Download failed: {req.responseCode} - {req.error}");
                yield break;
            }

            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(req);
            if (bundle == null)
            {
                Debug.LogError("[SPLAT] AssetBundle is null.");
                yield break;
            }

            // Find the GaussianSplatAsset inside the bundle
            GaussianSplatAsset[] splatAssets = bundle.LoadAllAssets<GaussianSplatAsset>();
            if (splatAssets == null || splatAssets.Length == 0)
            {
                Debug.LogError("[SPLAT] No GaussianSplatAsset found in bundle.");
                yield break;
            }

            var splatAsset = splatAssets[0];

            // --- Find any instance field of type GaussianSplatAsset (name can vary between versions) ---
            var rendererType = typeof(GaussianSplatRenderer);
            FieldInfo assetField = null;

            foreach (var field in rendererType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (typeof(GaussianSplatAsset).IsAssignableFrom(field.FieldType))
                {
                    assetField = field;
                    Debug.Log("[SPLAT] Using field '" + field.Name + "' on GaussianSplatRenderer for runtime asset assignment.");
                    break;
                }
            }

            if (assetField == null)
            {
                var allFields = rendererType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                foreach (var f in allFields)
                {
                    Debug.Log("[SPLAT] Field on GaussianSplatRenderer: " + f.Name + " : " + f.FieldType);
                }
                Debug.LogError("[SPLAT] Could not find any field of type GaussianSplatAsset on GaussianSplatRenderer.");
                yield break;
            }


            assetField.SetValue(splatRenderer, splatAsset);

            Debug.Log("[SPLAT] Runtime splat assigned successfully via reflection (field: " + assetField.Name + ").");
        }
    }
}
