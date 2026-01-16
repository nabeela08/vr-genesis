using System;
using System.Threading.Tasks;
using UnityEngine;

[Serializable] public class SceneListWrapper { public SceneItem[] items; }

[Serializable]
public class SceneItem
{
    public string name;
    public int entityId;
    public string entityName;
    public string path;
    public float[] position;
    public float[] rotation;
    public float[] scale;
}

public class GenesisGLBSceneComposer : MonoBehaviour
{
    [Header("Paste the JSON ARRAY here: [ {...}, {...} ]")]
    [TextArea(5, 20)]
    public string sceneJson;

    [Header("Base URL (NO 'generic' if your working URL has no generic)")]
    public string baseUrl = "https://genesis.platform.myw.ai/api/Storage/getBlob/";

    [Header("References")]
    public RuntimeGlbURL glbLoader;
    public Transform glbRoot;

    private bool _started = false;

    // Call this ONLY after login success
    public void StartComposeAfterLogin()
    {
        if (_started) return;
        _started = true;
        _ = Compose();
    }

    private async Task Compose()
    {
        if (string.IsNullOrEmpty(sceneJson))
        {
            Debug.LogError("[Composer] sceneJson is empty. Paste the JSON array in Inspector.");
            return;
        }

        // Token from PlatformAuth
        var token = PlatformAuth.AccessToken;
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("[Composer] Access token is empty. Login first.");
            return;
        }

        // Wrap array for JsonUtility
        string wrapped = "{\"items\":" + sceneJson + "}";
        var data = JsonUtility.FromJson<SceneListWrapper>(wrapped);

        if (data?.items == null || data.items.Length == 0)
        {
            Debug.LogError("[Composer] No items in JSON.");
            return;
        }

        foreach (var item in data.items)
        {
            string fullUrl = CombineUrl(baseUrl, item.path);

            Vector3 pos = ToVec3(item.position, Vector3.zero);
            Vector3 rot = ToVec3(item.rotation, Vector3.zero);
            Vector3 scl = ToVec3(item.scale, Vector3.one);

            string objName = !string.IsNullOrEmpty(item.entityName) ? item.entityName : item.name;

            Debug.Log($"[Composer] Loading {objName} => {fullUrl}");

            await glbLoader.LoadOneGlb(
                fullUrl,
                token,
                objName,
                pos,
                rot,
                scl,
                glbRoot
            );
        }
    }

    static Vector3 ToVec3(float[] arr, Vector3 fallback)
    {
        if (arr == null || arr.Length != 3) return fallback;
        return new Vector3(arr[0], arr[1], arr[2]);
    }

    static string CombineUrl(string baseUrl, string path)
        => baseUrl.TrimEnd('/') + "/" + (path ?? "").TrimStart('/');
}
