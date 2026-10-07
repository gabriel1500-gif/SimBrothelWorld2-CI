using System;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using SBW2.Core;
using SBW2.Content;
using SBW2.Data;

namespace SBW2.UI
{
    public class MobileGameController : MonoBehaviour
    {
        [SerializeField] Text statusText;
        [SerializeField] Text titleText;
        [SerializeField] Text bodyText;
        [SerializeField] GameObject heroPanel;
        [SerializeField] RectTransform contentPanel;

        [SerializeField] RawImage heroImage;
        [SerializeField] Text heroCaption;
        [SerializeField] Text heroTitleText;
        [SerializeField] Text heroMetaText;
        [SerializeField] Text heroBadgeText;

        [SerializeField] GameObject metricRow;
        [SerializeField] Text metric1Label;
        [SerializeField] Text metric1Value;
        [SerializeField] Text metric2Label;
        [SerializeField] Text metric2Value;
        [SerializeField] Text metric3Label;
        [SerializeField] Text metric3Value;
        [SerializeField] Text metric4Label;
        [SerializeField] Text metric4Value;

        [SerializeField] Text dayChipText;
        [SerializeField] Text goldChipText;
        [SerializeField] Text difficultyChipText;
        [SerializeField] Text stageChipText;

        [SerializeField] Button homeButton;
        [SerializeField] Button marketButton;
        [SerializeField] Button staffButton;
        [SerializeField] Button propertiesButton;
        [SerializeField] Button progressionButton;
        [SerializeField] Button equipmentButton;
        [SerializeField] Button galleryButton;
        [SerializeField] Button endDayButton;
        [SerializeField] Button moreButton;
        [SerializeField] GameObject morePanel;

        [SerializeField] GameObject homePanel;
        [SerializeField] Text homeLastDayText;
        [SerializeField] Text homeMissionText;
        [SerializeField] Text homeRewardText;

        [SerializeField] Button previousButton;
        [SerializeField] Button nextButton;
        [SerializeField] Button primaryButton;
        [SerializeField] Button secondaryButton;
        [SerializeField] Button tertiaryButton;
        [SerializeField] Button quaternaryButton;
        [SerializeField] DaySummaryPanel daySummary;

        [SerializeField] GameObject customPanel;
        [SerializeField] InputField customNameInput;
        [SerializeField] InputField customAgeInput;

        string view = "home";
        string selectedCharacterId = "elsa";
        int index;
        int propertyUpgradeIndex;
        string pendingSellId;
        string customKind = "balanced";
        string customMessage = "";

        CharacterGallery gallery;
        MobileStartMenu startMenu;
        PhotoPackLoader photoLoader;

        bool mediaDiagnosticRunning;
        string mediaDiagnosticResult = "Not run";

        GameplayManager GM => GameplayManager.Instance;
        ProgressionManager PM => ProgressionManager.Instance;

        void Start()
        {
            gallery = FindAnyObjectByType<CharacterGallery>();
            startMenu = FindAnyObjectByType<MobileStartMenu>();
            photoLoader = FindAnyObjectByType<PhotoPackLoader>();

            if (customPanel != null)
                customPanel.SetActive(false);
            if (morePanel != null)
                morePanel.SetActive(false);
            if (homePanel != null)
                homePanel.SetActive(false);

            homeButton.onClick.AddListener(() => Open("home"));
            marketButton.onClick.AddListener(() => Open("market"));
            staffButton.onClick.AddListener(() => Open("staff"));
            if (moreButton != null)
                moreButton.onClick.AddListener(() => Open("more"));
            propertiesButton.onClick.AddListener(() => Open("properties"));
            progressionButton.onClick.AddListener(() => Open("progression"));
            equipmentButton.onClick.AddListener(() => Open("equipment"));
            galleryButton.onClick.AddListener(() => Open("gallery"));

            endDayButton.onClick.AddListener(() =>
            {
                if (GM?.State == null) return;

                if (GM.State.campaignEnded && !GM.State.endless)
                {
                    Open("home");
                    return;
                }

                GM.EndDay();
                Open("home");
                daySummary?.ShowLastDay();
            });

            previousButton.onClick.AddListener(Previous);
            nextButton.onClick.AddListener(Next);

            if (GM != null) GM.Changed += Render;
            Render();
        }

        void OnDestroy()
        {
            if (GM != null) GM.Changed -= Render;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && view != "home")
                Open("home");
        }

        void Open(string target)
        {
            view = target;
            index = 0;
            pendingSellId = null;
            Render();
        }

