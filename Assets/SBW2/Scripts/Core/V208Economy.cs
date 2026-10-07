using System;
using UnityEngine;
using SBW2.Data;

namespace SBW2.Core
{
    public static class V208Economy
    {
        public const float GameDifficultyFactor = 1.10f;

        public static int XpNeeded(int rank)
        {
            switch (rank)
            {
                case 1: return 70;
                case 2: return 120;
                case 3: return 180;
                case 4: return 250;
                default: return 0;
            }
        }

        public static int RankGoldCost(int rank) =>
            Mathf.Max(0, Mathf.RoundToInt(rank * 45f * GameDifficultyFactor));

        public static int SkillApCost(int level) =>
            2 + Mathf.Max(0, level);

        public static int WorkXp(int customers) =>
            2 + Mathf.RoundToInt(Mathf.Max(0, customers) * 0.8f);

        public static int WorkAp(int customers) =>
            customers >= 18 ? 2 : customers >= 8 ? 1 : 0;

        public static float ManagerIncome(string mode) =>
            mode == "easy" ? 1.14f : mode == "hard" ? 0.90f : 1f;

        public static float ManagerCost(string mode) =>
            mode == "easy" ? 0.90f : mode == "hard" ? 1.13f : 1f;

        public static float ManagerEvent(string mode) =>
            mode == "easy" ? 0.82f : mode == "hard" ? 1.22f : 1f;

        public static int UpgradeCost(string key, int currentLevel)
        {
            int baseCost = key == "roomExpansion" ? 260 :
                           key == "roomQuality" ? 180 : 130;
            int n = currentLevel + 1;
            return baseCost * n * n;
        }

        public static int RecommendedPrice(
            int rank,
            int charisma,
            int refinement,
            int reputation,
            int serviceSkill)
        {
            return Mathf.Max(
                5,
                Mathf.RoundToInt(
                    7 +
                    rank * 6 +
                    charisma * 0.22f +
                    refinement * 0.20f +
                    reputation * 0.18f +
                    serviceSkill * 2
                )
            );
        }

        public static int RecommendedPrice(StaffState p)
        {
            if (p == null) return 5;

            return RecommendedPrice(
                p.rank,
                p.charisma,
                p.refinement,
                p.reputation,
                p.skills?.service ?? 0
            );
        }
    }
}
