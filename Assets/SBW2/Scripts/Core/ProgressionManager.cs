using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SBW2.Data;

namespace SBW2.Core
{
    public class ProgressionManager : MonoBehaviour
    {
        public static ProgressionManager Instance { get; private set; }

        readonly List<EquipmentItem> catalog = new List<EquipmentItem>();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildCatalog();
        }

        void BuildCatalog()
        {
            catalog.Clear();

            Add(new EquipmentItem
            {
                id = "fan", name = "Silk Fan", cost = 90,
                charisma = 3, refinement = 2
            });
            Add(new EquipmentItem
            {
                id = "comb", name = "Pearl Comb", cost = 120,
                charisma = 2, refinement = 4
            });
            Add(new EquipmentItem
            {
                id = "robe", name = "Embroidered Robe", cost = 180,
                charisma = 4, refinement = 5
            });
            Add(new EquipmentItem
            {
                id = "oil", name = "Scented Oil", cost = 150,
                charisma = 5, joy = 2
            });
            Add(new EquipmentItem
            {
                id = "pin", name = "Jade Hairpin", cost = 220,
                refinement = 6, reputation = 2
            });
            Add(new EquipmentItem
            {
                id = "sandals", name = "Lacquered Sandals", cost = 160,
                refinement = 3, constitution = 2
            });
            Add(new EquipmentItem
            {
                id = "tea", name = "Ceremonial Tea Set", cost = 260,
                refinement = 7, reputation = 3
            });
            Add(new EquipmentItem
            {
                id = "mirror", name = "Bronze Mirror", cost = 240,
                charisma = 6, joy = 2
            });
            Add(new EquipmentItem
            {
                id = "cushion", name = "Luxury Cushion", cost = 300,
                joy = 6, constitution = 2
            });
            Add(new EquipmentItem
            {
                id = "jewel", name = "Moonstone Jewel", cost = 420,
                charisma = 7, reputation = 5
            });
            Add(new EquipmentItem
            {
                id = "perfume", name = "Rare Perfume", cost = 480,
                charisma = 8, refinement = 3
            });
            Add(new EquipmentItem
            {
                id = "brocade", name = "Imperial Brocade", cost = 750,
                charisma = 8, refinement = 8, reputation = 4
            });
        }

        void Add(EquipmentItem item) => catalog.Add(item);

        public IEnumerable<EquipmentItem> Catalog => catalog;

        public EquipmentItem Item(string id) =>
            string.IsNullOrWhiteSpace(id)
                ? null
                : catalog.FirstOrDefault(x => x.id == id);

        public string ItemName(string id) =>
            Item(id)?.name ?? "—";

        public int XpRequiredForNextRank(StaffState s)
        {
            if (s == null || s.rank >= 5) return int.MaxValue;
            return V208Economy.XpNeeded(s.rank);
        }

        public int RankUpGoldCost(StaffState s)
        {
            if (s == null || s.rank >= 5) return int.MaxValue;
            return V208Economy.RankGoldCost(s.rank);
        }

        public bool RankUp(string id)
        {
            var gm = GameplayManager.Instance;
            var s = gm?.Staff(id);
            if (gm == null || s == null || !s.owned || s.rank >= 5) return false;

            int xpNeed = XpRequiredForNextRank(s);
            int goldNeed = RankUpGoldCost(s);

            if (s.xp < xpNeed || gm.State.gold < goldNeed) return false;

            s.xp -= xpNeed;
            gm.State.gold -= goldNeed;
            s.rank++;
            s.reputation = Mathf.Clamp(s.reputation + 6, 0, 100);
            s.ap += 2;

            gm.Save();
            gm.NotifyChanged();
            return true;
        }

        public bool SpendAP(string id, string skill)
        {
            var gm = GameplayManager.Instance;
            var s = gm?.Staff(id);
            if (gm == null || s == null || !s.owned || s.ap <= 0) return false;

            int current =
                skill == "charm" ? s.skills.charm :
                skill == "service" ? s.skills.service :
                skill == "stamina" ? s.skills.stamina : -1;

            if (current < 0 || current >= 5) return false;

            int cost = V208Economy.SkillApCost(current);
            if (s.ap < cost) return false;

            s.ap -= cost;

            if (skill == "charm") s.skills.charm++;
            else if (skill == "service") s.skills.service++;
            else s.skills.stamina++;

            gm.Save();
            gm.NotifyChanged();
            return true;
        }