        void Render()
        {
            if (GM?.State == null)
            {
                statusText.text = "LOADING";
                titleText.text = "";
                bodyText.text = "";
                return;
            }

            ResetActions();
            SetPaging(false);
            if (metricRow != null)
                metricRow.SetActive(false);

            if (customPanel != null)
                customPanel.SetActive(view == "custom");
            if (morePanel != null)
                morePanel.SetActive(view == "more");
            if (homePanel != null)
                homePanel.SetActive(view == "home");
            if (contentPanel != null)
                contentPanel.gameObject.SetActive(view != "more" && view != "home");

            var s = GM.State;
            string stage = CampaignManager.Instance?.CurrentStage() ?? "";

            if (statusText != null)
                statusText.text = "SBW2";
            if (dayChipText != null)
                dayChipText.text = $"DAY  {s.day}/{s.maxDay}";
            if (goldChipText != null)
                goldChipText.text = $"GOLD  {s.gold}";
            if (difficultyChipText != null)
                difficultyChipText.text = (s.manager ?? "").ToUpper();
            if (stageChipText != null)
                stageChipText.text = string.IsNullOrWhiteSpace(stage)
                    ? "OPENING"
                    : stage.ToUpper();

            RefreshNavStyle();

            switch (view)
            {
                case "market": RenderMarket(); break;
                case "staff": RenderStaff(); break;
                case "properties": RenderProperties(); break;
                case "propertyUpgrade": RenderPropertyUpgrade(); break;
                case "progression": RenderProgression(); break;
                case "equipment": RenderEquipment(); break;
                case "gallery": RenderGallery(); break;
                case "more": RenderMore(); break;
                case "management": RenderManagement(); break;
                case "custom": RenderCustom(); break;
                case "diagnostics": RenderDiagnostics(); break;
                default: RenderHome(); break;
            }
        }

        void RenderHome()
        {
            titleText.text = "OVERVIEW";

            var state = GM.State;
            int staffCount = state.staff.Count(x => x.owned);
            int propertyCount = state.properties.Count(x => x.owned);
            int openMissions = state.missions.Count(x => !x.completed);

            SetMetrics(
                "STAFF", staffCount.ToString(),
                "PROPERTIES", propertyCount.ToString(),
                "OPEN MISSIONS", openMissions.ToString(),
                "LAST PROFIT", state.lastDay.profit.ToString()
            );

            if (homeLastDayText != null)
            {
                string mode = state.campaignEnded && !state.endless
                    ? "\n<color=#F2C66D>CAMPAIGN COMPLETE</color>"
                    : state.endless
                        ? "\n<color=#F2C66D>ENDLESS MODE</color>"
                        : "";

                homeLastDayText.text =
                    "<color=#B995AD>LAST DAY</color>\n" +
                    $"<size=42><color=#F2C66D>{state.lastDay.profit}</color></size>  PROFIT" +
                    mode +
                    "\n\n" +
                    $"Revenue  <b>{state.lastDay.revenue}</b>     " +
                    $"Costs  <b>{state.lastDay.costs}</b>\n" +
                    $"Customers  <b>{state.lastDay.customers}</b>" +
                    (string.IsNullOrEmpty(state.lastDay.incident)
                        ? ""
                        : $"\n<color=#C7A5B8>{state.lastDay.incident}</color>");
            }

            if (homeMissionText != null)
            {
                var missionLines = new StringBuilder();
                missionLines.AppendLine("<color=#B995AD>MISSIONS</color>");

                if (state.missions == null || state.missions.Length == 0)
                {
                    missionLines.AppendLine("<color=#817684>No active missions.</color>");
                }
                else
                {
                    foreach (var mission in state.missions.Take(3))
                    {
                        missionLines.AppendLine(
                            $"{(mission.completed ? "✓" : "•")}  " +
                            $"<b>{mission.title}</b>   {mission.progress}/{mission.target}"
                        );
                    }
                }

                homeMissionText.text = missionLines.ToString();
            }

            if (homeRewardText != null)
            {
                var rewards = new StringBuilder();
                rewards.AppendLine("<color=#B995AD>READY</color>");

                if (state.buyerOffer != null)
                {
                    var offered = GM.Character(state.buyerOffer.id);
                    rewards.AppendLine(
                        $"<color=#F2C66D>Client offer</color>\n" +
                        $"{offered?.name ?? state.buyerOffer.id} · " +
                        $"{state.buyerOffer.offer} Gold"
                    );
                }

                if (state.weeklyToken > 0)
                    rewards.AppendLine("<color=#F2C66D>Weekly card</color>");
                if (state.specialTokens > 0)
                    rewards.AppendLine($"Premium cards  {state.specialTokens}");

                if (state.buyerOffer == null &&
                    state.weeklyToken <= 0 &&
                    state.specialTokens <= 0)
                {
                    rewards.AppendLine("<color=#817684>Nothing pending.</color>");
                }

                homeRewardText.text = rewards.ToString();
            }

            if (bodyText != null)
                bodyText.text = "";

            HideHero();

            Bind(secondaryButton, "SYSTEM", () => Open("diagnostics"));
            Bind(
                tertiaryButton,
                state.buyerOffer != null ? "CLIENT OFFER" : "MANAGEMENT",
                () => Open("management")
            );

            Bind(quaternaryButton, $"CUSTOM  {state.customCount}/5", () =>
            {
                customMessage = "";
                Open("custom");
            });

            if (state.campaignEnded && !state.endless)
            {
                Bind(primaryButton, "CONTINUE ENDLESS", () =>
                {
                    GM.EnableEndless();
                    Render();
                });
            }
        }

