#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace SBW2.EditorTools
{
    public static class SBW2AndroidSetup
    {
        [MenuItem("SBW2/Configure Android Project")]
        public static void Configure()
        {
            PlayerSettings.productName = "Sim Brothel World 2";
            PlayerSettings.companyName = "SBW2";
            PlayerSettings.bundleVersion = "0.19.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.gabriel1500.simbrothelworld2");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.bundleVersionCode = 19;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // Unity 6 exposes PlayerSettings.Android.appCategory. Reflection keeps
            // the repository's lightweight CI stubs compatible while the real Unity
            // editor writes android:appCategory="game" into the Android player config.
            var appCategory = typeof(PlayerSettings.Android).GetProperty("appCategory");
            if (appCategory != null && appCategory.CanWrite)
                appCategory.SetValue(null, "game", null);

            PlayerSettings.Android.renderOutsideSafeArea = false;
            PlayerSettings.Android.optimizedFramePacing = true;

            Debug.Log("SBW2 Android project settings configured. No APK was built.");
        }
    }
}
#endif
