using System.Threading.Tasks;
using UnityEngine;
using GLTFast;

public class RuntimeGlbURL : MonoBehaviour
{
    // Loads ONE GLB with token and places it using given transforms
    public async Task<GameObject> LoadOneGlb(
        string fullUrl,
        string bearerToken,
        string objectName,
        Vector3 position,
        Vector3 rotationEuler,
        Vector3 scale,
        Transform parent = null
    )
    {
        // Wrapper root 
        var wrapper = new GameObject(string.IsNullOrEmpty(objectName) ? "GLB_Instance" : objectName);

        if (parent != null)
            wrapper.transform.SetParent(parent, true);

        wrapper.transform.position = position;
        wrapper.transform.rotation = Quaternion.Euler(rotationEuler);
        wrapper.transform.localScale = scale;

        // IMPORTANT: protected download provider
        var downloadProvider = new BearerTokenDownloadProvider(bearerToken);
        var gltf = new GltfImport(downloadProvider);

        Debug.Log("[RuntimeGlbURL] Loading from: " + fullUrl);

        bool loaded = await gltf.Load(fullUrl);
        if (!loaded)
        {
            Debug.LogError("[RuntimeGlbURL] Load FAILED: " + fullUrl);
            Destroy(wrapper);
            return null;
        }

        bool instantiated = await gltf.InstantiateMainSceneAsync(wrapper.transform);
        if (!instantiated)
        {
            Debug.LogError("[RuntimeGlbURL] Instantiate FAILED: " + fullUrl);
            Destroy(wrapper);
            return null;
        }

        return wrapper;
    }
}
