using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;

public class SplatLoader : MonoBehaviour
{
    [Header("Protected SPLAT URL")]
    [SerializeField] private string splatUrl;

    [Header("Debug")]
    [SerializeField] private string localSplatPath;
    [SerializeField] private int downloadedBytes;

    //call from PlatformAuth
    public IEnumerator DownloadSplat(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(splatUrl))
        {
            Debug.LogError("[SPLAT] splatUrl is empty.");
            yield break;
        }

        string fileName = Path.GetFileName(splatUrl);
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = "remoteModel.splat";
        }

        localSplatPath = Path.Combine(Application.persistentDataPath, fileName);
        Debug.Log($"[SPLAT] Downloading from: {splatUrl}");
        Debug.Log($"[SPLAT] Will save to: {localSplatPath}");

        using (UnityWebRequest req = UnityWebRequest.Get(splatUrl))
        {
            req.SetRequestHeader("Authorization", "Bearer " + accessToken);
            req.certificateHandler = new BypassCertificateHandler();
            req.disposeCertificateHandlerOnDispose = true;

            yield return req.SendWebRequest();

            Debug.Log($"[SPLAT] result={req.result}, code={req.responseCode}, error={req.error}");

            if (req.result == UnityWebRequest.Result.Success)
            {
                byte[] data = req.downloadHandler.data;
                downloadedBytes = data != null ? data.Length : 0;

                if (downloadedBytes > 0)
                {
                    File.WriteAllBytes(localSplatPath, data);
                    Debug.Log($"[SPLAT] Downloaded {downloadedBytes} bytes and saved to: {localSplatPath}");
                }
                else
                {
                    Debug.LogError("[SPLAT] Downloaded data is empty!");
                    localSplatPath = null;
                }
            }
            else
            {
                Debug.LogError("[SPLAT] Download failed.");
                localSplatPath = null;
                downloadedBytes = 0;
            }
        }
    }

    public string GetLocalSplatPath()
    {
        return localSplatPath;
    }

    private class BypassCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }
}
