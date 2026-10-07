using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using SBW2.Content;
using SBW2.Data;
using SBW2.Save;

namespace SBW2.Core
{
    public class GameplayManager : MonoBehaviour
    {
        [Serializable]
        class PropertyCatalog { public LegacyProperty[] properties; }

        [Serializable]
        class MediaCompleteMarker
        {
            public string source_build;
            public int physical_photo_count;
            public int active_pack_count;
            public string archive_sha256;
        }

        [Serializable]
        class BuildStamp
        {
            public string app_version;
            public string source_build;
            public string revision;
            public string channel;
        }

        [Serializable]
        class LegacyProperty
        {
            public string id;
            public string name;
            public string district;
            public int cap;
            public float appeal;
            public int upkeep;
            public int price;
        }

        public static GameplayManager Instance { get; private set; }
        public GameplayRoster Roster { get; private set; }
        public GameSaveV3 State { get; private set; }
        public event Action Changed;
        public event Action DataLoaded;
        public bool DataReady { get; private set; }
        public string DataError { get; private set; }
        public string BuildRevision { get; private set; } = "unknown";
        public string BuildChannel { get; private set; } = "unknown";
        public bool HasSaveFile =>
            File.Exists(SavePath) ||
            File.Exists(SaveMigrationManager.BackupPath(SavePath));

        readonly System.Random rng = new System.Random();
        LegacyProperty[] propertyCatalog = Array.Empty<LegacyProperty>();

        static readonly string[] LegacyInitialMarketIds =
        {
            "pebbles_flintstone_adult_21",
            "nurse_joy",
            "mrs_possible",
            "anna",
            "bulma",
            "nami"
        };

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            StartCoroutine(LoadData());
        }

        IEnumerator LoadData()
        {
            DataReady = false;
            DataError = null;

            string rosterJson = null, rosterErr = null;
            string propertyJson = null, propertyErr = null;
            string mediaJson = null, mediaErr = null;
            string buildJson = null;

            yield return StreamingAssetReader.LoadTextAsync(
                "SBW2/Data/SBW2_GAMEPLAY_ROSTER_v208.json",
                x => rosterJson = x,
                e => rosterErr = e
            );

            yield return StreamingAssetReader.LoadTextAsync(
                "SBW2/Data/v208_properties.json",
                x => propertyJson = x,
                e => propertyErr = e
            );

            yield return StreamingAssetReader.LoadTextAsync(
                "SBW2/Data/media_complete_v208.json",
                x => mediaJson = x,
                e => mediaErr = e
            );

            yield return StreamingAssetReader.LoadTextAsync(
                "SBW2/Data/build_info.json",
                x => buildJson = x,
                e => Debug.LogWarning("Build stamp unavailable: " + e)
            );

            if (!string.IsNullOrEmpty(rosterErr))
            {
                FailDataLoad("Roster: " + rosterErr);
                yield break;
            }

            if (!string.IsNullOrEmpty(propertyErr))
            {
                FailDataLoad("Properties: " + propertyErr);
                yield break;
            }

            if (!string.IsNullOrEmpty(mediaErr))
            {
                FailDataLoad("Media marker: " + mediaErr);
                yield break;
            }

            try
            {
                Roster = JsonUtility.FromJson<GameplayRoster>(rosterJson);
                var catalog = JsonUtility.FromJson<PropertyCatalog>(propertyJson);
                propertyCatalog = catalog?.properties ?? Array.Empty<LegacyProperty>();

                var media = JsonUtility.FromJson<MediaCompleteMarker>(mediaJson);
                if (media == null ||
                    media.source_build != "v208_ELSA_21" ||
                    media.physical_photo_count != 650 ||
                    media.active_pack_count != 32 ||
                    string.IsNullOrWhiteSpace(media.archive_sha256))
                {
                    throw new InvalidDataException(
                        "v208 media completion marker is invalid or incomplete."
                    );
                }

                if (!string.IsNullOrWhiteSpace(buildJson))
                {
                    var stamp = JsonUtility.FromJson<BuildStamp>(buildJson);
                    if (stamp != null)
                    {
                        BuildRevision = string.IsNullOrWhiteSpace(stamp.revision)
                            ? "unknown"
                            : stamp.revision;
                        BuildChannel = string.IsNullOrWhiteSpace(stamp.channel)
                            ? "unknown"
                            : stamp.channel;
                    }
                }
            }
            catch (Exception ex)
            {
                FailDataLoad("Invalid v208 JSON: " + ex.Message);
                yield break;
            }

            if (Roster?.characters == null || Roster.characters.Length != 126)
            {
                FailDataLoad(
                    "Gameplay roster invariant failed. Expected 126 characters, found " +
                    (Roster?.characters?.Length ?? 0) + "."
                );
                yield break;
            }

            if (propertyCatalog == null || propertyCatalog.Length != 7)
            {
                FailDataLoad(
                    "Property catalog invariant failed. Expected 7 properties, found " +
                    (propertyCatalog?.Length ?? 0) + "."
                );
                yield break;
            }

            Load();
            DataReady = true;
            DataError = null;
            DataLoaded?.Invoke();
            Changed?.Invoke();

            Debug.Log(
                "SBW2 v208 runtime data ready: " +
                Roster.characters.Length + " characters, " +
                propertyCatalog.Length + " properties."
            );
        }

        void FailDataLoad(string message)
        {
            DataReady = false;
            DataError = message;
            Debug.LogError("SBW2 data load failed: " + message);
            DataLoaded?.Invoke();
            Changed?.Invoke();
        }

        public void NotifyChanged() => Changed?.Invoke();

        StaffState CreateStaffState(GameplayCharacter c)
        {
            var s = new StaffState
            {
                id = c.id,
                price = c.price,
                salary = c.salary,
                rank = c.rank
            };

            if (c.stats != null && c.stats.Length >= 8)
            {
                s.charisma = c.stats[0];
                s.refinement = c.stats[1];
                s.constitution = c.stats[2];
                s.character = c.stats[3];
                s.reputation = c.stats[4];
                s.joy = c.stats[5];
                s.libido = c.stats[6];
                s.health = c.stats[7];
            }

            if (c.skills != null && c.skills.Length >= 3)
            {
                s.skills.charm = c.skills[0];
                s.skills.service = c.skills[1];
                s.skills.stamina = c.skills[2];
            }

            return s;
        }