        void RenderCustom()
        {
            titleText.text = "CUSTOM CHARACTER";
            HideHero();

            var s = GM.State;
            bool maxed = s.customCount >= 5;

            if (customNameInput != null)
            {
                customNameInput.interactable = !maxed;
                customNameInput.characterLimit = 24;
            }

            if (customAgeInput != null)
            {
                customAgeInput.interactable = !maxed;
                customAgeInput.characterLimit = 2;

                if (string.IsNullOrWhiteSpace(customAgeInput.text))
                    customAgeInput.text = "25";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Created           {s.customCount}/5");
            sb.AppendLine($"Profile           {CustomKindLabel(customKind)}");
            sb.AppendLine("Age range         21–80");
            sb.AppendLine("Recruit cost      1000");
            sb.AppendLine("Rank / salary     1 / 12");
            sb.AppendLine("Base fee          24");
            sb.AppendLine();
            sb.AppendLine(
                "Enter a name and age above. " +
                "The candidate is added to the market."
            );

            if (!string.IsNullOrWhiteSpace(customMessage))
            {
                sb.AppendLine();
                sb.AppendLine(customMessage);
            }

            bodyText.text = sb.ToString();

            Bind(
                primaryButton,
                maxed ? "LIMIT REACHED" : "CREATE",
                () =>
                {
                    int age;
                    if (!int.TryParse(customAgeInput?.text, out age))
                        age = 0;

                    if (GM.CreateCustomCharacter(
                            customNameInput?.text,
                            age,
                            customKind,
                            out var createdId,
                            out var error))
                    {
                        customMessage =
                            $"Created {GM.Character(createdId)?.name ?? createdId}.";
                        if (customNameInput != null)
                            customNameInput.text = "";
                        if (customAgeInput != null)
                            customAgeInput.text = "25";
                    }
                    else
                    {
                        customMessage = error ?? "Could not create character.";
                    }

                    Render();
                },
                !maxed
            );

            Bind(secondaryButton, $"KIND {CustomKindLabel(customKind).ToUpper()}", () =>
            {
                CycleCustomKind();
                Render();
            }, !maxed);

            Bind(tertiaryButton, "MARKET", () => Open("market"));
            Bind(quaternaryButton, "BACK", () => Open("home"));
        }

        string CustomKindLabel(string kind)
        {
            if (kind == "charisma") return "Charismatic";
            if (kind == "refinement") return "Refined";
            if (kind == "stamina") return "Stamina";
            return "Balanced";
        }

        void CycleCustomKind()
        {
            string[] kinds = { "balanced", "charisma", "refinement", "stamina" };
            int current = Array.IndexOf(kinds, customKind);
            customKind = kinds[(current + 1 + kinds.Length) % kinds.Length];
        }

        void RenderManagement()
        {
            titleText.text = "MANAGEMENT";
            HideHero();

            var s = GM.State;
            int weekDay = ((s.day - 1) % 7) + 1;
            var mission = s.missions != null && s.missions.Length > 0
                ? s.missions[0]
                : null;

            var owned = GM.Owned().ToArray();
            GameplayCharacter selected = null;

            if (owned.Length > 0)
            {
                index = Wrap(index, owned.Length);
                selected = owned[index];
                selectedCharacterId = selected.id;
            }

            string joker = WeeklyJokerLabel(s.weeklyJoker);
            int cash = GM.WeeklyJokerCashValue(s.weeklyJoker);
            int premiumCandidates = GM.PremiumCandidateCount();

            var sb = new StringBuilder();
            sb.AppendLine($"Week day          {weekDay}/7");
            sb.AppendLine($"Weekly card       {(s.weeklyToken > 0 ? joker : "USED")}");
            sb.AppendLine($"Premium cards     {s.specialTokens}");
            sb.AppendLine($"Premium available {premiumCandidates}");

            if (mission != null)
            {
                sb.AppendLine();
                sb.AppendLine("MONTHLY MISSION");
                sb.AppendLine(mission.label ?? mission.title);
                sb.AppendLine($"{mission.progress}/{mission.target}");
            }

            sb.AppendLine();
            sb.AppendLine("MANAGEMENT STATS");
            sb.AppendLine($"Revenue           {s.totalRevenue}");
            sb.AppendLine($"Costs             {s.totalCosts}");
            sb.AppendLine($"Customers         {s.totalCustomers}");
            sb.AppendLine($"Sick episodes     {s.sickEpisodes}");
            sb.AppendLine($"Days worked       {s.daysWorked}");
            sb.AppendLine($"Missions won      {s.missionsCompleted}");
            sb.AppendLine($"Campaign score    {GM.CampaignScore()}");

            if (s.buyerOffer != null)
            {
                var offered = GM.Character(s.buyerOffer.id);
                sb.AppendLine();
                sb.AppendLine("CLIENT OFFER");
                sb.AppendLine(
                    $"{offered?.name ?? s.buyerOffer.id}: " +
                    $"{s.buyerOffer.offer} Gold"
                );
                sb.AppendLine($"Current value     {s.buyerOffer.value}");
            }
            else if (selected != null)
            {
                sb.AppendLine();
                sb.AppendLine("SELECTED STAFF");
                sb.AppendLine(selected.name);
                sb.AppendLine($"Sale value        {GM.GirlValue(selected.id)}");
            }

            bodyText.text = sb.ToString();

            if (s.buyerOffer != null)
            {
                Bind(
                    primaryButton,
                    $"ACCEPT {s.buyerOffer.offer}",
                    () =>
                    {
                        GM.AcceptBuyerOffer();
                        Render();
                    }
                );

                Bind(
                    secondaryButton,
                    "DECLINE OFFER",
                    () =>
                    {
                        GM.DeclineBuyerOffer();
                        Render();
                    }
                );

                Bind(
                    tertiaryButton,
                    $"PREMIUM {s.specialTokens}",
                    () =>
                    {
                        GM.UsePremiumCard();
                        Render();
                    },
                    s.specialTokens > 0 && premiumCandidates > 0
                );

                return;
            }

            Bind(
                primaryButton,
                $"USE {joker.ToUpper()}",
                () =>
                {
                    GM.UseWeeklyJoker(false);
                    Render();
                },
                s.weeklyToken > 0
            );

            Bind(
                secondaryButton,
                $"CASH {cash}",
                () =>
                {
                    GM.UseWeeklyJoker(true);
                    Render();
                },
                s.weeklyToken > 0
            );

            Bind(
                tertiaryButton,
                $"PREMIUM {s.specialTokens}",
                () =>
                {
                    GM.UsePremiumCard();
                    Render();
                },
                s.specialTokens > 0 && premiumCandidates > 0
            );

            if (selected != null)
            {
                int value = GM.GirlValue(selected.id);
                bool confirming = pendingSellId == selected.id;

                Bind(
                    quaternaryButton,
                    confirming
                        ? $"CONFIRM SELL {value}"
                        : $"SELL {value}",
                    () =>
                    {
                        if (pendingSellId == selected.id)
                        {
                            pendingSellId = null;
                            GM.SellStaff(selected.id);
                        }
                        else
                        {
                            pendingSellId = selected.id;
                        }

                        Render();
                    }
                );
            }
        }

        string WeeklyJokerLabel(string kind)
        {
            if (kind == "health") return "Rest";
            if (kind == "xp") return "Tutoring";
            if (kind == "demand") return "Busy Night";
            if (kind == "reputation") return "Good Rumors";
            return "Sponsorship";
        }

        void RenderDiagnostics()
        {
            titleText.text = "SYSTEM";

            var roster = GM.Roster?.characters ?? Array.Empty<GameplayCharacter>();
            int activePacks = roster.Count(x => x != null && x.photo_pack_active);
            int photoRefs = roster
                .Where(x => x != null && x.photo_pack_active)
                .Sum(x => Mathf.Max(0, x.photo_count));

            string revision = string.IsNullOrWhiteSpace(GM.BuildRevision)
                ? "unknown"
                : GM.BuildRevision.Substring(0, Mathf.Min(12, GM.BuildRevision.Length));

            bodyText.text =
                $"App version       {Application.version}\n" +
                $"Source build      v208_ELSA_21\n" +
                $"Revision          {revision}\n" +
                $"Channel           {GM.BuildChannel}\n" +
                $"Runtime data      {(GM.DataReady ? "READY" : "NOT READY")}\n" +
                $"Roster            {roster.Length}/126\n" +
                $"Photo packs       {activePacks}/32\n" +
                $"Photo references  {photoRefs}/650\n" +
                $"Save loaded       {(GM.State != null ? "YES" : "NO")}\n" +
                $"Media sample      {mediaDiagnosticResult}\n\n" +
                MobilePerformanceManager.DeviceSummary();

            HideHero();
            Bind(primaryButton, "BACK", () => Open("home"));
            Bind(secondaryButton, "TITLE", () => startMenu?.ShowTitle());
            Bind(
                tertiaryButton,
                mediaDiagnosticRunning ? "TESTING..." : "MEDIA TEST",
                () =>
                {
                    if (!mediaDiagnosticRunning)
                        StartCoroutine(RunMediaDiagnostic());
                },
                !mediaDiagnosticRunning
            );
        }

        IEnumerator RunMediaDiagnostic()
        {
            if (mediaDiagnosticRunning)
                yield break;

            if (photoLoader == null)
                photoLoader = FindAnyObjectByType<PhotoPackLoader>();

            if (photoLoader == null || GM?.Roster?.characters == null)
            {
                mediaDiagnosticResult = "LOADER ERROR";
                Render();
                yield break;
            }

            mediaDiagnosticRunning = true;
            mediaDiagnosticResult = "RUNNING";
            Render();

            int checkedPacks = 0;
            int failures = 0;

            foreach (var character in GM.Roster.characters.Where(
                         x => x != null && x.photo_pack_active))
            {
                CharacterPackManifest pack = null;
                string error = null;

                yield return photoLoader.LoadPackManifest(
                    character.id,
                    x => pack = x,
                    e => error = e
                );

                if (!string.IsNullOrEmpty(error) ||
                    pack?.photos == null ||
                    pack.photos.Length == 0)
                {
                    failures++;
                    continue;
                }

                Texture2D texture = null;
                error = null;

                yield return photoLoader.LoadTexture(
                    character.id,
                    pack.photos[0],
                    x => texture = x,
                    e => error = e
                );

                if (!string.IsNullOrEmpty(error) || texture == null)
                {
                    failures++;
                }
                else
                {
                    Destroy(texture);
                    checkedPacks++;
                }
            }

            mediaDiagnosticRunning = false;
            mediaDiagnosticResult =
                failures == 0 && checkedPacks == 32
                    ? "PASS 32/32"
                    : $"FAIL {failures} · PASS {checkedPacks}/32";

            Render();
        }

        void RenderMarket()
        {
            var arr = GM.Market().ToArray();
            titleText.text = "MARKET";

            if (arr.Length == 0)
            {
                SetMetrics("TODAY", "0", "GOLD", GM.State.gold.ToString(), "STATUS", "EMPTY", "PHOTOS", "—");
                bodyText.text =
                    "<color=#C79AB8>NO CANDIDATES TODAY</color>\n\n" +
                    "Finish the day or return later for a new market rotation.";
                HideHero();
                return;
            }

            index = Wrap(index, arr.Length);
            var candidate = arr[index];
            selectedCharacterId = candidate.id;
            bool affordable = GM.State.gold >= candidate.cost;

            SetMetrics(
                "RANK", candidate.rank.ToString(),
                "COST", candidate.cost.ToString(),
                "SALARY", candidate.salary.ToString(),
                "PHOTOS", candidate.photo_count.ToString()
            );

            bodyText.text =
                "<color=#C79AB8>CANDIDATE PROFILE</color>\n" +
                $"Source   {candidate.source}\n" +
                $"Base fee   {candidate.price}\n\n" +
                (affordable
                    ? "<color=#9FD8AF>Ready to recruit.</color>"
                    : $"<color=#E0A7A7>Need {candidate.cost - GM.State.gold} more Gold.</color>");

            ShowHero(candidate);
            SetHeroInfo(
                candidate.name,
                candidate.source,
                affordable ? "AVAILABLE" : "LOCKED"
            );
            SetPaging(arr.Length > 1);

            Bind(primaryButton, $"HIRE  {candidate.cost}", () =>
            {
                GM.Hire(candidate.id);
                Render();
            }, affordable);

            Bind(secondaryButton, "OPEN GALLERY", () =>
            {
                selectedCharacterId = candidate.id;
                Open("gallery");
            });
        }

        void RenderStaff()
        {
            var arr = GM.Owned().ToArray();
            titleText.text = "STAFF";

            if (arr.Length == 0)
            {
                SetMetrics("STAFF", "0", "GOLD", GM.State.gold.ToString(), "ROOMS", "—", "STATUS", "EMPTY");
                bodyText.text =
                    "<color=#C79AB8>NO STAFF YET</color>\n\n" +
                    "Visit the market to recruit your first character.";
                HideHero();
                return;
            }

            index = Wrap(index, arr.Length);
            var character = arr[index];
            var state = GM.Staff(character.id);
            selectedCharacterId = character.id;

            int recommended = V208Economy.RecommendedPrice(
                state.rank,
                PM.EffectiveCharisma(state),
                PM.EffectiveRefinement(state),
                PM.EffectiveReputation(state),
                state.skills.service
            );

            SetMetrics(
                "RANK", state.rank.ToString(),
                "XP", state.xp.ToString(),
                "AP", state.ap.ToString(),
                "FEE", state.price.ToString()
            );

            bodyText.text =
                "<color=#C79AB8>STATUS</color>\n" +
                $"Action   {ActionLabel(state.action)}\n" +
                $"Workplace   {state.workplace}\n" +
                $"Health   {state.health}     Joy   {state.joy}\n" +
                $"Recommended fee   {recommended}\n\n" +
                "<color=#C79AB8>ATTRIBUTES</color>\n" +
                $"Charisma   {PM.EffectiveCharisma(state)}     Refinement   {PM.EffectiveRefinement(state)}\n" +
                $"Constitution   {PM.EffectiveConstitution(state)}     Reputation   {PM.EffectiveReputation(state)}";

            ShowHero(character);
            SetHeroInfo(
                character.name,
                $"Fee {state.price} · Recommended {recommended}",
                ActionLabel(state.action).ToUpper()
            );
            SetPaging(arr.Length > 1);

            Bind(primaryButton, "NEXT ACTION", () => CycleStaffAction(character.id));
            Bind(secondaryButton, "WORKPLACE", () => CycleWorkplace(character.id));
            Bind(tertiaryButton, "FEE -5", () => GM.SetPrice(character.id, state.price - 5));
            Bind(quaternaryButton, "FEE +5", () => GM.SetPrice(character.id, state.price + 5));
        }

        void RenderProperties()
        {
            titleText.text = "PROPERTIES";

            var owned = GM.State.properties;
            if (owned == null || owned.Length == 0)
            {
                SetMetrics("OWNED", "0", "ROOMS", "0", "APPEAL", "—", "UPKEEP", "—");
                bodyText.text = "<color=#C79AB8>NO PROPERTIES CONFIGURED</color>";
                HideHero();
                return;
            }

            index = Wrap(index, owned.Length);
            var property = owned[index];
            var next = GM.NextPropertyForPurchase();

            SetMetrics(
                "ROOMS", GM.RoomCapacity(property).ToString(),
                "APPEAL", Mathf.RoundToInt(property.appeal * 100).ToString(),
                "UPKEEP", property.upkeep.ToString(),
                "NEXT", next == null ? "DONE" : next.purchasePrice.ToString()
            );

            bodyText.text =
                $"<size=34>{property.name}</size>\n" +
                $"<color=#AFA4B5>{property.district}</color>\n\n" +
                "<color=#C79AB8>UPGRADES</color>\n" +
                $"Advertising   L{property.advertising}      Security   L{property.security}\n" +
                $"Comfort   L{property.comfort}      Clinic   L{property.clinic}\n" +
                $"Room Quality   L{property.roomQuality}      Expansion   L{property.roomExpansion}\n\n" +
                (next == null
                    ? "<color=#9FD8AF>All properties owned.</color>"
                    : "<color=#D8A75A>NEXT PROPERTY</color>\n" +
                      $"{next.name} · {next.district}\nCost   {next.purchasePrice} Gold");

            HideHero();
            SetPaging(owned.Length > 1);

            Bind(primaryButton, "UPGRADES", () =>
            {
                propertyUpgradeIndex = 0;
                view = "propertyUpgrade";
                Render();
            });

            if (next != null)
            {
                Bind(secondaryButton, $"BUY  {next.purchasePrice}", () =>
                {
                    GM.BuyNextProperty();
                    Render();
                }, GM.State.gold >= next.purchasePrice);
            }
        }

        void RenderPropertyUpgrade()
        {
            titleText.text = "UPGRADES";

            var owned = GM.State.properties;
            if (owned == null || owned.Length == 0)
            {
                SetMetrics("PROPERTY", "—", "UPGRADE", "—", "LEVEL", "—", "COST", "—");
                bodyText.text = "No property available.";
                HideHero();
                return;
            }

            index = Wrap(index, owned.Length);
            var property = owned[index];

            string[] keys =
            {
                "advertising", "security", "comfort",
                "clinic", "roomQuality", "roomExpansion"
            };

            string[] labels =
            {
                "Advertising", "Security", "Comfort",
                "Clinic", "Room Quality", "Room Expansion"
            };

            propertyUpgradeIndex = Wrap(propertyUpgradeIndex, keys.Length);
            string key = keys[propertyUpgradeIndex];
            string label = labels[propertyUpgradeIndex];

            int level =
                key == "advertising" ? property.advertising :
                key == "security" ? property.security :
                key == "comfort" ? property.comfort :
                key == "clinic" ? property.clinic :
                key == "roomQuality" ? property.roomQuality :
                property.roomExpansion;

            int cost = level >= 5 ? 0 : V208Economy.UpgradeCost(key, level);

            SetMetrics(
                "PROPERTY", property.name,
                "UPGRADE", label,
                "LEVEL", $"{level}/5",
                "COST", level >= 5 ? "MAX" : cost.ToString()
            );

            bodyText.text =
                $"<size=34>{label}</size>\n" +
                $"<color=#AFA4B5>{property.name}</color>\n\n" +
                (level >= 5
                    ? "<color=#9FD8AF>Maximum level reached.</color>"
                    : $"Next level costs <color=#F3D28E>{cost} Gold</color>.");

            HideHero();

            Bind(primaryButton, level >= 5 ? "MAX LEVEL" : $"UPGRADE  {cost}", () =>
            {
                if (level < 5)
                    GM.UpgradeProperty(property.id, key);
                Render();
            }, level < 5 && GM.State.gold >= cost);

            Bind(secondaryButton, "PREVIOUS", () =>
            {
                propertyUpgradeIndex--;
                Render();
            });

            Bind(tertiaryButton, "NEXT", () =>
            {
                propertyUpgradeIndex++;
                Render();
            });

            Bind(quaternaryButton, "BACK", () => Open("properties"));
        }

        void RenderProgression()
        {
            titleText.text = "PROGRESSION";
            var arr = GM.Owned().ToArray();

            if (arr.Length == 0)
            {
                SetMetrics("RANK", "—", "XP", "—", "AP", "—", "COST", "—");
                bodyText.text = "No staff available.";
                HideHero();
                return;
            }

            var character = arr.FirstOrDefault(x => x.id == selectedCharacterId) ?? arr[0];
            selectedCharacterId = character.id;
            var state = GM.Staff(character.id);

            bool maxRank = state.rank >= 5;
            int xpNeed = maxRank ? 0 : PM.XpRequiredForNextRank(state);
            int goldNeed = maxRank ? 0 : PM.RankUpGoldCost(state);

            SetMetrics(
                "RANK", $"{state.rank}/5",
                "XP", maxRank ? "MAX" : $"{state.xp}/{xpNeed}",
                "AP", state.ap.ToString(),
                "RANK COST", maxRank ? "MAX" : goldNeed.ToString()
            );

            bodyText.text =
                "<color=#C79AB8>SKILLS</color>\n" +
                $"Charm      L{state.skills.charm}     {SkillCostLabel(state.skills.charm)}\n" +
                $"Service    L{state.skills.service}     {SkillCostLabel(state.skills.service)}\n" +
                $"Stamina    L{state.skills.stamina}     {SkillCostLabel(state.skills.stamina)}";

            ShowHero(character);
            SetHeroInfo(
                character.name,
                maxRank ? "Maximum rank" : $"XP {state.xp}/{xpNeed}",
                maxRank ? "MAX RANK" : $"RANK {state.rank}"
            );

            Bind(primaryButton, maxRank ? "RANK MAX" : $"RANK UP  {goldNeed}",
                () => { if (!maxRank) PM.RankUp(character.id); Render(); },
                !maxRank && state.xp >= xpNeed && GM.State.gold >= goldNeed);

            int charmCost = state.skills.charm >= 5 ? 0 : V208Economy.SkillApCost(state.skills.charm);
            int serviceCost = state.skills.service >= 5 ? 0 : V208Economy.SkillApCost(state.skills.service);
            int staminaCost = state.skills.stamina >= 5 ? 0 : V208Economy.SkillApCost(state.skills.stamina);

            Bind(secondaryButton,
                state.skills.charm >= 5 ? "CHARM MAX" : $"CHARM  {charmCost} AP",
                () => { PM.SpendAP(character.id, "charm"); Render(); },
                state.skills.charm < 5 && state.ap >= charmCost);

            Bind(tertiaryButton,
                state.skills.service >= 5 ? "SERVICE MAX" : $"SERVICE  {serviceCost} AP",
                () => { PM.SpendAP(character.id, "service"); Render(); },
                state.skills.service < 5 && state.ap >= serviceCost);

            Bind(quaternaryButton,
                state.skills.stamina >= 5 ? "STAMINA MAX" : $"STAMINA  {staminaCost} AP",
                () => { PM.SpendAP(character.id, "stamina"); Render(); },
                state.skills.stamina < 5 && state.ap >= staminaCost);
        }

        void RenderEquipment()
        {
            titleText.text = "EQUIPMENT";

            var owned = GM.Owned().ToArray();
            if (owned.Length == 0)
            {
                SetMetrics("ITEM", "—", "COST", "—", "OWNED", "—", "GOLD", GM.State.gold.ToString());
                bodyText.text = "No staff available.";
                HideHero();
                return;
            }

            var character = owned.FirstOrDefault(x => x.id == selectedCharacterId) ?? owned[0];
            selectedCharacterId = character.id;
            var state = GM.Staff(character.id);

            var items = PM.Catalog.ToArray();
            if (items.Length == 0)
            {
                SetMetrics("ITEM", "0", "COST", "—", "OWNED", "0", "GOLD", GM.State.gold.ToString());
                bodyText.text = "No equipment configured.";
                HideHero();
                return;
            }

            index = Wrap(index, items.Length);
            var item = items[index];
            int inventory = PM.InventoryCount(item.id);

            string slot1 = PM.EquippedItemId(state, 0);
            string slot2 = PM.EquippedItemId(state, 1);
            string slot3 = PM.EquippedItemId(state, 2);

            SetMetrics(
                "COST", item.cost.ToString(),
                "INVENTORY", inventory.ToString(),
                "GOLD", GM.State.gold.ToString(),
                "ITEM", $"{index + 1}/{items.Length}"
            );

            bodyText.text =
                $"<size=34>{item.name}</size>\n" +
                "<color=#C79AB8>BONUSES</color>\n" +
                $"CHA +{item.charisma}     REF +{item.refinement}     CON +{item.constitution}\n" +
                $"REP +{item.reputation}     JOY +{item.joy}\n\n" +
                "<color=#C79AB8>EQUIPPED</color>\n" +
                $"Slot 1   {PM.ItemName(slot1)}\n" +
                $"Slot 2   {PM.ItemName(slot2)}\n" +
                $"Slot 3   {PM.ItemName(slot3)}";

            ShowHero(character);
            SetHeroInfo(character.name, item.name, "EQUIPMENT");
            SetPaging(items.Length > 1);

            Bind(primaryButton, $"BUY  {item.cost}", () =>
            {
                PM.BuyItem(item.id);
                Render();
            }, GM.State.gold >= item.cost);

            Bind(
                secondaryButton,
                slot1 == item.id ? "UNEQUIP 1" : "EQUIP 1",
                () => { PM.EquipItem(character.id, 0, item.id); Render(); },
                slot1 == item.id || inventory > 0
            );

            Bind(
                tertiaryButton,
                slot2 == item.id ? "UNEQUIP 2" : "EQUIP 2",
                () => { PM.EquipItem(character.id, 1, item.id); Render(); },
                slot2 == item.id || inventory > 0
            );

            Bind(
                quaternaryButton,
                slot3 == item.id ? "UNEQUIP 3" : "EQUIP 3",
                () => { PM.EquipItem(character.id, 2, item.id); Render(); },
                slot3 == item.id || inventory > 0
            );
        }

        void RenderGallery()
        {
            titleText.text = "GALLERY";
            var character = GM.Character(selectedCharacterId);

            if (character == null)
            {
                SetMetrics("UNLOCKED", "—", "TOTAL", "—", "NEXT", "—", "STATUS", "—");
                bodyText.text = "Character unavailable.";
                HideHero();
                return;
            }

            int unlocked = GM.PhotoUnlockedCount(character.id);
            int progress = GM.PhotoWorkProgress(character.id);

            SetMetrics(
                "UNLOCKED", unlocked.ToString(),
                "TOTAL", character.photo_count.ToString(),
                "NEXT PHOTO", unlocked >= character.photo_count ? "DONE" : $"{progress}/3",
                "STATUS", unlocked >= character.photo_count ? "COMPLETE" : "ACTIVE"
            );

            bodyText.text =
                $"<size=34>{character.name}</size>\n" +
                $"Photos unlocked   {unlocked}/{character.photo_count}\n" +
                (unlocked >= character.photo_count
                    ? "<color=#9FD8AF>Photo pack complete.</color>"
                    : $"Next photo   {progress}/3 worked days");

            ShowHero(character);
            SetHeroInfo(
                character.name,
                $"{unlocked}/{character.photo_count} unlocked",
                "GALLERY"
            );

            Bind(primaryButton, "PREVIOUS PHOTO", () => gallery?.Previous());
            Bind(secondaryButton, "NEXT PHOTO", () => gallery?.Next());
        }

        void RenderMore()
        {
            titleText.text = "MORE";

            var state = GM.State;
            SetMetrics(
                "GOLD", state.gold.ToString(),
                "STAFF", state.staff.Count(x => x.owned).ToString(),
                "PROPERTIES", state.properties.Count(x => x.owned).ToString(),
                "DAY", $"{state.day}/{state.maxDay}"
            );

            if (bodyText != null)
                bodyText.text = "";

            HideHero();
        }

        void ShowHero(CharacterData character)
        {
            SetMainLayout(true);

            if (heroImage != null)
                heroImage.gameObject.SetActive(true);
            if (heroCaption != null)
                heroCaption.gameObject.SetActive(true);

            if (gallery != null)
                gallery.Open(character);
        }

        void HideHero()
        {
            SetMainLayout(false);

            if (heroImage != null)
                heroImage.gameObject.SetActive(false);
            if (heroCaption != null)
                heroCaption.gameObject.SetActive(false);

            SetHeroInfo("", "", "");
        }

        void SetHeroInfo(string title, string meta, string badge)
        {
            if (heroTitleText != null)
                heroTitleText.text = title ?? "";
            if (heroMetaText != null)
                heroMetaText.text = meta ?? "";
            if (heroBadgeText != null)
                heroBadgeText.text = badge ?? "";
        }

        void SetMainLayout(bool hasHero)
        {
            if (heroPanel != null)
                heroPanel.SetActive(hasHero);

            if (contentPanel == null)
                return;

            var min = contentPanel.anchorMin;
            var max = contentPanel.anchorMax;
            min.y = 0.205f;
            max.y = hasHero || view == "custom" ? 0.405f : 0.675f;
            contentPanel.anchorMin = min;
            contentPanel.anchorMax = max;
            contentPanel.offsetMin = Vector2.zero;
            contentPanel.offsetMax = Vector2.zero;
        }

        void SetMetrics(
            string l1, string v1,
            string l2, string v2,
            string l3, string v3,
            string l4, string v4)
        {
            if (metricRow != null)
                metricRow.SetActive(true);

            if (metric1Label != null) metric1Label.text = l1;
            if (metric1Value != null) metric1Value.text = v1;
            if (metric2Label != null) metric2Label.text = l2;
            if (metric2Value != null) metric2Value.text = v2;
            if (metric3Label != null) metric3Label.text = l3;
            if (metric3Value != null) metric3Value.text = v3;
            if (metric4Label != null) metric4Label.text = l4;
            if (metric4Value != null) metric4Value.text = v4;
        }

        void SetPaging(bool visible)
        {
            if (previousButton != null)
                previousButton.gameObject.SetActive(visible);
            if (nextButton != null)
                nextButton.gameObject.SetActive(visible);
        }

        void RefreshNavStyle()
        {
            StyleNav(
                homeButton,
                view == "home" ||
                view == "management" ||
                view == "custom" ||
                view == "diagnostics"
            );
            StyleNav(marketButton, view == "market");
            StyleNav(staffButton, view == "staff");
            StyleNav(
                moreButton,
                view == "more" ||
                view == "properties" ||
                view == "propertyUpgrade" ||
                view == "progression" ||
                view == "equipment" ||
                view == "gallery"
            );
        }

        void StyleNav(Button button, bool active)
        {
            if (button == null) return;

            var image = button.GetComponent<Image>();
            if (image != null)
                image.color = active ? MobileTheme.AccentSoft : MobileTheme.Button;
        }

        void CycleStaffAction(string characterId)
        {
            var s = GM.Staff(characterId);
            if (s == null) return;

            string[] actions =
            {
                "work", "rest", "trainCharm", "trainRef", "trainCon"
            };

            int current = Array.IndexOf(actions, s.action);
            int next = (current + 1 + actions.Length) % actions.Length;
            GM.SetAction(characterId, actions[next]);
        }

        string ActionLabel(string action)
        {
            if (action == "work") return "Work";
            if (action == "rest") return "Rest";
            if (action == "trainCharm") return "Train Charisma";
            if (action == "trainRef") return "Train Refinement";
            if (action == "trainCon") return "Train Constitution";
            return action ?? "—";
        }

        string SkillCostLabel(int level)
        {
            return level >= 5 ? "MAX" : V208Economy.SkillApCost(level) + " AP";
        }

        void CycleWorkplace(string characterId)
        {
            var owned = GM.State.properties.Where(x => x.owned).ToArray();
            if (owned.Length == 0) return;

            var s = GM.Staff(characterId);
            int current = Array.FindIndex(owned, x => x.id == s.workplace);
            int next = (current + 1 + owned.Length) % owned.Length;
            GM.SetWorkplace(characterId, owned[next].id);
        }

        void Previous()
        {
            pendingSellId = null;
            index--;
            Render();
        }

        void Next()
        {
            pendingSellId = null;
            index++;
            Render();
        }

        int Wrap(int value, int count)
        {
            if (count <= 0) return 0;
            return ((value % count) + count) % count;
        }

        void ResetActions()
        {
            Clear(primaryButton);
            Clear(secondaryButton);
            Clear(tertiaryButton);
            Clear(quaternaryButton);
        }

        void Clear(Button button)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.interactable = true;
            button.gameObject.SetActive(false);
        }

        void Bind(
            Button button,
            string label,
            UnityEngine.Events.UnityAction action,
            bool interactable = true)
        {
            if (button == null) return;

            button.gameObject.SetActive(true);
            button.interactable = interactable;
            button.onClick.RemoveAllListeners();

            if (interactable && action != null)
                button.onClick.AddListener(action);

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                if (!interactable)
                    image.color = MobileTheme.Disabled;
                else if (button == primaryButton)
                    image.color = MobileTheme.AccentDeep;
                else
                    image.color = MobileTheme.Button;
            }

            var t = button.GetComponentInChildren<Text>();
            if (t != null) t.text = label;
        }
    }
}
