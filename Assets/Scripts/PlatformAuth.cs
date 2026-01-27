using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Networking;
using TMPro;

public class PlatformAuth : MonoBehaviour
{
    public static string AccessToken;

    [SerializeField] private GenesisGLBSceneComposer composer;

    [Header("SSO Settings")]
    [SerializeField] private string loginUrl;   // token endpoint
    [SerializeField] private string clientId;

    [Header("UI References")]
    [SerializeField] private GameObject loginUIRoot;
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TextMeshProUGUI statusText;

    [Header("Default values (optional)")]
    [SerializeField] private string defaultEmail;
    [SerializeField] private string defaultPassword;

    [Header("Splat Runtime Loader")]
    [SerializeField] private RuntimeSplatLoader runtimeSplatLoader;

    private string accessToken;
    private string refreshToken;
    private long accessTokenExpiryUnix;

    [Serializable]
    private class TokenResponse
    {
        public string access_token;
        public int expires_in;
        public string refresh_token;
        public int refresh_expires_in;
    }

    private const string PREF_ACCESS = "access_token";
    private const string PREF_REFRESH = "refresh_token";
    private const string PREF_EXPIRY = "access_expiry";

    private void Start()
    {
        if (loginUIRoot != null) loginUIRoot.SetActive(true);
        SetStatus("Please login.");

        Debug.Log($"GFX API: {SystemInfo.graphicsDeviceType} | {SystemInfo.graphicsDeviceName} | {SystemInfo.graphicsDeviceVersion}");
    }

    private void TryAutoLogin()
    {
        string savedAccess = PlayerPrefs.GetString(PREF_ACCESS, "");
        string savedRefresh = PlayerPrefs.GetString(PREF_REFRESH, "");
        long expiry = 0;
        long.TryParse(PlayerPrefs.GetString(PREF_EXPIRY, "0"), out expiry);

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Access token still valid
        if (!string.IsNullOrEmpty(savedAccess) && now < expiry)
        {
            accessToken = savedAccess;
            AccessToken = savedAccess;

            Debug.Log("[Auth] Using saved access token (still valid).");

            OnAuthenticated();
            return;
        }

        if (!string.IsNullOrEmpty(savedRefresh))
        {
            Debug.Log("[Auth] Access expired, trying refresh token...");
            StartCoroutine(RefreshTokenCoroutine(savedRefresh));
        }
        else
        {
            Debug.Log("[Auth] No saved session. Show login UI.");
            SetStatus("Please login.");
            if (loginUIRoot != null) loginUIRoot.SetActive(true);
        }
    }

    // Button onClick
    public void OnLoginButtonClicked()
    {
        string email = (emailInput != null && !string.IsNullOrEmpty(emailInput.text)) ? emailInput.text : defaultEmail;
        string password = (passwordInput != null && !string.IsNullOrEmpty(passwordInput.text)) ? passwordInput.text : defaultPassword;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            SetStatus("Please enter email and password.");
            return;
        }

        StartCoroutine(LoginCoroutine(email, password));
    }

    private IEnumerator LoginCoroutine(string email, string password)
    {
        SetStatus("Logging in...");

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
                Debug.LogError($"[Auth] Login failed: {request.responseCode} - {request.error}\n{request.downloadHandler.text}");
                SetStatus("Login failed. Check credentials.");
                if (loginUIRoot != null) loginUIRoot.SetActive(true);
                yield break;
            }

            var json = request.downloadHandler.text;
            var tokenResponse = JsonUtility.FromJson<TokenResponse>(json);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.access_token))
            {
                Debug.LogError("[Auth] No access_token in response.");
                SetStatus("Login failed (no access token).");
                if (loginUIRoot != null) loginUIRoot.SetActive(true);
                yield break;
            }

            SaveTokens(tokenResponse);
            Debug.Log("[Auth] Login success. Tokens saved.");

            OnAuthenticated();
        }
    }

    private IEnumerator RefreshTokenCoroutine(string refresh)
    {
        SetStatus("Restoring session...");

        WWWForm form = new WWWForm();
        form.AddField("client_id", clientId);
        form.AddField("grant_type", "refresh_token");
        form.AddField("refresh_token", refresh);

        using (UnityWebRequest request = UnityWebRequest.Post(loginUrl, form))
        {
            request.certificateHandler = new BypassCertificateHandler();
            request.disposeCertificateHandlerOnDispose = true;
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[Auth] Refresh failed: {request.responseCode} - {request.error}\n{request.downloadHandler.text}");
                SetStatus("Session expired. Please login again.");
                if (loginUIRoot != null) loginUIRoot.SetActive(true);
                yield break;
            }

            var json = request.downloadHandler.text;
            var tokenResponse = JsonUtility.FromJson<TokenResponse>(json);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.access_token))
            {
                Debug.LogError("[Auth] Refresh response missing access_token.");
                SetStatus("Session expired. Please login again.");
                if (loginUIRoot != null) loginUIRoot.SetActive(true);
                yield break;
            }

            SaveTokens(tokenResponse);
            Debug.Log("[Auth] Session restored. Tokens saved.");

            OnAuthenticated();
        }
    }

    private void SaveTokens(TokenResponse tokenResponse)
    {
        accessToken = tokenResponse.access_token;
        refreshToken = tokenResponse.refresh_token;

        AccessToken = accessToken;

        accessTokenExpiryUnix =
            DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            + tokenResponse.expires_in
            - 30; // safety buffer

        PlayerPrefs.SetString(PREF_ACCESS, accessToken);
        PlayerPrefs.SetString(PREF_REFRESH, refreshToken);
        PlayerPrefs.SetString(PREF_EXPIRY, accessTokenExpiryUnix.ToString());
        PlayerPrefs.Save();
    }

    private void OnAuthenticated()
    {
        SetStatus("Authenticated. Loading scene...");

    if (loginUIRoot != null) loginUIRoot.SetActive(false);

    //splat loader
    if (runtimeSplatLoader != null)
    {
        Debug.Log("[SPLAT] Starting runtime splat load after auth...");
        StartCoroutine(runtimeSplatLoader.LoadSplatFromPlatform(accessToken));
    }
    else
    {
        Debug.LogWarning("[SPLAT] runtimeSplatLoader reference not set on PlatformAuth.");
    }
    
    if (composer != null)
    {
        composer.StartComposeAfterLogin();
    }
    else
    {
        Debug.LogWarning("[Auth] Composer reference not set in Inspector.");
    }
    }

    // DEV ONLY – bypass SSL
    private class BypassCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        Debug.Log(msg);
    }
}