        void SynchronizeSaveRoster()
        {
            if (State == null || Roster?.characters == null) return;

            var list = (State.staff ?? Array.Empty<StaffState>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.id))
                .GroupBy(x => x.id)
                .Select(x => x.First())
                .ToList();

            var ids = new HashSet<string>(list.Select(x => x.id));

            foreach (var c in Roster.characters)
            {
                if (c == null || string.IsNullOrWhiteSpace(c.id) || ids.Contains(c.id))
                    continue;

                list.Add(CreateStaffState(c));
                ids.Add(c.id);
            }

            foreach (var c in State.customCharacters ?? Array.Empty<GameplayCharacter>())
            {
                if (c == null || string.IsNullOrWhiteSpace(c.id) || ids.Contains(c.id))
                    continue;

                list.Add(CreateStaffState(c));
                ids.Add(c.id);
            }

            State.staff = list.ToArray();
        }

        public void NewGame(string mode)
        {
            int gold = mode == "easy" ? 300 : mode == "hard" ? 75 : 150;
            string starterId = mode == "easy" ? "belldandy" : mode == "hard" ? "lara_croft" : "elsa";

            var staff = new List<StaffState>();

            foreach (var c in Roster.characters)
            {
                var s = CreateStaffState(c);

                if (c.id == starterId)
                {
                    s.owned = true;
                    s.action = "work";
                    s.workplace = "alley";
                }

                staff.Add(s);
            }

            var initialProperty = MakeProperty("alley");
            var initialMission = MakeInitialMission();

            State = new GameSaveV3
            {
                manager = mode,
                gold = gold,
                staff = staff.ToArray(),
                marketIds = LegacyInitialMarketIds
                    .Where(id => id != starterId && Character(id) != null)
                    .ToArray(),
                properties = new[] { initialProperty },
                missions = new[] { initialMission },
                lastDay = new LastDayState(),
                weeklyToken = 1,
                weeklyJoker = RollWeeklyJoker(),
                specialTokens = 0,
                lastBuyerOfferDay = -999,
                buyerOffer = null
            };

            State = SaveStateNormalizer.Normalize(State);
            Save();
            Changed?.Invoke();
        }

        PropertyState MakeProperty(string id)
        {
            var p = GetLegacyProperty(id);
            if (p == null)
                return new PropertyState { id = id, name = id, owned = true };

            return new PropertyState
            {
                id = p.id,
                name = p.name,
                district = p.district,
                owned = true,
                level = 1,
                upkeep = p.upkeep,
                capacity = p.cap,
                upgradeCost = p.price,
                incomeMultiplier = p.appeal,
                appeal = p.appeal,
                purchasePrice = p.price
            };
        }

        LegacyProperty GetLegacyProperty(string id) =>
            propertyCatalog.FirstOrDefault(x => x.id == id);

        public GameplayCharacter Character(string id)
        {
            var baseCharacter = Roster?.characters?.FirstOrDefault(x => x.id == id);
            if (baseCharacter != null) return baseCharacter;

            return State?.customCharacters?
                .FirstOrDefault(x => x != null && x.id == id);
        }

        public StaffState Staff(string id) =>
            State?.staff?.FirstOrDefault(x => x.id == id);

        public IEnumerable<GameplayCharacter> Market() =>
            (State?.marketIds ?? Array.Empty<string>())
            .Select(Character)
            .Where(x => x != null && Staff(x.id)?.owned != true);

        public IEnumerable<GameplayCharacter> Owned() =>
            State?.staff?.Where(x => x.owned)
            .Select(x => Character(x.id))
            .Where(x => x != null) ?? Enumerable.Empty<GameplayCharacter>();

        void EnsureMarketCandidates()
        {
            if (State == null || Roster?.characters == null)
                return;

            var valid = (State.marketIds ?? Array.Empty<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .Where(id =>
                {
                    var character = Character(id);
                    var staff = Staff(id);
                    return character != null && staff != null && !staff.owned;
                })
                .Take(6)
                .ToList();

            if (valid.Count < 6)
            {
                var pool = Roster.characters
                    .Where(x => x != null)
                    .Where(x =>
                    {
                        var staff = Staff(x.id);
                        return staff != null &&
                               !staff.owned &&
                               !valid.Contains(x.id);
                    })
                    .ToList();

                while (pool.Count > 0 && valid.Count < 6)
                {
                    int n = rng.Next(pool.Count);
                    valid.Add(pool[n].id);
                    pool.RemoveAt(n);
                }
            }

            State.marketIds = valid.ToArray();
        }

        public int RoomCapacity(PropertyState p) =>
            p == null ? 0 : p.capacity + Mathf.Max(0, p.roomExpansion);

        public int PhotoUnlockedCount(string characterId)
        {
            var character = Character(characterId);
            int total =
                character != null && character.photo_pack_active
                    ? Mathf.Max(0, character.photo_count)
                    : 0;

            if (total <= 0) return 0;
            if (State == null) return 1;

            var entry = (State.photoUnlockCounts ?? Array.Empty<PhotoUnlockState>())
                .FirstOrDefault(x => x != null && x.id == characterId);

            int count = entry == null ? 1 : Mathf.Max(1, entry.count);
            return Mathf.Min(total, count);
        }

        public int PhotoWorkProgress(string characterId)
        {
            if (State == null || string.IsNullOrWhiteSpace(characterId))
                return 0;

            var entry = (State.photoWorkDays ?? Array.Empty<PhotoWorkDayState>())
                .FirstOrDefault(x => x != null && x.id == characterId);

            int value = entry == null ? 0 : Mathf.Max(0, entry.count);
            return value % 3;
        }

        void SetPhotoWorkProgress(string characterId, int count)
        {
            if (State == null || string.IsNullOrWhiteSpace(characterId))
                return;

            var list = (State.photoWorkDays ?? Array.Empty<PhotoWorkDayState>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.id))
                .ToList();

            var entry = list.FirstOrDefault(x => x.id == characterId);
            int next = Mathf.Max(0, count) % 3;

            if (entry == null)
                list.Add(new PhotoWorkDayState { id = characterId, count = next });
            else
                entry.count = next;

            State.photoWorkDays = list.ToArray();
        }

        void SetPhotoUnlockedCount(string characterId, int count)
        {
            if (State == null || string.IsNullOrWhiteSpace(characterId))
                return;

            var list = (State.photoUnlockCounts ?? Array.Empty<PhotoUnlockState>())
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.id))
                .ToList();

            var entry = list.FirstOrDefault(x => x.id == characterId);
            int next = Mathf.Max(1, count);

            if (entry == null)
                list.Add(new PhotoUnlockState { id = characterId, count = next });
            else
                entry.count = next;

            State.photoUnlockCounts = list.ToArray();
        }

        bool UnlockNextPhoto(StaffState staff)
        {
            if (staff == null) return false;

            var character = Character(staff.id);
            int total =
                character != null && character.photo_pack_active
                    ? Mathf.Max(0, character.photo_count)
                    : 0;

            if (total <= 0) return false;

            int before = PhotoUnlockedCount(staff.id);
            if (before >= total) return false;

            int progress = (PhotoWorkProgress(staff.id) % 3) + 1;

            if (progress < 3)
            {
                SetPhotoWorkProgress(staff.id, progress);
                return false;
            }

            SetPhotoWorkProgress(staff.id, 0);
            SetPhotoUnlockedCount(staff.id, before + 1);
            return true;
        }

        int PropertyUsage(string id) =>
            State.staff.Count(x =>
                x.owned &&
                x.action == "work" &&
                x.sickDays <= 0 &&
                x.workplace == id
            );

        PropertyState BestPropertyForNewHire()
        {
            PropertyState best = State.properties[0];
            float bestRatio = 999f;

            foreach (var p in State.properties)
            {
                int cap = RoomCapacity(p);
                int assigned = PropertyUsage(p.id);
                float ratio = assigned / (float)Mathf.Max(1, cap);

                if (assigned < cap && ratio < bestRatio)
                {
                    best = p;
                    bestRatio = ratio;
                }
            }

            return best ?? State.properties[0];
        }

        public bool Hire(string id)
        {
            var c = Character(id);
            var s = Staff(id);

            if (c == null || s == null || s.owned || State.gold < c.cost)
                return false;

            State.gold -= c.cost;
            s.owned = true;
            s.action = "rest";
            s.workplace = BestPropertyForNewHire().id;

            RefreshMarket();
            Save();
            Changed?.Invoke();
            return true;
        }

        public void SetAction(string id, string action)
        {
            var s = Staff(id);
            if (s == null || !s.owned) return;

            string[] allowed = { "work", "rest", "trainCharm", "trainRef", "trainCon" };
            if (!allowed.Contains(action)) return;

            s.action = action;
            EnsureWorkplaceAssignments();
            Save();
            Changed?.Invoke();
        }

        public void SetWorkplace(string id, string workplaceId)
        {
            var s = Staff(id);
            var p = State.properties.FirstOrDefault(x => x.id == workplaceId && x.owned);
            if (s == null || !s.owned || p == null) return;

            s.workplace = workplaceId;
            EnsureWorkplaceAssignments();
            Save();
            Changed?.Invoke();
        }


        public bool SetPrice(string id, int value)
        {
            var s = Staff(id);
            if (s == null || !s.owned) return false;

            int next = Mathf.Clamp(value, 5, 250);
            if (s.price == next) return true;

            s.price = next;
            Save();
            Changed?.Invoke();
            return true;
        }

        public PropertyState NextPropertyForPurchase()
        {
            if (propertyCatalog == null || propertyCatalog.Length == 0)
                return null;

            foreach (var legacy in propertyCatalog)
            {
                if (State == null || State.properties == null ||
                    !State.properties.Any(x => x.id == legacy.id))
                {
                    return new PropertyState
                    {
                        id = legacy.id,
                        name = legacy.name,
                        district = legacy.district,
                        owned = false,
                        level = 0,
                        upkeep = legacy.upkeep,
                        capacity = legacy.cap,
                        appeal = legacy.appeal,
                        incomeMultiplier = legacy.appeal,
                        purchasePrice = legacy.price,
                        upgradeCost = legacy.price
                    };
                }
            }

            return null;
        }

        public bool BuyNextProperty()
        {
            var next = NextPropertyForPurchase();
            if (next == null) return false;
            return BuyOrUpgradeProperty(next.id);
        }

        public bool BuyOrUpgradeProperty(string id)
        {
            var owned = State.properties.FirstOrDefault(x => x.id == id);

            if (owned != null)
                return UpgradeProperty(id, "comfort");

            var legacy = GetLegacyProperty(id);
            if (legacy == null) return false;

            int idx = Array.FindIndex(propertyCatalog, x => x.id == id);
            if (idx > 0)
            {
                string previousId = propertyCatalog[idx - 1].id;
                if (!State.properties.Any(x => x.id == previousId))
                    return false;
            }

            if (State.gold < legacy.price) return false;

            State.gold -= legacy.price;
            var list = State.properties.ToList();
            list.Add(MakeProperty(id));
            State.properties = list.ToArray();

            EnsureWorkplaceAssignments();
            Save();
            Changed?.Invoke();
            return true;
        }

        public bool UpgradeProperty(string id, string key)
        {
            var p = State.properties.FirstOrDefault(x => x.id == id);
            if (p == null) return false;

            int level =
                key == "advertising" ? p.advertising :
                key == "security" ? p.security :
                key == "comfort" ? p.comfort :
                key == "clinic" ? p.clinic :
                key == "roomQuality" ? p.roomQuality :
                key == "roomExpansion" ? p.roomExpansion : -1;

            if (level < 0 || level >= 5) return false;

            int cost = V208Economy.UpgradeCost(key, level);
            if (State.gold < cost) return false;

            State.gold -= cost;

            if (key == "advertising") p.advertising++;
            else if (key == "security") p.security++;
            else if (key == "comfort") p.comfort++;
            else if (key == "clinic") p.clinic++;
            else if (key == "roomQuality") p.roomQuality++;
            else if (key == "roomExpansion") p.roomExpansion++;

            p.level =
                1 + p.advertising + p.security + p.comfort +
                p.clinic + p.roomQuality + p.roomExpansion;

            p.capacity = GetLegacyProperty(p.id)?.cap ?? p.capacity;
            p.upgradeCost = V208Economy.UpgradeCost(key, level + 1);

            EnsureWorkplaceAssignments();
            Save();
            Changed?.Invoke();
            return true;
        }

        int[] CustomStats(string kind)
        {
            if (kind == "charisma")
                return new[] { 68, 48, 52, 34, 10, 62, 48, 100 };
            if (kind == "refinement")
                return new[] { 50, 70, 48, 36, 10, 64, 44, 100 };
            if (kind == "stamina")
                return new[] { 48, 48, 72, 32, 10, 60, 46, 100 };

            return new[] { 56, 56, 56, 34, 10, 62, 48, 100 };
        }

        public bool CreateCustomCharacter(
            string name,
            int age,
            string kind,
            out string createdId,
            out string error)
        {
            createdId = null;
            error = null;

            if (State == null)
            {
                error = "No active game.";
                return false;
            }

            name = (name ?? "").Trim();
            kind = string.IsNullOrWhiteSpace(kind) ? "balanced" : kind;

            if (State.customCount >= 5)
            {
                error = "Maximum of 5 custom characters reached.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                error = "Enter a name.";
                return false;
            }

            if (age < 21 || age > 80)
            {
                error = "Age must be between 21 and 80.";
                return false;
            }

            string[] allowedKinds =
            {
                "balanced", "charisma", "refinement", "stamina"
            };

            if (!allowedKinds.Contains(kind))
                kind = "balanced";

            createdId =
                "custom_" +
                DateTime.UtcNow.Ticks.ToString() +
                "_" +
                rng.Next(100, 1000).ToString();

            var character = new GameplayCharacter
            {
                id = createdId,
                name = name,
                inspiredBy = "",
                source = "Custom",
                profile = kind,
                age = age,
                cost = 1000,
                rank = 1,
                salary = 12,
                price = 24,
                photo_pack_active = false,
                photo_count = 0,
                runtime_var = "",
                stats = CustomStats(kind),
                skills = new[] { 0, 0, 0 }
            };

            var custom = (State.customCharacters ?? Array.Empty<GameplayCharacter>())
                .ToList();
            custom.Add(character);
            State.customCharacters = custom.ToArray();

            var staff = (State.staff ?? Array.Empty<StaffState>()).ToList();
            var staffState = CreateStaffState(character);
            staffState.owned = false;
            staffState.action = "market";
            staff.Add(staffState);
            State.staff = staff.ToArray();

            string newId = character.id;
            var market = new List<string> { newId };
            market.AddRange(
                (State.marketIds ?? Array.Empty<string>())
                .Where(x => x != newId)
            );
            State.marketIds = market.Take(6).ToArray();

            State.customCount++;
            Save();
            Changed?.Invoke();
            return true;
        }

        string RollWeeklyJoker()
        {
            string[] pool = { "health", "xp", "demand", "reputation" };
            return pool[rng.Next(pool.Length)];
        }

        public int WeeklyJokerCashValue(string kind)
        {
            if (kind == "gold") return 120;
            if (kind == "health") return 85;
            if (kind == "xp") return 95;
            if (kind == "demand") return 110;
            return 90;
        }

        public bool UseWeeklyJoker(bool takeCash)
        {
            if (State == null || State.weeklyToken < 1)
                return false;

            string kind = string.IsNullOrWhiteSpace(State.weeklyJoker)
                ? "health"
                : State.weeklyJoker;

            State.weeklyToken = 0;

            if (takeCash)
            {
                State.gold += WeeklyJokerCashValue(kind);
            }
            else
            {
                var owned = State.staff.Where(x => x != null && x.owned).ToArray();

                if (kind == "gold")
                {
                    State.gold += 120;
                }
                else if (kind == "health")
                {
                    foreach (var s in owned)
                        s.health = Mathf.Clamp(s.health + 22, 0, 100);
                }
                else if (kind == "xp")
                {
                    foreach (var s in owned)
                        s.xp += 25;
                }
                else if (kind == "demand")
                {
                    State.demandBoost = Mathf.Max(State.demandBoost, 5);
                }
                else if (kind == "reputation")
                {
                    foreach (var s in owned)
                        s.reputation = Mathf.Clamp(s.reputation + 3, 0, 100);
                }
            }

            Save();
            Changed?.Invoke();
            return true;
        }

        IEnumerable<StaffState> PremiumRewardCandidates()
        {
            if (State?.staff == null)
                return Enumerable.Empty<StaffState>();

            return State.staff.Where(s =>
            {
                if (s == null || s.owned) return false;
                var c = Character(s.id);
                return c != null && (s.rank >= 4 || c.cost >= 2400);
            });
        }

        public int PremiumCandidateCount() =>
            PremiumRewardCandidates().Count();

        public bool UsePremiumCard()
        {
            if (State == null || State.specialTokens < 1)
                return false;

            var pool = PremiumRewardCandidates().ToArray();
            if (pool.Length == 0)
                return false;

            var s = pool[rng.Next(pool.Length)];
            State.specialTokens--;

            s.owned = true;
            s.action = "rest";
            s.sickDays = 0;
            s.workplace = BestPropertyForNewHire().id;

            EnsureWorkplaceAssignments();
            RefreshMarket();
            Save();
            Changed?.Invoke();
            return true;
        }

        public int GirlValue(string id)
        {
            var c = Character(id);
            if (c == null) return 0;
            return Mathf.Max(100, Mathf.RoundToInt(c.cost));
        }

        bool SellStaffCore(string id, int amount)
        {
            var s = Staff(id);
            if (State == null || s == null || !s.owned)
                return false;

            int gain = Mathf.Max(0, amount);

            var pm = ProgressionManager.Instance;
            if (pm != null)
                pm.ReturnAllEquipmentToInventory(s);

            s.owned = false;
            s.action = "market";
            s.sickDays = 0;
            State.gold += gain;

            if (State.buyerOffer != null && State.buyerOffer.id == id)
                State.buyerOffer = null;

            State.marketIds = (State.marketIds ?? Array.Empty<string>())
                .Where(x => x != id)
                .ToArray();

            EnsureWorkplaceAssignments();
            RefreshMarket();
            Save();
            Changed?.Invoke();
            return true;
        }

        public bool SellStaff(string id) =>
            SellStaffCore(id, GirlValue(id));

        void MaybeBuyerOffer()
        {
            if (State == null || State.buyerOffer != null)
                return;

            if ((State.day - State.lastBuyerOfferDay) < 12)
                return;

            if (rng.NextDouble() >= 0.03)
                return;

            var owned = State.staff.Where(x => x != null && x.owned).ToArray();
            if (owned.Length == 0)
                return;

            var s = owned[rng.Next(owned.Length)];
            int value = GirlValue(s.id);
            int offer = Mathf.Max(
                value + 10,
                Mathf.RoundToInt(
                    value * (1.10f + (float)rng.NextDouble() * 0.16f)
                )
            );

            State.lastBuyerOfferDay = State.day;
            State.buyerOffer = new BuyerOfferState
            {
                id = s.id,
                value = value,
                offer = offer
            };
        }

        public bool AcceptBuyerOffer()
        {
            if (State?.buyerOffer == null)
                return false;

            string id = State.buyerOffer.id;
            int offer = State.buyerOffer.offer;
            State.buyerOffer = null;
            return SellStaffCore(id, offer);
        }

        public bool DeclineBuyerOffer()
        {
            if (State?.buyerOffer == null)
                return false;

            State.buyerOffer = null;
            Save();
            Changed?.Invoke();
            return true;
        }

        public void RefreshMarket()
        {
            if (Roster?.characters == null || State == null) return;

            var cheap = new List<GameplayCharacter>();
            var mid = new List<GameplayCharacter>();
            var premium = new List<GameplayCharacter>();

            foreach (var c in Roster.characters)
            {
                var s = Staff(c.id);
                if (s == null || s.owned) continue;

                if (c.cost <= 900) cheap.Add(c);
                else if (c.cost <= 1700) mid.Add(c);
                else premium.Add(c);
            }

            var picked = new List<string>();
            TakeRandom(cheap, 2, picked);
            TakeRandom(mid, 2, picked);

            var rest = cheap.Concat(mid).Concat(premium).ToList();

            while (rest.Count > 0 && picked.Count < 6)
            {
                int n = rng.Next(rest.Count);
                var c = rest[n];
                rest.RemoveAt(n);
                if (!picked.Contains(c.id))
                    picked.Add(c.id);
            }

            State.marketIds = picked.ToArray();
        }

        void TakeRandom(List<GameplayCharacter> pool, int count, List<string> output)
        {
            while (pool.Count > 0 && count > 0 && output.Count < 6)
            {
                int n = rng.Next(pool.Count);
                output.Add(pool[n].id);
                pool.RemoveAt(n);
                count--;
            }
        }

        void EnsureWorkplaceAssignments()
        {
            if (State?.properties == null || State.properties.Length == 0) return;

            var used = State.properties.ToDictionary(x => x.id, x => 0);
            string starterId =
                State.manager == "easy" ? "belldandy" :
                State.manager == "hard" ? "lara_croft" :
                "elsa";

            var owned = State.staff
                .Where(x => x.owned)
                .OrderByDescending(x => x.id == starterId)
                .ToArray();

            foreach (var s in owned)
            {
                if (s.action != "work" || s.sickDays > 0) continue;

                var current = State.properties.FirstOrDefault(x => x.id == s.workplace);
                if (current != null && used[current.id] < RoomCapacity(current))
                {
                    used[current.id]++;
                    continue;
                }

                var next = State.properties.FirstOrDefault(x => used[x.id] < RoomCapacity(x));
                if (next != null)
                {
                    s.workplace = next.id;
                    used[next.id]++;
                }
            }

            foreach (var s in owned)
                if (!State.properties.Any(x => x.id == s.workplace))
                    s.workplace = State.properties[0].id;
        }

        float AbsenceChance(StaffState p)
        {
            float joy = ProgressionManager.Instance != null
                ? ProgressionManager.Instance.EffectiveJoy(p)
                : p.joy;

            float v = (p.character * 0.64f - joy * 0.41f) / 120f;
            return Mathf.Clamp(v, 0.01f, 0.38f);
        }

        int Customers(StaffState p, PropertyState prop)
        {
            var pm = ProgressionManager.Instance;
            float charisma = pm != null ? pm.EffectiveCharisma(p) : p.charisma;
            float refinement = pm != null ? pm.EffectiveRefinement(p) : p.refinement;
            float reputation = pm != null ? pm.EffectiveReputation(p) : p.reputation;

            float attraction =
                charisma * 0.31f +
                refinement * 0.27f +
                reputation * 0.19f +
                p.rank * 5 +
                p.skills.charm * 3;

            float ads = 1 + prop.advertising * 0.13f;
            float comfort = 1 + prop.comfort * 0.06f;
            float room = 1 + prop.roomQuality * 0.05f;
            float demand = State.demandBoost > 0 ? 1.32f : 1.08f;

            float pressure = Mathf.Clamp(
                1.55f - p.price / Mathf.Max(35f, attraction * 0.90f),
                0.42f,
                1.24f
            );

            float health = Mathf.Clamp(p.health / 100f, 0.30f, 1f);
            float streetFlow = prop.id == "alley" ? 1.04f : 1.10f;
            float random = 0.88f + (float)rng.NextDouble() * 0.34f;

            return Mathf.Clamp(
                Mathf.FloorToInt(
                    (attraction / 15.2f) *
                    prop.appeal *
                    ads *
                    comfort *
                    room *
                    demand *
                    pressure *
                    health *
                    streetFlow *
                    random
                ),
                0,
                36
            );
        }

        bool MaybeSick(StaffState p, bool training)
        {
            float h = Mathf.Clamp(p.health, 0, 100);
            float con = ProgressionManager.Instance != null
                ? ProgressionManager.Instance.EffectiveConstitution(p)
                : p.constitution;

            float risk =
                (training ? 0.018f : 0.012f) +
                Mathf.Max(0, (38 - h) / 520f) +
                Mathf.Max(0, (45 - con) / 1100f);

            if (rng.NextDouble() < risk)
            {
                p.sickDays = rng.Next(1, 4);
                State.sickEpisodes++;
                return true;
            }

            return false;
        }

        (int revenue, int customers, bool worked, string outcome) Perform(
            StaffState p,
            PropertyState prop,
            bool hasRoom)
        {
            if (p.sickDays > 0)
            {
                p.sickDays--;
                p.health = Mathf.Clamp(p.health + 7 + prop.clinic * 4, 0, 100);
                p.joy = Mathf.Clamp(p.joy + 2, 0, 100);
                return (0, 0, false, "sick");
            }

            if (p.action == "rest")
            {
                int con = ProgressionManager.Instance != null
                    ? ProgressionManager.Instance.EffectiveConstitution(p)
                    : p.constitution;

                int heal = 8 + Mathf.FloorToInt(con / 18f) + prop.clinic * 3;
                p.health = Mathf.Clamp(p.health + heal, 0, 100);
                p.joy = Mathf.Clamp(p.joy + 5, 0, 100);
                return (0, 0, false, "rest");
            }

            if (p.action == "trainCharm")
            {
                p.charisma = Mathf.Clamp(p.charisma + 1 + rng.Next(0, 2), 0, 100);
                p.joy = Mathf.Clamp(p.joy - 1, 0, 100);
                p.xp += 5;
                MaybeSick(p, true);
                return (0, 0, false, "trainCharm");
            }

            if (p.action == "trainRef")
            {
                p.refinement = Mathf.Clamp(p.refinement + 1 + rng.Next(0, 2), 0, 100);
                p.joy = Mathf.Clamp(p.joy - 1, 0, 100);
                p.xp += 5;
                MaybeSick(p, true);
                return (0, 0, false, "trainRef");
            }

            if (p.action == "trainCon")
            {
                p.constitution = Mathf.Clamp(p.constitution + 1 + rng.Next(0, 2), 0, 100);
                p.health = Mathf.Clamp(p.health - 2, 0, 100);
                p.xp += 5;
                MaybeSick(p, true);
                return (0, 0, false, "trainCon");
            }

            if (!hasRoom)
                return (0, 0, false, "noRoom");

            if (p.health <= 10)
            {
                p.health = Mathf.Clamp(p.health + 10 + prop.clinic * 3, 0, 100);
                return (0, 0, false, "lowHealth");
            }

            if (rng.NextDouble() < AbsenceChance(p))
            {
                p.joy = Mathf.Clamp(p.joy - 2, 0, 100);
                return (0, 0, false, "absent");
            }

            int customers = Customers(p, prop);
            int gross = Mathf.RoundToInt(
                customers *
                p.price *
                V208Economy.ManagerIncome(State.manager) *
                (1 + p.skills.service * 0.025f)
            );

            int conEff = ProgressionManager.Instance != null
                ? ProgressionManager.Instance.EffectiveConstitution(p)
                : p.constitution;

            int fatigue = Mathf.Max(
                2,
                Mathf.CeilToInt(
                    customers *
                    (1.28f - conEff / 125f - p.skills.stamina * 0.025f)
                )
            );

            p.health = Mathf.Clamp(p.health - fatigue, 0, 100);
            p.joy = Mathf.Clamp(
                p.joy - Mathf.CeilToInt(customers / 8f),
                0,
                100
            );
            p.xp += V208Economy.WorkXp(customers);
            p.ap += V208Economy.WorkAp(customers);
            State.daysWorked++;

            MaybeSick(p, false);
            return (gross, customers, true, "work");
        }

        void Incident()
        {
            if (State.day < 30) return;

            float avgSecurity = State.properties.Length == 0
                ? 0f
                : (float)State.properties.Average(x => x.security);

            float risk =
                0.075f *
                V208Economy.ManagerEvent(State.manager) *
                Mathf.Max(0.48f, 1 - avgSecurity * 0.11f);

            if (rng.NextDouble() > risk) return;

            int k = rng.Next(1, 7);
            var owned = State.staff.Where(x => x.owned).ToArray();

            if (k == 1)
            {
                int loss = Mathf.Min(State.gold, rng.Next(20, 81));
                State.gold -= loss;
                State.lastDay.incident = $"Property damage: -{loss} Gold";
            }
            else if (k == 2)
            {
                int gain = rng.Next(25, 91);
                State.gold += gain;
                State.lastDay.incident = $"Large tip: +{gain} Gold";
            }
            else if (k == 3 && owned.Length > 0)
            {
                var p = owned[rng.Next(owned.Length)];
                p.health = Mathf.Clamp(p.health - rng.Next(5, 15), 0, 100);

                if (rng.NextDouble() < 0.35)
                {
                    p.sickDays = Mathf.Max(p.sickDays, 2);
                    State.sickEpisodes++;
                    State.lastDay.incident = $"{Character(p.id)?.name} became sick after an incident.";
                }
                else
                {
                    State.lastDay.incident = $"{Character(p.id)?.name} ended the day exhausted.";
                }
            }
            else if (k == 4)
            {
                State.demandBoost = Mathf.Max(State.demandBoost, 3);
                State.lastDay.incident = "District festival: demand boosted for 3 days.";
            }
            else if (k == 5 && owned.Length > 0)
            {
                var p = owned[rng.Next(owned.Length)];
                p.reputation = Mathf.Clamp(p.reputation + 3, 0, 100);
                State.lastDay.incident = $"{Character(p.id)?.name} gained reputation.";
            }
            else if (k == 6)
            {
                int loss = Mathf.Min(State.gold, rng.Next(10, 46));
                State.gold -= loss;
                State.lastDay.incident = $"Minor vandalism: -{loss} Gold";
            }
        }

        MissionState MakeInitialMission()
        {
            int k = rng.Next(1, 5);

            if (k == 1) return Mission("gold", 500, 30, "End the month with 500 Gold");
            if (k == 2) return MissionDelta("revenue", 700, 30, "Generate 700 Gold in revenue this month");
            if (k == 3) return MissionDelta("customers", 24, 30, "Serve 24 clients this month");
            return Mission("owned", 2, 30, "Have 2 hired girls");
        }

        MissionState MakeMission()
        {
            int block = Mathf.FloorToInt((State.day - 1) / 30f) + 1;
            int deadline = block * 30;
            var owned = State.staff.Where(x => x.owned).ToArray();

            int bestRank = owned.Length == 0 ? 0 : owned.Max(x => x.rank);
            int bestRep = owned.Length == 0 ? 0 : owned.Max(x => x.reputation);

            var kinds = new List<string> { "gold", "revenue", "customers", "owned", "reputation" };
            if (bestRank < 5) kinds.Add("rank");
            if (State.properties.Length < propertyCatalog.Length) kinds.Add("property");

            string previous = State.missions != null && State.missions.Length > 0
                ? State.missions[0].kind : "";

            kinds = kinds.Where(x => x != previous).ToList();
            if (kinds.Count == 0)
                kinds.AddRange(new[] { "gold", "revenue", "customers" });

            string kind = kinds[rng.Next(kinds.Count)];
            float scale = Mathf.Pow(1.09f, Mathf.Max(0, block - 1));

            if (kind == "gold")
            {
                int amount = Mathf.RoundToInt(320 * scale);
                int target = Mathf.Max(500, State.gold + amount);
                return Mission("gold", target, deadline, $"End the month with {target} Gold");
            }

            if (kind == "revenue")
            {
                int amount = Mathf.RoundToInt(700 * scale);
                return MissionDelta("revenue", amount, deadline, $"Generate {amount} Gold in revenue this month");
            }

            if (kind == "customers")
            {
                int amount = Mathf.Max(
                    24,
                    Mathf.RoundToInt(24 * scale + Mathf.Max(0, block - 1) * 2)
                );
                return MissionDelta("customers", amount, deadline, $"Serve {amount} clients this month");
            }

            if (kind == "owned")
            {
                int n = Mathf.Min(
                    Roster.characters.Length,
                    owned.Length + 1 + Mathf.FloorToInt(Mathf.Max(0, block - 1) / 4f)
                );
                return Mission("owned", n, deadline, $"Have {n} hired girls");
            }

            if (kind == "rank")
            {
                int n = Mathf.Min(
                    5,
                    Mathf.Max(
                        bestRank + 1,
                        2 + Mathf.FloorToInt(Mathf.Max(0, block - 1) / 3f)
                    )
                );
                return Mission("rank", n, deadline, $"Have one rank {n} girl");
            }

            if (kind == "property")
            {
                int n = Mathf.Min(propertyCatalog.Length, State.properties.Length + 1);
                return Mission("property", n, deadline, $"Own {n} properties");
            }

            int rep = Mathf.Min(
                98,
                Mathf.Max(
                    bestRep + 2,
                    68 + Mathf.FloorToInt(Mathf.Max(0, block - 1) * 1.5f)
                )
            );

            return Mission("reputation", rep, deadline, $"Raise one girl's reputation to {rep}");
        }

        MissionState Mission(string kind, int target, int deadline, string label) =>
            new MissionState
            {
                id = "monthly",
                kind = kind,
                target = target,
                deadline = deadline,
                label = label,
                title = "Monthly Mission",
                description = label
            };

        MissionState MissionDelta(string kind, int amount, int deadline, string label) =>
            new MissionState
            {
                id = "monthly",
                kind = kind,
                startValue = kind == "revenue" ? State?.totalRevenue ?? 0 : State?.totalCustomers ?? 0,
                amount = amount,
                target = (kind == "revenue" ? State?.totalRevenue ?? 0 : State?.totalCustomers ?? 0) + amount,
                deadline = deadline,
                label = label,
                title = "Monthly Mission",
                description = label
            };

        bool MissionSatisfied(MissionState m)
        {
            if (m == null) return false;

            var owned = State.staff.Where(x => x.owned).ToArray();

            if (m.kind == "gold") return State.gold >= m.target;
            if (m.kind == "owned") return owned.Length >= m.target;
            if (m.kind == "rank") return owned.Any(x => x.rank >= m.target);
            if (m.kind == "property") return State.properties.Length >= m.target;
            if (m.kind == "revenue") return State.totalRevenue - m.startValue >= (m.amount > 0 ? m.amount : m.target);
            if (m.kind == "customers") return State.totalCustomers - m.startValue >= (m.amount > 0 ? m.amount : m.target);
            if (m.kind == "reputation") return owned.Any(x => x.reputation >= m.target);

            return false;
        }

        void UpdateMissionProgress()
        {
            if (State.missions == null || State.missions.Length == 0) return;

            var m = State.missions[0];
            var owned = State.staff.Where(x => x.owned).ToArray();

            if (m.kind == "gold") m.progress = State.gold;
            else if (m.kind == "owned") m.progress = owned.Length;
            else if (m.kind == "property") m.progress = State.properties.Length;
            else if (m.kind == "rank") m.progress = owned.Length == 0 ? 0 : owned.Max(x => x.rank);
            else if (m.kind == "revenue") m.progress = Mathf.Max(0, State.totalRevenue - m.startValue);
            else if (m.kind == "customers") m.progress = Mathf.Max(0, State.totalCustomers - m.startValue);
            else if (m.kind == "reputation") m.progress = owned.Length == 0 ? 0 : owned.Max(x => x.reputation);
        }

        public int CampaignScore()
        {
            var owned = State.staff.Where(x => x.owned).ToArray();
            int maxRank = owned.Length == 0 ? 0 : owned.Max(x => x.rank);

            int missionPts = State.missionsCompleted * 4;
            int propertyPts = Mathf.Max(0, State.properties.Length - 1) * 3;
            int rankPts = maxRank * 2;
            int staffPts = Mathf.Min(10, Mathf.FloorToInt(owned.Length / 2f));

            int economyPts =
                State.gold >= 15000 ? 8 :
                State.gold >= 7500 ? 6 :
                State.gold >= 3000 ? 4 :
                State.gold >= 1000 ? 2 : 0;

            int clientPts =
                State.totalCustomers >= 1500 ? 8 :
                State.totalCustomers >= 750 ? 6 :
                State.totalCustomers >= 300 ? 4 :
                State.totalCustomers >= 100 ? 2 : 0;

            return missionPts + propertyPts + rankPts + staffPts + economyPts + clientPts;
        }

        void FinishCampaign()
        {
            if (State.campaignEnded || State.endless) return;

            int score = CampaignScore();
            int tier = score >= 50 ? 3 : score >= 28 ? 2 : 1;

            State.campaignEnded = true;
            State.endingScore = score;
            State.endingTitle =
                tier == 3 ? "Empire of Lanterns" :
                tier == 2 ? "Respected House" :
                "A New Beginning";
        }

        public void EnableEndless()
        {
            State.endless = true;
            State.campaignEnded = false;
            Save();
            Changed?.Invoke();
        }

        public void EndDay()
        {
            if (State == null || (State.campaignEnded && !State.endless)) return;
            if (!State.endless && State.day > State.maxDay) return;

            EnsureWorkplaceAssignments();

            int revenue = 0;
            int customers = 0;
            int costs = 0;
            var dayResults = new List<DailyStaffResult>();
            var used = State.properties.ToDictionary(x => x.id, x => 0);

            foreach (var prop in State.properties)
                costs += prop.upkeep;

            foreach (var p in State.staff.Where(x => x.owned))
            {
                var prop = State.properties.FirstOrDefault(x => x.id == p.workplace)
                           ?? State.properties[0];

                bool hasRoom = true;

                if (p.action == "work")
                {
                    PropertyState chosen = null;

                    if (used[prop.id] < RoomCapacity(prop))
                        chosen = prop;
                    else
                        chosen = State.properties.FirstOrDefault(x => used[x.id] < RoomCapacity(x));

                    hasRoom = chosen != null;

                    if (chosen != null)
                    {
                        prop = chosen;
                        p.workplace = chosen.id;
                        used[chosen.id]++;
                    }
                }

                var result = Perform(p, prop, hasRoom);

                bool photoUnlocked = false;
                if (result.worked)
                    photoUnlocked = UnlockNextPhoto(p);

                var character = Character(p.id);
                dayResults.Add(new DailyStaffResult
                {
                    id = p.id,
                    name = character?.name ?? p.id,
                    action = p.action,
                    workplace = prop.name ?? prop.id,
                    outcome = result.outcome,
                    revenue = result.revenue,
                    customers = result.customers,
                    health = p.health,
                    joy = p.joy,
                    photoUnlocked = photoUnlocked,
                    photoCount = PhotoUnlockedCount(p.id),
                    photoTotal = character?.photo_count ?? 0,
                    photoProgress = PhotoWorkProgress(p.id)
                });

                revenue += result.revenue;
                customers += result.customers;
                costs += p.salary;
            }

            foreach (var prop in State.properties)
            {
                costs +=
                    prop.advertising * 2 +
                    prop.security * 2 +
                    prop.comfort * 2 +
                    prop.clinic * 2 +
                    prop.roomQuality * 2 +
                    prop.roomExpansion * 3;
            }

            costs = Mathf.RoundToInt(
                costs *
                V208Economy.ManagerCost(State.manager) *
                V208Economy.GameDifficultyFactor
            );

            State.gold += revenue - costs;

            State.lastDay = new LastDayState
            {
                revenue = revenue,
                costs = costs,
                profit = revenue - costs,
                customers = customers,
                incident = "",
                staffResults = dayResults.ToArray()
            };

            State.totalRevenue += revenue;
            State.totalCosts += costs;
            State.totalCustomers += customers;

            Incident();

            if (State.demandBoost > 0)
                State.demandBoost--;

            State.day++;

            if ((State.day - 1) % 7 == 0)
            {
                State.weeklyToken = 1;
                State.weeklyJoker = RollWeeklyJoker();
            }

            RefreshMarket();

            if (State.missions != null &&
                State.missions.Length > 0 &&
                State.day - 1 == State.missions[0].deadline)
            {
                if (MissionSatisfied(State.missions[0]))
                {
                    State.missionsCompleted++;
                    State.specialTokens++;
                    State.missions[0].completed = true;
                }

                if (State.day <= State.maxDay)
                    State.missions = new[] { MakeMission() };
            }

            UpdateMissionProgress();
            MaybeBuyerOffer();

            if (!State.endless && State.day > State.maxDay)
                FinishCampaign();

            Save();
            Changed?.Invoke();
        }

        public bool Load()
        {
            if (!File.Exists(SavePath) &&
                !File.Exists(SaveMigrationManager.BackupPath(SavePath)))
                return false;

            if (SaveMigrationManager.TryLoadPrimaryOrBackup(
                    SavePath,
                    out var loaded,
                    out var message,
                    out var usedBackup))
            {
                State = SaveStateNormalizer.Normalize(loaded);
                SynchronizeSaveRoster();

                if (State.properties == null || State.properties.Length == 0)
                    State.properties = new[] { MakeProperty("alley") };

                if (State.missions == null || State.missions.Length == 0)
                    State.missions = new[] { MakeInitialMission() };

                EnsureWorkplaceAssignments();
                EnsureMarketCandidates();

                if (string.IsNullOrWhiteSpace(State.weeklyJoker))
                    State.weeklyJoker = RollWeeklyJoker();

                UpdateMissionProgress();

                if (usedBackup)
                {
                    Debug.LogWarning(message);

                    // Preserve the known-good rolling backup while replacing
                    // the corrupt primary save recovered above.
                    try
                    {
                        if (File.Exists(SavePath))
                            File.Delete(SavePath);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(
                            "Could not remove corrupt primary save before recovery: " +
                            ex.Message
                        );
                    }

                    Save();
                }
                else
                {
                    Debug.Log(message);
                }

                Changed?.Invoke();
                return true;
            }

            Debug.LogWarning("Save load failed: " + message);
            return false;
        }

        public void DeleteSave()
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);

            string backup = SaveMigrationManager.BackupPath(SavePath);
            if (!string.IsNullOrWhiteSpace(backup) && File.Exists(backup))
                File.Delete(backup);

            string temp = SavePath + ".tmp";
            if (File.Exists(temp))
                File.Delete(temp);

            State = null;
            Changed?.Invoke();
        }

        public string SaveFilePath => SavePath;

        string SavePath =>
            Path.Combine(Application.persistentDataPath, "sbw2_unity_save_v3.json");

        public void Save()
        {
            if (State == null) return;

            string json = JsonUtility.ToJson(State, true);

            if (!SaveMigrationManager.WriteWithRollingBackup(
                    SavePath,
                    json,
                    out var error))
            {
                Debug.LogError("SBW2 save failed: " + error);
            }
        }
    }
}
