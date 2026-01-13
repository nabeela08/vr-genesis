using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;      
using GLTFast;

public class PlatformAuth : MonoBehaviour
{
    private byte[] downloadedGlbData;
    [SerializeField] private Transform modelParent;

    [Header("SSO Settings")]
    [SerializeField] private string loginUrl;   
    [SerializeField] private string clientId;   

    [Header("Login Credentials (for auto-test or default values)")]
    [SerializeField] private string defaultEmail;
    [SerializeField] private string defaultPassword;

    [Header("UI References")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TextMeshProUGUI statusText;

    [Header("Protected GLB Settings")]
    [SerializeField] private string glbUrl;     
    [SerializeField] private GltfAsset gltfAsset;

    [Header("Splat Runtime Loader")]
    [SerializeField] private RuntimeSplatLoader runtimeSplatLoader;

    [Header("Login UI Root (hide after success)")]
    [SerializeField] private GameObject loginUIRoot;

    private string accessToken;

    [Serializable]
    private class TokenResponse
    {
        public string access_token;
        public int expires_in;  // optional, depends on response
    }

    // Called from Login Button onClick
    public void OnLoginButtonClicked()
    {
        string email = emailInput != null && !string.IsNullOrEmpty(emailInput.text)
            ? emailInput.text
            : defaultEmail;

        string password = passwordInput != null && !string.IsNullOrEmpty(passwordInput.text)
            ? passwordInput.text
            : defaultPassword;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            SetStatus("Please enter email and password.");
            return;
        }

        StartCoroutine(LoginAndLoadGlb(email, password));
    }

    private IEnumerator LoginAndLoadGlb(string email, string password)
    {
        SetStatus("Logging in...");

        // Build form payload (same as AuthService)
        WWWForm form = new WWWForm();
        form.AddField("client_id", clientId);
        form.AddField("grant_type", "password");
        form.AddField("username", email);
        form.AddField("password", password);

        using (UnityWebRequest request = UnityWebRequest.Post(loginUrl, form))
        {
            request.certificateHandler = new BypassCertificateHandler();
            request.disposeCertificateHandlerOnDispose = true;
            
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Login failed: {request.responseCode} - {request.error}\n{request.downloadHandler.text}");
                SetStatus("Login failed: " + request.error);
                yield break;
            }

            string json = request.downloadHandler.text;
            Debug.Log("Login response: " + json);

            //Parse JSON to get access_token 
            TokenResponse tokenResponse;
            try
            {
                tokenResponse = JsonUtility.FromJson<TokenResponse>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError("Error parsing token JSON: " + ex.Message);
                SetStatus("Error parsing token.");
                yield break;
            }

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.access_token))
            {
                Debug.LogError("Token response did not contain access_token. Check JSON field names.");
                SetStatus("No access_token found in response.");
                yield break;
            }

            accessToken = tokenResponse.access_token;
            Debug.Log("Got access token: " + accessToken);
            SetStatus("Login successful. Loading 3D model...");

        }

        // Use accessToken to load GLB via glTFast 
        if (gltfAsset == null)
        {
            Debug.LogError("GltfAsset reference is missing.");
            SetStatus("Error: GltfAsset missing.");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(glbUrl))
        {
            Debug.LogError("glbUrl is empty.");
            SetStatus("Error: GLB URL is empty.");
            yield break;
        }

        yield return StartCoroutine(TestRawGlbDownload());

        if (downloadedGlbData == null || downloadedGlbData.Length == 0)
{
    Debug.LogError("[GLB] Downloaded GLB data is empty.");
    SetStatus("Error: GLB download failed.");
    yield break;
}

// ---- Use glTFast directly from bytes ----
SetStatus("Parsing GLB...");

var gltfImport = new GLTFast.GltfImport();

// Start async load from the byte array
var loadTask = gltfImport.Load(downloadedGlbData);

// Wait for the task to finish inside coroutine
while (!loadTask.IsCompleted)
{
    yield return null;
}

