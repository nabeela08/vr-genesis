using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GLTFast;

public class RuntimeGlbURL : MonoBehaviour
{[Header("Full URL of the GLB (https://...)")]
    [SerializeField] private string glbUrl;

    [Header("Spawn point (XR camera or XR Origin)")]
    [SerializeField] private Transform spawnPoint;

    private GameObject instanceRoot;

    private async void Start()
    {
        Debug.Log("[RuntimeGlbUrl] Start()");

        if (string.IsNullOrEmpty(glbUrl))
        {
            Debug.LogError("[RuntimeGlbUrl] glbUrl is empty");
            return;
        }

        var gltf = new GltfImport();

        Debug.Log("[RuntimeGlbUrl] Loading from: " + glbUrl);

        bool loaded = await gltf.Load(glbUrl);

        Debug.Log("[RuntimeGlbUrl] Load finished. Success = " + loaded);

        if (!loaded)
        {
            Debug.LogError("[RuntimeGlbUrl] Loading glTF FAILED");
            return;
        }

        // Parent object created to store model
        instanceRoot = new GameObject("RuntimeGlbInstance");

        // Spawn infront of main camera
        if (spawnPoint != null)
        {
            instanceRoot.transform.position =
                spawnPoint.position + spawnPoint.forward * 1.5f; 
            instanceRoot.transform.rotation = spawnPoint.rotation;
        }
        else
        {
            instanceRoot.transform.position = Vector3.zero;
        }

    
        bool instantiated = await gltf.InstantiateMainSceneAsync(instanceRoot.transform);

        Debug.Log("[RuntimeGlbUrl] Instantiate finished. Success = " + instantiated);

        if (!instantiated)
        {
            Debug.LogError("[RuntimeGlbUrl] Instantiation FAILED");
        }
    }
}
