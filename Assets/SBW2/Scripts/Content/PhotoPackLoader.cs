using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using SBW2.Data;

namespace SBW2.Content
{
    public class PhotoPackLoader : MonoBehaviour
    {
        public IEnumerator LoadPackManifest(string characterId, Action<CharacterPackManifest> onLoaded, Action<string> onError = null)
        {
            string relative = Path.Combine("SBW2", "Characters", characterId, "pack.json");
            CharacterPackManifest result = null;

            yield return StreamingAssetReader.LoadTextAsync(
                relative,
                json => result = JsonUtility.FromJson<CharacterPackManifest>(json),
                onError
            );

            if (result == null)
            {
                onError?.Invoke($"Could not parse pack manifest for {characterId}.");
                yield break;
            }

            onLoaded?.Invoke(result);
        }

        public IEnumerator LoadTexture(string characterId, string fileName, Action<Texture2D> onLoaded, Action<string> onError = null)
        {
            string fullPath = Path.Combine(
                Application.streamingAssetsPath,
                "SBW2",
                "Characters",
                characterId,
                "photos",
                fileName
            );

#if UNITY_ANDROID && !UNITY_EDITOR
            using (var request = UnityWebRequestTexture.GetTexture(fullPath))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke(request.error);
                    yield break;
                }

                onLoaded?.Invoke(DownloadHandlerTexture.GetContent(request));
            }
#else
            if (!File.Exists(fullPath))
            {
                onError?.Invoke($"Photo not found: {fullPath}");
                yield break;
            }

            byte[] bytes = File.ReadAllBytes(fullPath);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(tex, bytes))
            {
                UnityEngine.Object.Destroy(tex);
                onError?.Invoke($"Invalid image: {fullPath}");
                yield break;
            }
            onLoaded?.Invoke(tex);
            yield break;
#endif
        }
    }
}