        public int InventoryCount(string itemId)
        {
            var state = GameplayManager.Instance?.State;
            if (state?.inventory == null || string.IsNullOrWhiteSpace(itemId))
                return 0;

            var entry = state.inventory.FirstOrDefault(x => x != null && x.id == itemId);
            return Mathf.Max(0, entry?.count ?? 0);
        }

        void SetInventoryCount(string itemId, int count)
        {
            var gm = GameplayManager.Instance;
            if (gm?.State == null || string.IsNullOrWhiteSpace(itemId))
                return;

            var list = (gm.State.inventory ?? Array.Empty<InventoryItemState>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.id))
                .ToList();

            var entry = list.FirstOrDefault(x => x.id == itemId);
            int next = Mathf.Max(0, count);

            if (entry == null && next > 0)
            {
                list.Add(new InventoryItemState { id = itemId, count = next });
            }
            else if (entry != null)
            {
                entry.count = next;
                if (entry.count <= 0)
                    list.Remove(entry);
            }

            gm.State.inventory = list.ToArray();
        }

        public bool BuyItem(string itemId)
        {
            var gm = GameplayManager.Instance;
            var item = Item(itemId);

            if (gm?.State == null || item == null || gm.State.gold < item.cost)
                return false;

            gm.State.gold -= item.cost;
            SetInventoryCount(item.id, InventoryCount(item.id) + 1);

            gm.Save();
            gm.NotifyChanged();
            return true;
        }

        public string EquippedItemId(StaffState s, int slot)
        {
            if (s?.equip == null || slot < 0 || slot >= s.equip.Length)
                return null;

            return s.equip[slot];
        }

        public void ReturnAllEquipmentToInventory(StaffState s)
        {
            if (s?.equip == null) return;

            for (int i = 0; i < s.equip.Length; i++)
            {
                string id = s.equip[i];
                if (string.IsNullOrWhiteSpace(id)) continue;

                SetInventoryCount(id, InventoryCount(id) + 1);
                s.equip[i] = null;
            }
        }

        public bool EquipItem(string characterId, int slot, string itemId)
        {
            var gm = GameplayManager.Instance;
            var s = gm?.Staff(characterId);

            if (gm?.State == null ||
                s == null ||
                !s.owned ||
                slot < 0 ||
                slot >= 3)
                return false;

            if (s.equip == null || s.equip.Length != 3)
            {
                var normalized = new string[3];
                if (s.equip != null)
                    Array.Copy(s.equip, normalized, Math.Min(3, s.equip.Length));
                s.equip = normalized;
            }

            string old = s.equip[slot];

            // Selecting the already equipped item toggles the slot back to inventory.
            if (!string.IsNullOrWhiteSpace(itemId) && old == itemId)
            {
                SetInventoryCount(old, InventoryCount(old) + 1);
                s.equip[slot] = null;
                gm.Save();
                gm.NotifyChanged();
                return true;
            }

            if (!string.IsNullOrWhiteSpace(itemId))
            {
                if (Item(itemId) == null || InventoryCount(itemId) < 1)
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(old))
                SetInventoryCount(old, InventoryCount(old) + 1);

            if (!string.IsNullOrWhiteSpace(itemId))
            {
                SetInventoryCount(itemId, InventoryCount(itemId) - 1);
                s.equip[slot] = itemId;
            }
            else
            {
                s.equip[slot] = null;
            }

            gm.Save();
            gm.NotifyChanged();
            return true;
        }

        public int EffectiveCharisma(StaffState s) =>
            Effective(s, x => x.charisma, s?.charisma ?? 0);

        public int EffectiveRefinement(StaffState s) =>
            Effective(s, x => x.refinement, s?.refinement ?? 0);

        public int EffectiveConstitution(StaffState s) =>
            Effective(s, x => x.constitution, s?.constitution ?? 0);

        public int EffectiveReputation(StaffState s) =>
            Effective(s, x => x.reputation, s?.reputation ?? 0);

        public int EffectiveJoy(StaffState s) =>
            Effective(s, x => x.joy, s?.joy ?? 0);

        int Effective(
            StaffState s,
            Func<EquipmentItem, int> selector,
            int baseValue)
        {
            int total = baseValue;

            if (s?.equip != null)
            {
                foreach (string id in s.equip)
                {
                    var item = Item(id);
                    if (item != null)
                        total += selector(item);
                }
            }

            return Mathf.Clamp(total, 0, 120);
        }
    }
}
