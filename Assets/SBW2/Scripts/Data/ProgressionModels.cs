using System;

namespace SBW2.Data
{
    [Serializable]
    public class SkillState
    {
        public int charm;
        public int service;
        public int stamina;
    }

    [Serializable]
    public class EquipmentItem
    {
        public string id;
        public string name;
        public int cost;
        public int charisma;
        public int refinement;
        public int constitution;
        public int reputation;
        public int joy;
    }

    [Serializable]
    public class InventoryItemState
    {
        public string id;
        public int count;
    }

    // Deprecated v0.18 migration shape. Kept for save compatibility only.
    [Serializable]
    public class EquipmentLoadout
    {
        public string outfitId;
        public string accessoryId;
    }
}
