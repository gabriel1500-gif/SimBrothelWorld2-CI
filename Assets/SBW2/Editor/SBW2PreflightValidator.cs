#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SBW2.Data;

namespace SBW2.EditorTools
{
    public static class SBW2PreflightValidator
    {
        [Serializable]
        class PropertyCatalog
        {
            public PropertyEntry[] properties;
        }

        [Serializable]
        class PropertyEntry
        {
            public string id;
        }

        public static bool Validate(
            bool requirePhysicalPhotos,
            out List<string> errors,
            out List<string> warnings)
        {
            errors = new List<string>();
            warnings = new List<string>();

            string streaming = Application.streamingAssetsPath;
            string dataRoot = Path.Combine(streaming, "SBW2", "Data");
            string rosterPath = Path.Combine(dataRoot, "SBW2_GAMEPLAY_ROSTER_v208.json");
            string propertyPath = Path.Combine(dataRoot, "v208_properties.json");

            GameplayRoster roster = null;

            if (!File.Exists(rosterPath))
            {
                errors.Add("Gameplay roster v208 is missing.");
            }
            else
            {
                try
                {
                    roster = JsonUtility.FromJson<GameplayRoster>(File.ReadAllText(rosterPath));

                    if (roster?.characters == null)
                    {
                        errors.Add("Gameplay roster could not be parsed.");
                    }
                    else
                    {
                        if (roster.characters.Length != 126)
                            errors.Add($"Roster expected 126, found {roster.characters.Length}.");

                        var ids = roster.characters
                            .Where(x => x != null)
                            .Select(x => x.id)
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .ToArray();

                        if (ids.Length != ids.Distinct().Count())
                            errors.Add("Roster contains duplicate IDs.");

                        foreach (string starter in new[] { "belldandy", "elsa", "lara_croft" })
                            if (!ids.Contains(starter))
                                errors.Add($"Starter missing from roster: {starter}.");

                        var active = roster.characters
                            .Where(x => x != null && x.photo_pack_active)
                            .ToArray();

                        if (active.Length != 32)
                            errors.Add($"Active packs expected 32, found {active.Length}.");

                        int referencedPhotos = 0;
                        int physicalPhotos = 0;

                        foreach (var character in active)
                        {
                            string packPath = Path.Combine(
                                streaming, "SBW2", "Characters", character.id, "pack.json"
                            );

                            if (!File.Exists(packPath))
                            {
                                errors.Add($"{character.id}: pack.json missing.");
                                continue;
                            }

                            CharacterPackManifest pack = null;

                            try
                            {
                                pack = JsonUtility.FromJson<CharacterPackManifest>(
                                    File.ReadAllText(packPath)
                                );
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"{character.id}: pack.json parse error: {ex.Message}");
                                continue;
                            }

                            if (pack?.photos == null)
                            {
                                errors.Add($"{character.id}: pack manifest has no photo list.");
                                continue;
                            }

                            if (pack.photos.Length != character.photo_count)
                            {
                                errors.Add(
                                    $"{character.id}: roster says {character.photo_count}, " +
                                    $"pack lists {pack.photos.Length}."
                                );
                            }

                            if (pack.photos.Length != pack.photos.Distinct().Count())
                                errors.Add($"{character.id}: duplicate filenames in pack manifest.");

                            referencedPhotos += pack.photos.Length;

                            foreach (string file in pack.photos)
                            {
                                string photoPath = Path.Combine(
                                    streaming, "SBW2", "Characters", character.id, "photos", file
                                );

                                if (File.Exists(photoPath))
                                    physicalPhotos++;
                            }
                        }

                        if (referencedPhotos != 650)
                            errors.Add($"Photo references expected 650, found {referencedPhotos}.");

                        if (physicalPhotos != 650)
                        {
                            string message =
                                $"Physical photos present: {physicalPhotos}/650. " +
                                "Metadata is complete; media integration is still staged.";

                            if (requirePhysicalPhotos) errors.Add(message);
                            else warnings.Add(message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add("Roster validation exception: " + ex.Message);
                }
            }

            if (!File.Exists(propertyPath))
            {
                errors.Add("v208 property catalog is missing.");
            }
            else
            {
                try
                {
                    var catalog = JsonUtility.FromJson<PropertyCatalog>(
                        File.ReadAllText(propertyPath)
                    );

                    int count = catalog?.properties?.Length ?? 0;
                    if (count != 7)
                        errors.Add($"Property catalog expected 7 entries, found {count}.");
                }
                catch (Exception ex)
                {
                    errors.Add("Property validation exception: " + ex.Message);
                }
            }

            string scenePath = "Assets/SBW2/Scenes/MobileRebuild.unity";
            if (!File.Exists(scenePath))
                errors.Add("MobileRebuild scene is missing.");

            bool sceneEnabled = EditorBuildSettings.scenes.Any(
                x => x.enabled && x.path == scenePath
            );

            if (!sceneEnabled)
                errors.Add("MobileRebuild scene is not enabled in Build Settings.");

            return errors.Count == 0;
        }

        [MenuItem("SBW2/Run Preflight Validation")]
        public static void Run()
        {
            bool pass = Validate(
                false,
                out var errors,
                out var warnings
            );

            foreach (string warning in warnings)
                Debug.LogWarning("SBW2 PRE-FLIGHT: " + warning);

            if (pass)
            {
                string message =
                    "PASS\n\n" +
                    "126 roster\n" +
                    "7 properties\n" +
                    "32 active pack manifests\n" +
                    "650 photo references\n" +
                    "MobileRebuild scene enabled";

                if (warnings.Count > 0)
                    message += "\n\nWarnings: " + warnings.Count;

                EditorUtility.DisplayDialog("SBW2 Preflight", message, "OK");
                Debug.Log("SBW2 PRE-FLIGHT PASS");
            }
            else
            {
                string report = string.Join("\n", errors);
                Debug.LogError("SBW2 PRE-FLIGHT FAILED\n" + report);
                EditorUtility.DisplayDialog(
                    "SBW2 Preflight",
                    $"FAILED — {errors.Count} issue(s)\n\nSee Console for details.",
                    "OK"
                );
            }
        }
    }
}
#endif
