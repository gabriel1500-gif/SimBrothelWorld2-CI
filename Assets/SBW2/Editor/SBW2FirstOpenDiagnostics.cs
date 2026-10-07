#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SBW2.Data;
using SBW2.Core;

namespace SBW2.EditorTools
{
    public static class SBW2FirstOpenDiagnostics
    {
        [MenuItem("SBW2/First Open Diagnostics")]
        public static void Run()
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            string dataRoot = Path.Combine(Application.streamingAssetsPath, "SBW2", "Data");
            string gameplayRoster = Path.Combine(dataRoot, "SBW2_GAMEPLAY_ROSTER_v208.json");
            string masterManifest = Path.Combine(dataRoot, "SBW2_MANIFIESTO_PERSONAJES_v208.json");
            string propertyCatalog = Path.Combine(dataRoot, "v208_properties.json");

            if (!File.Exists(gameplayRoster)) errors.Add("Gameplay roster is missing.");
            if (!File.Exists(masterManifest)) errors.Add("Master v208 manifest is missing.");
            if (!File.Exists(propertyCatalog)) errors.Add("v208 property catalog is missing.");

            GameplayRoster roster = null;

            if (File.Exists(gameplayRoster))
            {
                try
                {
                    roster = JsonUtility.FromJson<GameplayRoster>(File.ReadAllText(gameplayRoster));
                }
                catch (Exception ex)
                {
                    errors.Add("Gameplay roster parse error: " + ex.Message);
                }
            }

            if (roster == null || roster.characters == null)
            {
                errors.Add("Gameplay roster has no characters.");
            }
            else
            {
                if (roster.characters.Length != 126)
                    errors.Add($"Expected 126 roster characters, found {roster.characters.Length}.");

                var duplicateIds = roster.characters
                    .Where(x => x != null)
                    .GroupBy(x => x.id)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToArray();

                if (duplicateIds.Length > 0)
                    errors.Add("Duplicate character IDs: " + string.Join(", ", duplicateIds));

                string[] starters = { "belldandy", "elsa", "lara_croft" };

                foreach (string id in starters)
                {
                    var c = roster.characters.FirstOrDefault(x => x.id == id);

                    if (c == null)
                        errors.Add("Starter missing from roster: " + id);
                    else if (!c.photo_pack_active)
                        warnings.Add($"{c.name} is a valid starter but has no active v208 photo pack.");
                }
            }

            if (V208Economy.XpNeeded(1) != 70 ||
                V208Economy.XpNeeded(2) != 120 ||
                V208Economy.XpNeeded(3) != 180 ||
                V208Economy.XpNeeded(4) != 250)
                errors.Add("v208 XP table invariant failed.");

            if (V208Economy.SkillApCost(0) != 2 ||
                V208Economy.SkillApCost(4) != 6)
                errors.Add("v208 skill AP cost invariant failed.");

            string summary = $"Errors: {errors.Count}\nWarnings: {warnings.Count}\n\n";

            if (errors.Count > 0)
            {
                Debug.LogError("SBW2 FIRST OPEN DIAGNOSTICS FAILED\n" + string.Join("\n", errors));
                if (warnings.Count > 0) Debug.LogWarning(string.Join("\n", warnings));
                EditorUtility.DisplayDialog("SBW2 First Open", summary + "FAILED — see Console.", "OK");
                return;
            }

            if (warnings.Count > 0)
                Debug.LogWarning("SBW2 FIRST OPEN WARNINGS\n" + string.Join("\n", warnings));

            Debug.Log("SBW2 FIRST OPEN DIAGNOSTICS PASS");
            EditorUtility.DisplayDialog(
                "SBW2 First Open",
                summary + "Core data and v208 economy invariants passed.\n\nNext: run SBW2 > Run Preflight Validation.",
                "OK"
            );
        }
    }
}
#endif