if (!loadTask.Result)
{
    Debug.LogError("[GLB] glTFast failed to parse GLB.");
    SetStatus("Error: Failed to parse 3D model.");
    yield break;
}
// ---- Choose a scene to instantiate ----
int sceneCount = gltfImport.SceneCount;
Debug.Log($"[GLB] sceneCount = {sceneCount}");

if (sceneCount == 0)
{
    Debug.LogError("[GLB] GLB has no scenes; cannot instantiate.");
    SetStatus("Error: 3D model has no scenes.");
    yield break;
}

// Just use scene 0
int sceneIndexToUse = 0;

SetStatus("Instantiating 3D model...");

Transform parent = modelParent != null ? modelParent : this.transform;

var instTask = gltfImport.InstantiateSceneAsync(parent, sceneIndexToUse);

while (!instTask.IsCompleted)
{
    yield return null;
}

if (!instTask.Result)
{
    Debug.LogError("[GLB] Failed to instantiate 3D model.");
    SetStatus("Error: Failed to instantiate 3D model.");
    yield break;
}

// Hide login UI when glb loaded
if (loginUIRoot != null)
{
    loginUIRoot.SetActive(false);
}

Debug.Log("[GLB] GLB loaded and instantiated successfully!");
SetStatus("Model loaded!");

// ---- After GLB is ready, load the splat from platform ----

// ---- After GLB is ready, load the splat from platform ----
if (runtimeSplatLoader != null)
{
    StartCoroutine(runtimeSplatLoader.LoadSplatFromPlatform(accessToken));
}
else
{
    Debug.LogWarning("[SPLAT] runtimeSplatLoader reference is not set on PlatformAuth.");
}

// if (splatLoader != null)
// {
//     StartCoroutine(splatLoader.LoadSplatFromPlatform(accessToken));
// }
// else
// {
//     Debug.LogWarning("[SPLAT] splatLoader reference is not set on PlatformAuth.");
// }


        // ---- Download SPLAT file from platform ----
        // if (splatLoader != null)
        // {
        //     SetStatus("Downloading splat file...");
        //     yield return StartCoroutine(splatLoader.DownloadSplat(accessToken));

        //     string localSplatPath = splatLoader.GetLocalSplatPath();
        //     if (!string.IsNullOrEmpty(localSplatPath))
        //     {
        //         Debug.Log("[SPLAT] Splat file downloaded successfully: " + localSplatPath);
        //     }
        //     else
        //     {
        //         Debug.LogError("[SPLAT] Splat download failed or path is empty.");
        //     }
        // }
        // else
        // {
        //     Debug.LogWarning("[SPLAT] No SplatLoader assigned, skipping splat download.");
        // }

        // Hide login UI when glb loaded
        if (loginUIRoot != null)
        {
            loginUIRoot.SetActive(false);
        }

        Debug.Log("[GLB] GLB loaded and instantiated successfully!");
        SetStatus("Model loaded!");

    }

    //ADDITIONAL
    private IEnumerator TestRawGlbDownload()
    {
        Debug.Log("[TEST] Starting raw GLB download test...");
        using (UnityWebRequest req = UnityWebRequest.Get(glbUrl))
        {
            req.SetRequestHeader("Authorization", "Bearer " + accessToken);
            // Still use bypass for now (same as login)
            req.certificateHandler = new BypassCertificateHandler();
            req.disposeCertificateHandlerOnDispose = true;

            yield return req.SendWebRequest();

            Debug.Log($"[TEST] result={req.result}, code={req.responseCode}, error={req.error}");
            if (req.result == UnityWebRequest.Result.Success)
            {
                downloadedGlbData = req.downloadHandler.data;
                Debug.Log($"[TEST] bytes downloaded = {downloadedGlbData.Length}");
            }
            else
            {
                downloadedGlbData = null;
                Debug.Log("[TEST] no data downloaded");
            }
        }
    }

    //for unity to not verify SSL certificate
    private class BypassCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            // DEV ONLY – always trust
            return true;
        }
    }

    private void SetStatus(string msg)
    {
        if (statusText != null)
        {
            statusText.text = msg;
        }
        Debug.Log(msg);
    }
}
