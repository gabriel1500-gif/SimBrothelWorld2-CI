#if UNITY_EDITOR && UNITY_ANDROID
using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace SBW2.EditorTools
{
    public sealed class SBW2AndroidGameManifestPostprocessor
        : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 1000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string manifest = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifest))
            {
                Debug.LogWarning("SBW2: generated AndroidManifest.xml not found at " + manifest);
                return;
            }

            const string androidNs = "http://schemas.android.com/apk/res/android";

            var doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.Load(manifest);

            var application = doc.SelectSingleNode("/manifest/application") as XmlElement;
            if (application == null)
            {
                Debug.LogWarning("SBW2: Android manifest has no application element.");
                return;
            }

            // Canonical Android game classification (API 26+).
            application.SetAttribute("appCategory", androidNs, "game");

            // Deprecated by Android 8, but some OEM game classifiers historically
            // consulted it. Keeping both is harmless and maximizes Samsung compatibility.
            application.SetAttribute("isGame", androidNs, "true");

            doc.Save(manifest);
            Debug.Log(
                "SBW2 Android manifest hardened: appCategory=game, isGame=true."
            );
        }
    }
}
#endif
