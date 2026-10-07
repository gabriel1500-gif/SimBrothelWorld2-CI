using System;
using System.Linq;
using UnityEngine;
using SBW2.Data;

namespace SBW2.Save
{
    public static class SaveStateNormalizer
    {
        public static GameSaveV3 Normalize(GameSaveV3 save)
        {
            if (save == null) return null;

            if (save.staff == null) save.staff = Array.Empty<StaffState>();
            if (save.customCharacters == null)
                save.customCharacters = Array.Empty<GameplayCharacter>();
            if (save.customCount < save.customCharacters.Length)
                save.customCount = save.customCharacters.Length;
            save.customCount = Math.Max(0, Math.Min(5, save.customCount));
            if (save.marketIds == null) save.marketIds = Array.Empty<string>();
            if (save.properties == null) save.properties = Array.Empty<PropertyState>();
            if (save.missions == null) save.missions = Array.Empty<MissionState>();
            if (save.lastDay == null) save.lastDay = new LastDayState();
            if (save.lastDay.staffResults == null)
                save.lastDay.staffResults = Array.Empty<DailyStaffResult>();
            if (save.inventory == null) save.inventory = Array.Empty<InventoryItemState>();

            save.inventory = save.inventory
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.id))
                .GroupBy(x => x.id)
                .Select(g => new InventoryItemState
                {
                    id = g.Key,
                    count = Math.Max(0, g.Sum(x => Math.Max(0, x.count)))
                })
                .Where(x => x.count > 0)
                .ToArray();

            if (save.photoUnlockCounts == null)
                save.photoUnlockCounts = Array.Empty<PhotoUnlockState>();

            save.photoUnlockCounts = save.photoUnlockCounts
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.id))
                .GroupBy(x => x.id)
                .Select(g => new PhotoUnlockState
                {
                    id = g.Key,
                    count = Math.Max(1, g.Max(x => Math.Max(1, x.count)))
                })
                .ToArray();

            if (save.photoWorkDays == null)
                save.photoWorkDays = Array.Empty<PhotoWorkDayState>();

            save.photoWorkDays = save.photoWorkDays
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.id))
                .GroupBy(x => x.id)
                .Select(g => new PhotoWorkDayState
                {
                    id = g.Key,
                    count = Math.Max(0, g.Max(x => Math.Max(0, x.count))) % 3
                })
                .ToArray();

            foreach (var s in save.staff)
            {
                if (s == null) continue;
                if (s.skills == null) s.skills = new SkillState();
                if (s.equipment == null) s.equipment = new EquipmentLoadout();

                if (s.equip == null)
                {
                    s.equip = new string[3];
                }
                else if (s.equip.Length != 3)
                {
                    var normalizedEquip = new string[3];
                    Array.Copy(s.equip, normalizedEquip, Math.Min(3, s.equip.Length));
                    s.equip = normalizedEquip;
                }

                if (string.IsNullOrWhiteSpace(s.action))
                    s.action = s.owned ? "rest" : "market";

                if (string.IsNullOrWhiteSpace(s.workplace))
                    s.workplace = "alley";

                s.health = Mathf.Clamp(s.health, 0, 100);
                s.joy = Mathf.Clamp(s.joy, 0, 100);
                s.charisma = Mathf.Clamp(s.charisma, 0, 100);
                s.refinement = Mathf.Clamp(s.refinement, 0, 100);
                s.constitution = Mathf.Clamp(s.constitution, 0, 100);
                s.character = Mathf.Clamp(s.character, 0, 100);
                s.reputation = Mathf.Clamp(s.reputation, 0, 100);
                s.libido = Mathf.Clamp(s.libido, 0, 100);
            }

            foreach (var p in save.properties.Where(x => x != null))
            {
                if (p.level <= 0) p.level = 1;
                if (p.capacity < 0) p.capacity = 0;
                if (p.appeal <= 0f) p.appeal = 1f;
                if (p.incomeMultiplier <= 0f) p.incomeMultiplier = p.appeal;
            }

            if (save.weeklyToken < 0) save.weeklyToken = 0;
            if (save.specialTokens < 0) save.specialTokens = 0;

            if (save.buyerOffer != null &&
                string.IsNullOrWhiteSpace(save.buyerOffer.id))
            {
                save.buyerOffer = null;
            }

            if (save.day < 1) save.day = 1;
            if (save.maxDay <= 0) save.maxDay = 300;

            return save;
        }
    }
}
