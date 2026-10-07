#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SBW2.Data;
using SBW2.Core;

namespace SBW2.EditorTools
{
    [InitializeOnLoad]
    public static class SBW2AutoFirstOpen
    {
        const string Version = "0.18.0";
        const string SessionKey = "SBW2_AUTO_FIRST_OPEN_" + Version;

        static SBW2AutoFirstOpen()
        {
            if (SessionState.GetBool(SessionKey, false))
                return;

            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            // Build Automation runs Unity in batch mode. In that environment we
            // skip media/data validation, but still configure Android and create
            // the mobile scene so the cloud build has a valid enabled scene.
            if (Application.isBatchMode)
            {
                try
                {
                    Debug.Log("SBW2 cloud batch bootstrap: configuring Android and creating scene.");
                    SBW2AndroidSetup.Configure();
                    SBW2MobileRebuildBootstrap.Bootstrap();
                    Debug.Log("SBW2 cloud batch bootstrap complete.");
                }
                catch (Exception ex)
                {
                    Debug.LogError("SBW2 cloud batch bootstrap failed\n" + ex);
                    throw;
                }
                return;
            }

            try
            {
                Debug.Log("SBW2 v0.18.0 AUTO FIRST OPEN: starting validation.");

                if (!Validate(out string report))
                {
                    Debug.LogError(
                        "SBW2 AUTO FIRST OPEN FAILED\n" + report +
                        "\nFix the reported issue before creating a build."
                    );
                    return;
                }

                Debug.Log("SBW2 AUTO FIRST OPEN DATA PASS\n" + report);

                // Apply Android project settings automatically.
                SBW2AndroidSetup.Configure();

                // Recreate the current mobile scene from the verified scripts/data.
                SBW2MobileRebuildBootstrap.Bootstrap();

                const string scenePath = "Assets/SBW2/Scenes/MobileRebuild.unity";
                if (File.Exists(scenePath))
                {
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    Debug.Log(
                        "SBW2 v0.18.0 READY: MobileRebuild.unity opened automatically. " +
                        "Press Play after confirming there are no red Console errors."
                    );
                }
                else
                {
                    Debug.LogError("SBW2 AUTO FIRST OPEN: expected scene was not created.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("SBW2 AUTO FIRST OPEN EXCEPTION\n" + ex);
            }
        }

        static bool Validate(out string report)
        {
            var lines = new System.Collections.Generic.List<string>();
            bool pass = true;

            string streaming = Application.streamingAssetsPath;
            string dataDir = Path.Combine(streaming, "SBW2", "Data");

            string manifestPath = Path.Combine(
                dataDir, "SBW2_MANIFIESTO_PERSONAJES_v208.json"
            );
            string gameplayPath = Path.Combine(
                dataDir, "SBW2_GAMEPLAY_ROSTER_v208.json"
            );
            string propertiesPath = Path.Combine(
                dataDir, "v208_properties.json"
            );

            if (!File.Exists(manifestPath))
            {
                pass = false;
                lines.Add("FAIL: master manifest missing.");
            }

            if (!File.Exists(gameplayPath))
            {
                pass = false;
                lines.Add("FAIL: gameplay roster missing.");
            }

            if (!File.Exists(propertiesPath))
            {
                pass = false;
                lines.Add("FAIL: property catalog missing.");
            }

            if (!pass)
            {
                report = string.Join("\n", lines);
                return false;
            }

            var manifest = JsonUtility.FromJson<SBW2Manifest>(
                File.ReadAllText(manifestPath)
            );
            var gameplay = JsonUtility.FromJson<GameplayRoster>(
                File.ReadAllText(gameplayPath)
            );

            if (manifest == null || manifest.characters == null)
            {
                pass = false;
                lines.Add("FAIL: master manifest parse.");
            }
            else
            {
                lines.Add($"Roster: {manifest.characters.Length}");
                if (manifest.characters.Length != 126) pass = false;

                var active = manifest.characters.Where(x => x.photo_pack_active).ToArray();
                lines.Add($"Active packs: {active.Length}");
                if (active.Length != 32) pass = false;

                int physical = 0;

                foreach (var c in active)
                {
                    string charDir = Path.Combine(
                        streaming, "SBW2", "Characters", c.id
                    );
                    string packPath = Path.Combine(charDir, "pack.json");

                    if (!File.Exists(packPath))
                    {
                        pass = false;
                        lines.Add($"FAIL: {c.id} pack.json missing.");
                        continue;
                    }

                    var pack = JsonUtility.FromJson<CharacterPackManifest>(
                        File.ReadAllText(packPath)
                    );

                    if (pack == null || pack.photos == null)
                    {
                        pass = false;
                        lines.Add($"FAIL: {c.id} pack invalid.");
                        continue;
                    }

                    if (pack.photos.Length != c.photo_count)
                    {
                        pass = false;
                        lines.Add(
                            $"FAIL: {c.id} expected {c.photo_count}, " +
                            $"catalog has {pack.photos.Length}."
                        );
                    }

                    foreach (string photo in pack.photos)
                    {
                        string file = Path.Combine(charDir, "photos", photo);
                        if (!File.Exists(file))
                        {
                            pass = false;
                            lines.Add($"FAIL: {c.id}/{photo} missing.");
                        }
                        else
                        {
                            physical++;
                        }
                    }
                }

                lines.Add($"Physical photos: {physical}");
                if (physical != 650) pass = false;
            }

            if (gameplay == null || gameplay.characters == null)
            {
                pass = false;
                lines.Add("FAIL: gameplay roster parse.");
            }
            else
            {
                string[] starters = { "belldandy", "elsa", "lara_croft" };
                foreach (string id in starters)
                {
                    if (!gameplay.characters.Any(x => x.id == id))
                    {
                        pass = false;
                        lines.Add("FAIL: starter missing: " + id);
                    }
                }
            }

            if (V208Economy.XpNeeded(1) != 70 ||
                V208Economy.XpNeeded(2) != 120 ||
                V208Economy.XpNeeded(3) != 180 ||
                V208Economy.XpNeeded(4) != 250)
            {
                pass = false;
                lines.Add("FAIL: XP table invariant.");
            }

            if (V208Economy.SkillApCost(0) != 2 ||
                V208Economy.SkillApCost(4) != 6)
            {
                pass = false;
                lines.Add("FAIL: skill AP invariant.");
            }

            lines.Add(pass ? "RESULT: PASS" : "RESULT: FAIL");
            report = string.Join("\n", lines);
            return pass;
        }
    }
}
#endif
