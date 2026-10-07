using System;
using System.IO;
using UnityEngine;
using SBW2.Data;

namespace SBW2.Save
{
    public static class SaveMigrationManager
    {
        public static string BackupPath(string path) =>
            string.IsNullOrWhiteSpace(path) ? null : path + ".bak";

        public static bool TryLoadV3(string path, out GameSaveV3 save, out string message)
        {
            save = null;
            message = "";

            try
            {
                if (!File.Exists(path))
                {
                    message = "Save file not found.";
                    return false;
                }

                string json = File.ReadAllText(path);
                var probe = JsonUtility.FromJson<SchemaProbe>(json);

                if (probe == null)
                {
                    message = "Save JSON is invalid.";
                    return false;
                }

                if (probe.schema == 3)
                {
                    save = JsonUtility.FromJson<GameSaveV3>(json);
                    message = "Loaded schema 3.";
                    return save != null;
                }

                message = $"Unsupported save schema {probe.schema}.";
                return false;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        public static bool TryLoadPrimaryOrBackup(
            string path,
            out GameSaveV3 save,
            out string message,
            out bool usedBackup)
        {
            usedBackup = false;

            if (TryLoadV3(path, out save, out message))
                return true;

            string primaryError = message;
            string backupPath = BackupPath(path);
            string backupMessage = "Backup file not found.";

            if (!string.IsNullOrWhiteSpace(backupPath) &&
                TryLoadV3(backupPath, out save, out backupMessage))
            {
                usedBackup = true;
                message =
                    "Primary save failed (" + primaryError + "). " +
                    "Recovered from rolling backup. " + backupMessage;
                return true;
            }

            message =
                "Primary save failed: " + primaryError +
                (File.Exists(backupPath ?? "")
                    ? " Backup also failed: " + backupMessage
                    : " No backup was available.");

            save = null;
            return false;
        }

        public static bool WriteWithRollingBackup(
            string path,
            string json,
            out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(path))
            {
                error = "Save path is empty.";
                return false;
            }

            string temp = path + ".tmp";
            string backup = BackupPath(path);

            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(temp, json);

                // Read the completed temp file once before touching the live save.
                if (string.IsNullOrWhiteSpace(File.ReadAllText(temp)))
                    throw new IOException("Temporary save file is empty.");

                if (File.Exists(path))
                    File.Copy(path, backup, true);

                File.Copy(temp, path, true);
                File.Delete(temp);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;

                try
                {
                    if (File.Exists(temp))
                        File.Delete(temp);
                }
                catch { }

                return false;
            }
        }

        public static string Backup(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            string backup = BackupPath(path);
            File.Copy(path, backup, true);
            return backup;
        }

        [Serializable]
        class SchemaProbe
        {
            public int schema;
        }
    }
}
