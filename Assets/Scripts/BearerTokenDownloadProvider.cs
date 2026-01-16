using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using GLTFast.Loading;

public class BearerTokenDownloadProvider : IDownloadProvider
{
    private readonly string _token;

    public BearerTokenDownloadProvider(string token)
    {
        _token = token?.Trim();
    }

    public async Task<IDownload> Request(Uri url)
    {
        Debug.Log($"[DL] GET {url}");
        using var req = UnityWebRequest.Get(url);

        if (!string.IsNullOrEmpty(_token))
            req.SetRequestHeader("Authorization", $"Bearer {_token}");

        req.downloadHandler = new DownloadHandlerBuffer();

        var op = req.SendWebRequest();
        while (!op.isDone)
            await Task.Yield();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[DL] Failed: {req.responseCode} {req.error} URL={url}");
            return new SimpleDownload(null, req.error, true);
        }

        // For .glb/.bin => binary
        return new SimpleDownload(req.downloadHandler.data, null, true);
    }

    public async Task<ITextureDownload> RequestTexture(Uri url, bool nonReadable)
    {
        Debug.Log($"[DL] GET {url}");
        using var req = UnityWebRequestTexture.GetTexture(url, nonReadable);

        if (!string.IsNullOrEmpty(_token))
            req.SetRequestHeader("Authorization", $"Bearer {_token}");

        var op = req.SendWebRequest();
        while (!op.isDone)
            await Task.Yield();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[DL-Tex] Failed: {req.responseCode} {req.error} URL={url}");
            return new SimpleTextureDownload(null, req.error);
        }

        var tex = DownloadHandlerTexture.GetContent(req);
        return new SimpleTextureDownload(tex, null);
    }

    private class SimpleDownload : IDownload
    {
        public bool Success { get; }
        public string Error { get; }
        public byte[] Data { get; }
        public string Text { get; }
        public bool? IsBinary { get; }   

        public SimpleDownload(byte[] data, string error, bool? isBinary)
        {
            Data = data;
            Error = error;
            Success = string.IsNullOrEmpty(error);
            IsBinary = isBinary;

            // Text should be available for non-binary downloads (gltf json)
            Text = (IsBinary == false && data != null)
                ? System.Text.Encoding.UTF8.GetString(data)
                : null;
        }

        public void Dispose() { }
    }

    private class SimpleTextureDownload : ITextureDownload
    {
        public bool Success { get; }
        public string Error { get; }
        public byte[] Data { get; }      // usually null for textures
        public string Text { get; }      // usually null
        public bool? IsBinary { get; }   // textures are binary => true
        public Texture2D Texture { get; }

        public SimpleTextureDownload(Texture2D texture, string error)
        {
            Texture = texture;
            Error = error;
            Success = string.IsNullOrEmpty(error);

            IsBinary = true;
            Data = null;
            Text = null;
        }

        public void Dispose() { }
    }
}
