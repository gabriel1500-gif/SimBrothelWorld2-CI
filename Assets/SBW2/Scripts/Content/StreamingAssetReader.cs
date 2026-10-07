using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace SBW2.Content
{
    public static class StreamingAssetReader
    {
        public static IEnumerator LoadTextAsync(string relativePath, Action<string> onLoaded, Action<string> onError = null)
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, relativePath);

#if UNITY_ANDROID && !UNITY_EDITOR
            using (var request = UnityWebRequest.Get(fullPath))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke(request.error);
                    yield break;
                }

                onLoaded?.Invoke(request.downloadHandler.text);
            }
#else
            if (!File.Exists(fullPath))
            {
                onError?.Invoke($"File not found: {fullPath}");
                yield break;
            }

            onLoaded?.Invoke(File.ReadAllText(fullPath));
            yield break;
#endif
        }
    }
}
