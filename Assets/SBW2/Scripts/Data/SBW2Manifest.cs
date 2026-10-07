using System;

namespace SBW2.Data
{
    [Serializable]
    public class SBW2Manifest
    {
        public string project;
        public string current_build;
        public string game_file;
        public string source_backup;
        public int roster_character_count;
        public int active_photo_pack_count;
        public int active_photo_total;
        public PendingIntegration pending_next_integration;
        public CharacterData[] characters;
    }

    [Serializable]
    public class PendingIntegration
    {
        public string character_id;
        public string name;
        public int uploaded_images;
        public string suggested_next_build;
    }

    [Serializable]
    public class CharacterData
    {
        public string id;
        public string name;
        public string inspiredBy;
        public string source;
        public string profile;
        public int age;
        public int cost;
        public int rank;
        public int salary;
        public int price;
        public bool photo_pack_active;
        public int photo_count;
        public string runtime_var;
        public string note;
    }

    [Serializable]
    public class CharacterPackManifest
    {
        public string character_id;
        public string name;
        public int expected_photo_count;
        public string source_build;
        public string runtime_var_legacy;
        public string[] photos;
        public string note;
    }
}
