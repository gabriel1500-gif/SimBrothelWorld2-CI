#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SBW2.Content;
using SBW2.Core;
using SBW2.UI;

namespace SBW2.EditorTools
{
    public class SBW2PrebuildGate : IPreprocessBuildWithReport
    {
        [Serializable]
        class BuildStamp
        {
            public string app_version;
            public string source_build;
            public string revision;
            public string channel;
        }

        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            try
            {
                SBW2AndroidSetup.Configure();
                SBW2MobileRebuildBootstrap.Bootstrap();
                WriteBuildStamp();
            }
            catch (Exception ex)
            {
                throw new BuildFailedException(
                    "SBW2 deterministic scene/bootstrap preparation failed: " + ex
                );
            }

            if (!SBW2PreflightValidator.Validate(
                    true,
                    out var errors,
                    out var warnings))
            {
                throw new BuildFailedException(
                    "SBW2 preflight failed:\n" + string.Join("\n", errors)
                );
            }

            foreach (string warning in warnings)
                Debug.LogWarning("SBW2 BUILD GATE: " + warning);

            const string scenePath = "Assets/SBW2/Scenes/MobileRebuild.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            Require<GameplayManager>("GameplayManager");
            Require<ProgressionManager>("ProgressionManager");
            Require<CampaignManager>("CampaignManager");
            Require<PhotoPackLoader>("PhotoPackLoader");
            Require<MobilePerformanceManager>("MobilePerformanceManager");
            Require<Canvas>("Canvas");
            Require<GraphicRaycaster>("GraphicRaycaster");
            Require<EventSystem>("EventSystem");
            Require<StandaloneInputModule>("StandaloneInputModule");
            Require<MobileStartMenu>("MobileStartMenu");
            Require<MobileGameController>("MobileGameController");
            Require<CharacterGallery>("CharacterGallery");
            Require<DaySummaryPanel>("DaySummaryPanel");

            ValidateSerializedReferences(
                UnityEngine.Object.FindAnyObjectByType<MobileStartMenu>(),
                new[]
                {
                    "menuRoot", "gameRoot", "infoText",
                    "continueButton", "easyButton", "normalButton", "hardButton"
                }
            );

            ValidateSerializedReferences(
                UnityEngine.Object.FindAnyObjectByType<MobileGameController>(),
                new[]
                {
                    "statusText", "titleText", "bodyText",
                    "heroPanel", "contentPanel",
                    "heroImage", "heroCaption",
                    "heroTitleText", "heroMetaText", "heroBadgeText",
                    "metricRow",
                    "metric1Label", "metric1Value",
                    "metric2Label", "metric2Value",
                    "metric3Label", "metric3Value",
                    "metric4Label", "metric4Value",
                    "dayChipText", "goldChipText",
                    "difficultyChipText", "stageChipText",
                    "homeButton", "marketButton", "staffButton",
                    "propertiesButton", "progressionButton", "equipmentButton",
                    "galleryButton", "endDayButton",
                    "moreButton", "morePanel",
                    "homePanel", "homeLastDayText",
                    "homeMissionText", "homeRewardText",
                    "previousButton", "nextButton",
                    "primaryButton", "secondaryButton",
                    "tertiaryButton", "quaternaryButton", "daySummary",
                    "customPanel", "customNameInput", "customAgeInput"
                }
            );

            ValidateSerializedReferences(
                UnityEngine.Object.FindAnyObjectByType<CharacterGallery>(),
                new[] { "image", "caption" }
            );

            ValidateSerializedReferences(
                UnityEngine.Object.FindAnyObjectByType<DaySummaryPanel>(),
                new[] { "panel", "summaryText", "closeButton" }
            );

            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)
                != "com.gabriel1500.simbrothelworld2")
            {
                throw new BuildFailedException("Unexpected Android application identifier.");
            }

            if (PlayerSettings.bundleVersion != "0.19.0")
                throw new BuildFailedException(
                    "Unexpected bundleVersion: " + PlayerSettings.bundleVersion
                );

            if (PlayerSettings.Android.bundleVersionCode != 19)
                throw new BuildFailedException(
                    "Unexpected Android versionCode: " +
                    PlayerSettings.Android.bundleVersionCode
                );

            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
                throw new BuildFailedException("ARM64 is not enabled.");

            Debug.Log(
                "SBW2 BUILD GATE PASS — scene/UI/data wiring validated before Android export."
            );
        }

        static void WriteBuildStamp()
        {
            string revision =
                Environment.GetEnvironmentVariable("BUILD_REVISION") ??
                Environment.GetEnvironmentVariable("GIT_COMMIT") ??
                "local-editor";

            var stamp = new BuildStamp
            {
                app_version = PlayerSettings.bundleVersion,
                source_build = "v208_ELSA_21",
                revision = revision,
                channel = "unity-build"
            };

            string dir = Path.Combine(
                Application.streamingAssetsPath,
                "SBW2",
                "Data"
            );
            Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, "build_info.json");
            File.WriteAllText(path, JsonUtility.ToJson(stamp, true));

            if (!File.Exists(path))
                throw new BuildFailedException("Could not create build_info.json.");

            Debug.Log("SBW2 build stamp: " + revision);
        }

        static void Require<T>(string label) where T : UnityEngine.Object
        {
            if (UnityEngine.Object.FindAnyObjectByType<T>() == null)
                throw new BuildFailedException("Missing required scene component: " + label);
        }

        static void ValidateSerializedReferences(
            UnityEngine.Object target,
            IEnumerable<string> propertyNames)
        {
            if (target == null)
                throw new BuildFailedException("Serialized target is null.");

            var so = new SerializedObject(target);
            var missing = new List<string>();

            foreach (string name in propertyNames)
            {
                var property = so.FindProperty(name);
                if (property == null || property.objectReferenceValue == null)
                    missing.Add(name);
            }

            if (missing.Count > 0)
            {
                throw new BuildFailedException(
                    target.GetType().Name +
                    " has missing serialized references: " +
                    string.Join(", ", missing)
                );
            }
        }
    }
}
#endif
