#if UNITY_EDITOR
using System;
using UnityEditor.Callbacks;
using UnityEngine;

namespace SBW2.EditorTools
{
    public static class SBW2CloudSceneBootstrap
    {
        [DidReloadScripts(0)]
        static void PrepareCloudScene()
        {
            if (!Application.isBatchMode)
                return;

            try
            {
                Debug.Log("SBW2 Cloud v0.18: preparing MobileRebuild scene before player export.");
                SBW2AndroidSetup.Configure();
                SBW2MobileRebuildBootstrap.Bootstrap();
                Debug.Log("SBW2 Cloud: MobileRebuild scene prepared and enabled.");
            }
            catch (Exception ex)
            {
                // A committed fallback scene is already enabled in EditorBuildSettings.
                // Do not abort the cloud build if editor-time regeneration is unavailable.
                Debug.LogWarning("SBW2 Cloud scene regeneration skipped; using committed fallback scene.\n" + ex);
            }
        }
    }
}
#endif
