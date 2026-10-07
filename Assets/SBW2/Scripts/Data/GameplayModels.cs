using System;
using SBW2.Data;

namespace SBW2.Data
{
    [Serializable] public class GameplayRoster { public GameplayCharacter[] characters; }

    [Serializable]
    public class GameplayCharacter : CharacterData
    {
        public int[] stats;
        public int[] skills;
    }

    [Serializable]
    public class StaffState
    {
        public string id;
        public bool owned;
        public string action = "market";
        public string workplace = "alley";
        public int price;
        public int salary;
        public int rank;
        public int health = 100;
        public int joy = 60;
        public int xp;
        public int charisma;
        public int refinement;
        public int constitution;
        public int reputation;
        public int absentDays;
        public int sickDays;
        public int character;
        public int libido;

        // Progression fields required by ProgressionManager/UI.
        public int ap;
        public SkillState skills = new SkillState();

        // v208 exact equipment: three generic slots drawing from shared inventory.
        public string[] equip = new string[3];

        // Kept only so pre-v0.19 migration saves deserialize without loss/crash.
        public EquipmentLoadout equipment = new EquipmentLoadout();
    }

    [Serializable]
    public class PropertyState
    {
        public string id;
        public string name;
        public string district;
        public bool owned;
        public int level;
        public int upkeep;
        public int capacity;
        public int upgradeCost;
        public float incomeMultiplier = 1f;

        // v208 exact upgrade tracks
        public int advertising;
        public int security;
        public int comfort;
        public int clinic;
        public int roomQuality;
        public int roomExpansion;

        public float appeal = 1f;
        public int purchasePrice;
    }

    [Serializable]
    public class MissionState
    {
        public string id;
        public string kind;
        public string title;
        public string description;
        public string label;
        public bool completed;
        public int rewardGold;
        public int target;
        public int progress;
        public int deadline;
        public int startValue;
        public int amount;
    }

    [Serializable]
    public class IncidentState
    {
        public string title;
        public string description;
        public int goldDelta;
        public string characterId;
    }

    [Serializable]
    public class PhotoUnlockState
    {
        public string id;
        public int count;
    }

    [Serializable]
    public class PhotoWorkDayState
    {
        public string id;
        public int count;
    }

    [Serializable]
    public class BuyerOfferState
    {
        public string id;
        public int value;
        public int offer;
    }

    [Serializable]
    public class DailyStaffResult
    {
        public string id;
        public string name;
        public string action;
        public string workplace;
        public string outcome;
        public int revenue;
        public int customers;
        public int health;
        public int joy;
        public bool photoUnlocked;
        public int photoCount;
        public int photoTotal;
        public int photoProgress;
    }

    [Serializable]
    public class LastDayState
    {
        public int revenue;
        public int costs;
        public int profit;
        public int customers;
        public string incident;
        public DailyStaffResult[] staffResults = Array.Empty<DailyStaffResult>();
    }

    [Serializable]
    public class GameSaveV3
    {
        public int schema = 3;
        public string source_build = "v208_ELSA_21";
        public string manager = "normal";
        public int day = 1;
        public int maxDay = 300;
        public int gold = 150;
        public StaffState[] staff = Array.Empty<StaffState>();
        public GameplayCharacter[] customCharacters = Array.Empty<GameplayCharacter>();
        public int customCount;
        public string[] marketIds = Array.Empty<string>();
        public PropertyState[] properties = Array.Empty<PropertyState>();
        public MissionState[] missions = Array.Empty<MissionState>();
        public LastDayState lastDay = new LastDayState();
        public InventoryItemState[] inventory = Array.Empty<InventoryItemState>();
        public PhotoUnlockState[] photoUnlockCounts = Array.Empty<PhotoUnlockState>();
        public PhotoWorkDayState[] photoWorkDays = Array.Empty<PhotoWorkDayState>();

        // v208 weekly/monthly management systems.
        public int weeklyToken = 1;
        public string weeklyJoker = "health";
        public int specialTokens;
        public int lastBuyerOfferDay = -999;
        public BuyerOfferState buyerOffer;

        // v208 campaign/economy state
        public int totalRevenue;
        public int totalCosts;
        public int totalCustomers;
        public int sickEpisodes;
        public int daysWorked;
        public int missionsCompleted;
        public int demandBoost;
        public bool endless;
        public bool campaignEnded;
        public string endingTitle;
        public int endingScore;
    }
}
