#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SBW2.Core;
using SBW2.Content;
using SBW2.UI;

namespace SBW2.EditorTools
{
    public static class SBW2MobileRebuildBootstrap
    {
        [MenuItem("SBW2/Bootstrap Mobile Rebuild v0.19.0")]
        public static void Bootstrap()
        {
            const string sceneDir = "Assets/SBW2/Scenes";
            if (!Directory.Exists(sceneDir)) Directory.CreateDirectory(sceneDir);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var systems = new GameObject("Systems");
            systems.AddComponent<GameplayManager>();
            systems.AddComponent<ProgressionManager>();
            systems.AddComponent<CampaignManager>();
            systems.AddComponent<PhotoPackLoader>();
            systems.AddComponent<MobilePerformanceManager>();

            var canvasGo = new GameObject(
                "Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            var bg = Panel(canvasGo.transform, "Background", 0, 0, 1, 1, MobileTheme.Background);
            var topGlow = Panel(bg.transform, "TopRoseGlow", 0, .70f, 1, 1, MobileTheme.GlowRose);
            var bottomGlow = Panel(bg.transform, "BottomGoldGlow", 0, 0, 1, .22f, MobileTheme.GlowGold);

            var safe = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaFitter));
            safe.transform.SetParent(bg.transform, false);
            Stretch(safe.GetComponent<RectTransform>(), 0, 0, 1, 1);

            // -----------------------------------------------------------------
            // GAME ROOT
            // -----------------------------------------------------------------
            var gameRoot = new GameObject("GameRoot", typeof(RectTransform));
            gameRoot.transform.SetParent(safe.transform, false);
            Stretch(gameRoot.GetComponent<RectTransform>(), 0, 0, 1, 1);

            // Compact app bar. The old build spent almost 20% of the screen on
            // four large status blocks; V4 keeps all information without wasting space.
            var headerBand = Panel(
                gameRoot.transform,
                "HeaderBand",
                .025f, .932f, .975f, .985f,
                MobileTheme.TopBar
            );
            AccentBar(headerBand.transform, "HeaderAccent", 0, .94f, 1, 1, MobileTheme.Accent);

            var status = Text(
                headerBand.transform,
                "Brand",
                29,
                TextAnchor.MiddleLeft,
                .035f, .06f, .255f, .92f
            );
            status.color = MobileTheme.Accent;

            Text dayChip;
            Text goldChip;
            Text difficultyChip;
            Text stageChip;

            Chip(headerBand.transform, "DayChip", .285f, .10f, .47f, .90f, out dayChip);
            Chip(headerBand.transform, "GoldChip", .485f, .10f, .67f, .90f, out goldChip);

            difficultyChip = Text(
                headerBand.transform,
                "DifficultyChip",
                17,
                TextAnchor.MiddleCenter,
                .69f, .12f, .82f, .88f
            );
            difficultyChip.color = MobileTheme.Muted;

            stageChip = Text(
                headerBand.transform,
                "StageChip",
                17,
                TextAnchor.MiddleCenter,
                .83f, .12f, .975f, .88f
            );
            stageChip.color = MobileTheme.Muted;

            goldChip.color = MobileTheme.Accent;

            var title = Text(
                gameRoot.transform,
                "Title",
                48,
                TextAnchor.MiddleLeft,
                .04f, .875f, .96f, .932f
            );

            // Four stats in a 2x2 grid. On a phone this reads much better than four
            // narrow columns while keeping the same information density.
            var metricRow = new GameObject("MetricRow", typeof(RectTransform));
            metricRow.transform.SetParent(gameRoot.transform, false);
            Stretch(metricRow.GetComponent<RectTransform>(), .03f, .755f, .97f, .862f);

            Text m1l, m1v, m2l, m2v, m3l, m3v, m4l, m4v;
            MetricCard(metricRow.transform, "Metric1", .00f, .52f, .492f, 1, out m1l, out m1v);
            MetricCard(metricRow.transform, "Metric2", .508f, .52f, 1.00f, 1, out m2l, out m2v);
            MetricCard(metricRow.transform, "Metric3", .00f, 0, .492f, .48f, out m3l, out m3v);
            MetricCard(metricRow.transform, "Metric4", .508f, 0, 1.00f, .48f, out m4l, out m4v);

            // Hero card. Image geometry is close to 9:16, so photos no longer float
            // inside a large empty container.
            var heroPanel = Panel(
                gameRoot.transform,
                "HeroPanel",
                 .03f, .405f, .97f, .742f,
                MobileTheme.PanelAlt
            );
            AccentBar(heroPanel.transform, "HeroAccent", 0, 0, .012f, 1, MobileTheme.Accent);
            AccentBar(heroPanel.transform, "HeroGoldTop", 0, .992f, 1, 1, MobileTheme.Accent);

            var imageCard = Panel(
                heroPanel.transform,
                "PhotoCard",
                 .020f, .025f, .485f, .975f,
                MobileTheme.Background
            );

            var heroImageGo = new GameObject(
                "HeroImage",
                typeof(RectTransform),
                typeof(RawImage),
                typeof(AspectRatioFitter)
            );
            heroImageGo.transform.SetParent(imageCard.transform, false);
            Stretch(heroImageGo.GetComponent<RectTransform>(), .02f, .02f, .98f, .98f);

            var heroImage = heroImageGo.GetComponent<RawImage>();
            var heroFit = heroImageGo.GetComponent<AspectRatioFitter>();
            heroFit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            heroFit.aspectRatio = 9f / 16f;

            var captionBg = Panel(
                imageCard.transform,
                "CaptionBg",
                .02f, .02f, .98f, .145f,
                MobileTheme.Overlay
            );
            var heroCaption = Text(
                captionBg.transform,
                "HeroCaption",
                15,
                TextAnchor.MiddleCenter,
                .04f, .08f, .96f, .92f
            );
            heroCaption.color = MobileTheme.Text;

            var heroBadgePanel = Panel(
                heroPanel.transform,
                "HeroBadge",
                .515f, .785f, .965f, .930f,
                MobileTheme.AccentDeep
            );
            var heroBadge = Text(
                heroBadgePanel.transform,
                "HeroBadgeText",
                20,
                TextAnchor.MiddleCenter,
                .03f, .05f, .97f, .95f
            );
            heroBadge.color = MobileTheme.Accent;

            var heroTitle = Text(
                heroPanel.transform,
                "HeroTitle",
                41,
                TextAnchor.MiddleLeft,
                .515f, .555f, .965f, .780f
            );

            var heroMeta = Text(
                heroPanel.transform,
                "HeroMeta",
                23,
                TextAnchor.UpperLeft,
                .515f, .125f, .965f, .545f
            );
            heroMeta.color = MobileTheme.Muted;

            // Detail card: compact on character screens, tall on non-character screens.
            var contentPanel = Panel(
                gameRoot.transform,
                "ContentPanel",
                .03f, .205f, .97f, .392f,
                MobileTheme.Panel
            );
            AccentBar(contentPanel.transform, "ContentAccent", 0, .985f, 1, 1, MobileTheme.AccentSoft);

            var body = Text(
                contentPanel.transform,
                "Body",
                MobileTheme.BodyFont,
                TextAnchor.UpperLeft,
                .035f, .045f, .965f, .95f
            );

            // HOME gets purpose-built cards instead of one huge empty text panel.
            var homePanel = new GameObject("HomeDashboard", typeof(RectTransform));
            homePanel.transform.SetParent(gameRoot.transform, false);
            Stretch(homePanel.GetComponent<RectTransform>(), .03f, .205f, .97f, .705f);

            var lastDayCard = FramedCard(
                homePanel.transform,
                "LastDayCard",
                0, .50f, 1, 1,
                MobileTheme.AccentDeep,
                MobileTheme.Card
            );
            var homeLastDay = Text(
                lastDayCard.transform,
                "LastDayText",
                28,
                TextAnchor.UpperLeft,
                .045f, .08f, .955f, .92f
            );

            var missionCard = FramedCard(
                homePanel.transform,
                "MissionCard",
                0, 0, .49f, .46f,
                MobileTheme.AccentSoft,
                MobileTheme.Card
            );
            var homeMission = Text(
                missionCard.transform,
                "MissionText",
                23,
                TextAnchor.UpperLeft,
                .07f, .10f, .93f, .90f
            );

            var rewardCard = FramedCard(
                homePanel.transform,
                "RewardCard",
                .51f, 0, 1, .46f,
                MobileTheme.Accent,
                MobileTheme.Card
            );
            var homeReward = Text(
                rewardCard.transform,
                "RewardText",
                23,
                TextAnchor.UpperLeft,
                .07f, .10f, .93f, .90f
            );

            homePanel.SetActive(false);

            // Custom character editor uses its own card.
            var customPanel = Panel(
                gameRoot.transform,
                "CustomCharacterPanel",
                .03f, .414f, .97f, .690f,
                MobileTheme.PanelAlt
            );
            AccentBar(customPanel.transform, "CustomAccent", 0, 0, .010f, 1, MobileTheme.Accent);

            var customHeader = Text(
                customPanel.transform,
                "Header",
                36,
                TextAnchor.MiddleLeft,
                .05f, .74f, .95f, .94f
            );
            customHeader.text = "CREATE CHARACTER";

            var customName = Input(
                customPanel.transform,
                "CustomName",
                "Name",
                .06f, .43f, .94f, .66f
            );
            customName.characterLimit = 24;

            var customAge = Input(
                customPanel.transform,
                "CustomAge",
                "Age 21–80",
                .06f, .14f, .46f, .35f
            );
            customAge.contentType = InputField.ContentType.IntegerNumber;
            customAge.characterLimit = 2;
            customAge.text = "25";
            customPanel.SetActive(false);

            // Context action zone.
            var prev = Button(
                gameRoot.transform,
                "Previous",
                "‹",
                .03f, .112f, .085f, .192f,
                MobileTheme.CardAlt,
                34
            );
            var next = Button(
                gameRoot.transform,
                "Next",
                "›",
                .915f, .112f, .97f, .192f,
                MobileTheme.CardAlt,
                34
            );

            var a1 = Button(
                gameRoot.transform,
                "Action1",
                "ACTION",
                .105f, .153f, .49f, .192f,
                MobileTheme.AccentDeep,
                21
            );
            var a2 = Button(
                gameRoot.transform,
                "Action2",
                "ACTION",
                .51f, .153f, .895f, .192f,
                MobileTheme.Button,
                21
            );
            var a3 = Button(
                gameRoot.transform,
                "Action3",
                "ACTION",
                .105f, .112f, .49f, .148f,
                MobileTheme.Button,
                20
            );
            var a4 = Button(
                gameRoot.transform,
                "Action4",
                "ACTION",
                .51f, .112f, .895f, .148f,
                MobileTheme.Button,
                20
            );

            // Bottom navigation: only the four sections needed constantly.
            var bottomBar = Panel(
                gameRoot.transform,
                "BottomBar",
                .025f, .020f, .975f, .100f,
                MobileTheme.NavBar
            );

            var home = Button(
                bottomBar.transform,
                "Home",
                "HOME",
                .01f, .08f, .245f, .92f,
                MobileTheme.AccentDeep,
                19
            );
            var market = Button(
                bottomBar.transform,
                "Market",
                "MARKET",
                .255f, .08f, .49f, .92f,
                MobileTheme.Button,
                19
            );
            var staff = Button(
                bottomBar.transform,
                "Staff",
                "STAFF",
                .51f, .08f, .745f, .92f,
                MobileTheme.Button,
                19
            );
            var more = Button(
                bottomBar.transform,
                "More",
                "MORE",
                .755f, .08f, .99f, .92f,
                MobileTheme.Button,
                19
            );

            // "More" hub replaces the heavy 8-button permanent grid.
            var morePanel = Panel(
                gameRoot.transform,
                "MorePanel",
                .03f, .205f, .97f, .690f,
                MobileTheme.Panel
            );
            AccentBar(morePanel.transform, "MoreAccent", 0, .985f, 1, 1, MobileTheme.AccentSoft);

            var moreTitle = Text(
                morePanel.transform,
                "MoreTitle",
                24,
                TextAnchor.MiddleLeft,
                .055f, .865f, .945f, .96f
            );
            moreTitle.text = "MANAGEMENT  ·  COLLECTION";
            moreTitle.color = MobileTheme.Muted;

            var properties = Button(
                morePanel.transform,
                "Properties",
                "PROPERTIES",
                .055f, .61f, .475f, .84f,
                MobileTheme.CardAlt,
                22
            );
            var progression = Button(
                morePanel.transform,
                "Progression",
                "PROGRESSION",
                .525f, .61f, .945f, .84f,
                MobileTheme.CardAlt,
                22
            );
            var equipment = Button(
                morePanel.transform,
                "Equipment",
                "EQUIPMENT",
                .055f, .35f, .475f, .58f,
                MobileTheme.CardAlt,
                22
            );
            var galleryButton = Button(
                morePanel.transform,
                "Gallery",
                "GALLERY",
                .525f, .35f, .945f, .58f,
                MobileTheme.CardAlt,
                22
            );
            var endDay = Button(
                morePanel.transform,
                "EndDay",
                "END DAY",
                .055f, .09f, .945f, .29f,
                MobileTheme.AccentDeep,
                24
            );
            morePanel.SetActive(false);

            var galleryGo = new GameObject("CharacterGallery", typeof(CharacterGallery));
            galleryGo.transform.SetParent(gameRoot.transform, false);
            var gallery = galleryGo.GetComponent<CharacterGallery>();
            gallery.Configure(heroImage, heroCaption);

            // Day summary overlay.
            var dayPanel = Panel(
                safe.transform,
                "DaySummary",
                .075f, .255f, .925f, .745f,
                MobileTheme.PanelStrong
            );
            AccentBar(dayPanel.transform, "DayAccent", 0, .985f, 1, 1, MobileTheme.Accent);
            var dayText = Text(
                dayPanel.transform,
                "Summary",
                29,
                TextAnchor.UpperLeft,
                .08f, .20f, .92f, .92f
            );
            var dayClose = Button(
                dayPanel.transform,
                "Close",
                "CONTINUE",
                .20f, .055f, .80f, .16f,
                MobileTheme.AccentDeep,
                23
            );

            var daySummary = dayPanel.AddComponent<DaySummaryPanel>();
            var daySO = new SerializedObject(daySummary);
            daySO.FindProperty("panel").objectReferenceValue = dayPanel;
            daySO.FindProperty("summaryText").objectReferenceValue = dayText;
            daySO.FindProperty("closeButton").objectReferenceValue = dayClose;
            daySO.ApplyModifiedPropertiesWithoutUndo();

            // Controller wiring.
            var controllerGo = new GameObject("MobileGameController", typeof(MobileGameController));
            controllerGo.transform.SetParent(gameRoot.transform, false);
            var controller = controllerGo.GetComponent<MobileGameController>();
            var so = new SerializedObject(controller);

            so.FindProperty("statusText").objectReferenceValue = status;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("bodyText").objectReferenceValue = body;
            so.FindProperty("heroPanel").objectReferenceValue = heroPanel;
            so.FindProperty("contentPanel").objectReferenceValue = contentPanel.GetComponent<RectTransform>();

            so.FindProperty("heroImage").objectReferenceValue = heroImage;
            so.FindProperty("heroCaption").objectReferenceValue = heroCaption;
            so.FindProperty("heroTitleText").objectReferenceValue = heroTitle;
            so.FindProperty("heroMetaText").objectReferenceValue = heroMeta;
            so.FindProperty("heroBadgeText").objectReferenceValue = heroBadge;

            so.FindProperty("metricRow").objectReferenceValue = metricRow;
            so.FindProperty("metric1Label").objectReferenceValue = m1l;
            so.FindProperty("metric1Value").objectReferenceValue = m1v;
            so.FindProperty("metric2Label").objectReferenceValue = m2l;
            so.FindProperty("metric2Value").objectReferenceValue = m2v;
            so.FindProperty("metric3Label").objectReferenceValue = m3l;
            so.FindProperty("metric3Value").objectReferenceValue = m3v;
            so.FindProperty("metric4Label").objectReferenceValue = m4l;
            so.FindProperty("metric4Value").objectReferenceValue = m4v;

            so.FindProperty("dayChipText").objectReferenceValue = dayChip;
            so.FindProperty("goldChipText").objectReferenceValue = goldChip;
            so.FindProperty("difficultyChipText").objectReferenceValue = difficultyChip;
            so.FindProperty("stageChipText").objectReferenceValue = stageChip;

            so.FindProperty("homeButton").objectReferenceValue = home;
            so.FindProperty("marketButton").objectReferenceValue = market;
            so.FindProperty("staffButton").objectReferenceValue = staff;
            so.FindProperty("propertiesButton").objectReferenceValue = properties;
            so.FindProperty("progressionButton").objectReferenceValue = progression;
            so.FindProperty("equipmentButton").objectReferenceValue = equipment;
            so.FindProperty("galleryButton").objectReferenceValue = galleryButton;
            so.FindProperty("endDayButton").objectReferenceValue = endDay;
            so.FindProperty("moreButton").objectReferenceValue = more;
            so.FindProperty("morePanel").objectReferenceValue = morePanel;

            so.FindProperty("homePanel").objectReferenceValue = homePanel;
            so.FindProperty("homeLastDayText").objectReferenceValue = homeLastDay;
            so.FindProperty("homeMissionText").objectReferenceValue = homeMission;
            so.FindProperty("homeRewardText").objectReferenceValue = homeReward;

            so.FindProperty("previousButton").objectReferenceValue = prev;
            so.FindProperty("nextButton").objectReferenceValue = next;
            so.FindProperty("primaryButton").objectReferenceValue = a1;
            so.FindProperty("secondaryButton").objectReferenceValue = a2;
            so.FindProperty("tertiaryButton").objectReferenceValue = a3;
            so.FindProperty("quaternaryButton").objectReferenceValue = a4;
            so.FindProperty("daySummary").objectReferenceValue = daySummary;

            so.FindProperty("customPanel").objectReferenceValue = customPanel;
            so.FindProperty("customNameInput").objectReferenceValue = customName;
            so.FindProperty("customAgeInput").objectReferenceValue = customAge;
            so.ApplyModifiedPropertiesWithoutUndo();

            // -----------------------------------------------------------------
            // START MENU
            // -----------------------------------------------------------------
            var menuRoot = Panel(
                safe.transform,
                "StartMenu",
                .025f, .025f, .975f, .975f,
                MobileTheme.Background
            );

            var menuCard = Panel(
                menuRoot.transform,
                "MenuCard",
                .055f, .075f, .945f, .925f,
                MobileTheme.Panel
            );
            AccentBar(menuCard.transform, "MenuAccent", 0, .988f, 1, 1, MobileTheme.Accent);

            var eyebrow = Text(
                menuCard.transform,
                "Eyebrow",
                20,
                TextAnchor.MiddleCenter,
                .08f, .875f, .92f, .925f
            );
            eyebrow.text = "PRIVATE MANAGEMENT SIM";
            eyebrow.color = MobileTheme.Accent;

            var logo = Text(
                menuCard.transform,
                "Logo",
                62,
                TextAnchor.MiddleCenter,
                .06f, .655f, .94f, .86f
            );
            logo.text = "SIM BROTHEL\nWORLD 2";

            var subtitle = Text(
                menuCard.transform,
                "Subtitle",
                22,
                TextAnchor.MiddleCenter,
                .08f, .59f, .92f, .65f
            );
            subtitle.text = "UNITY MOBILE · v208";
            subtitle.color = MobileTheme.Muted;

            var infoPanel = Panel(
                menuCard.transform,
                "InfoPanel",
                .08f, .405f, .92f, .565f,
                MobileTheme.Card
            );
            var info = Text(
                infoPanel.transform,
                "Info",
                26,
                TextAnchor.MiddleCenter,
                .06f, .08f, .94f, .92f
            );

            var continueBtn = Button(
                menuCard.transform,
                "Continue",
                "CONTINUE",
                .12f, .305f, .88f, .385f,
                MobileTheme.AccentDeep,
                25
            );

            var newGame = Text(
                menuCard.transform,
                "NewGameLabel",
                19,
                TextAnchor.MiddleCenter,
                .10f, .255f, .90f, .295f
            );
            newGame.text = "START A NEW GAME";
            newGame.color = MobileTheme.Muted;

            var easyBtn = Button(
                menuCard.transform,
                "Easy",
                "EASY",
                .12f, .175f, .88f, .245f,
                MobileTheme.Button,
                22
            );
            var normalBtn = Button(
                menuCard.transform,
                "Normal",
                "NORMAL",
                .12f, .095f, .88f, .165f,
                MobileTheme.Button,
                22
            );
            var hardBtn = Button(
                menuCard.transform,
                "Hard",
                "HARD",
                .12f, .015f, .88f, .085f,
                MobileTheme.Button,
                22
            );

            var start = menuRoot.AddComponent<MobileStartMenu>();
            var startSO = new SerializedObject(start);
            startSO.FindProperty("menuRoot").objectReferenceValue = menuRoot;
            startSO.FindProperty("gameRoot").objectReferenceValue = gameRoot;
            startSO.FindProperty("infoText").objectReferenceValue = info;
            startSO.FindProperty("continueButton").objectReferenceValue = continueBtn;
            startSO.FindProperty("easyButton").objectReferenceValue = easyBtn;
            startSO.FindProperty("normalButton").objectReferenceValue = normalBtn;
            startSO.FindProperty("hardButton").objectReferenceValue = hardBtn;
            startSO.ApplyModifiedPropertiesWithoutUndo();

            // Required for Android touch/click interaction.
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                new GameObject(
                    "EventSystem",
                    typeof(EventSystem),
                    typeof(StandaloneInputModule)
                );
            }

            var cam = new GameObject("Main Camera", typeof(Camera));
            cam.tag = "MainCamera";
            cam.GetComponent<Camera>().backgroundColor = MobileTheme.Background;

            string scenePath = sceneDir + "/MobileRebuild.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(scenePath, true)
            };

            PlayerSettings.bundleVersion = "0.19.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "SBW2",
                    "Mobile Rebuild v0.19.0 V3 created. No APK built.",
                    "OK"
                );
            }
            else
            {
                Debug.Log("SBW2 MobileRebuild V3 scene generated for batch/cloud build.");
            }
        }

        static GameObject Panel(
            Transform parent,
            string name,
            float x1,
            float y1,
            float x2,
            float y2,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), x1, y1, x2, y2);
            var image = go.GetComponent<Image>();
            image.color = color;

            return go;
        }

        static GameObject AccentBar(
            Transform parent,
            string name,
            float x1,
            float y1,
            float x2,
            float y2,
            Color color)
        {
            return Panel(parent, name, x1, y1, x2, y2, color);
        }

        static void Chip(
            Transform parent,
            string name,
            float x1,
            float y1,
            float x2,
            float y2,
            out Text text)
        {
            var chip = Panel(parent, name, x1, y1, x2, y2, MobileTheme.Card);
            AccentBar(chip.transform, "GoldEdge", .03f, .94f, .97f, 1, MobileTheme.Accent);
            text = Text(
                chip.transform,
                "Text",
                20,
                TextAnchor.MiddleCenter,
                .03f, .04f, .97f, .96f
            );
            text.color = MobileTheme.Muted;
        }

        static void MetricCard(
            Transform parent,
            string name,
            float x1,
            float y1,
            float x2,
            float y2,
            out Text label,
            out Text value)
        {
            var card = Panel(parent, name, x1, y1, x2, y2, MobileTheme.Card);
            AccentBar(card.transform, "Accent", .04f, .955f, .96f, 1, MobileTheme.AccentSoft);

            label = Text(
                card.transform,
                "Label",
                13,
                TextAnchor.MiddleLeft,
                .08f, .58f, .94f, .90f
            );
            label.color = MobileTheme.Muted;

            value = Text(
                card.transform,
                "Value",
                32,
                TextAnchor.MiddleLeft,
                .08f, .08f, .94f, .58f
            );
            value.color = MobileTheme.Text;
        }

        static GameObject FramedCard(
            Transform parent,
            string name,
            float x1,
            float y1,
            float x2,
            float y2,
            Color border,
            Color fill)
        {
            var outer = Panel(parent, name, x1, y1, x2, y2, border);
            var inner = Panel(outer.transform, "Fill", .014f, .014f, .986f, .986f, fill);
            return inner;
        }

        static Text Text(
            Transform parent,
            string name,
            int size,
            TextAnchor anchor,
            float x1,
            float y1,
            float x2,
            float y2)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), x1, y1, x2, y2);

            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = anchor;
            text.color = MobileTheme.Text;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        static InputField Input(
            Transform parent,
            string name,
            string placeholder,
            float x1,
            float y1,
            float x2,
            float y2)
        {
            var go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(InputField)
            );
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), x1, y1, x2, y2);
            go.GetComponent<Image>().color = MobileTheme.Card;

            var input = go.GetComponent<InputField>();
            var text = Text(
                go.transform,
                "Text",
                25,
                TextAnchor.MiddleLeft,
                .04f, .08f, .96f, .92f
            );
            var hint = Text(
                go.transform,
                "Placeholder",
                23,
                TextAnchor.MiddleLeft,
                .04f, .08f, .96f, .92f
            );

            hint.text = placeholder;
            hint.color = MobileTheme.Faint;

            input.textComponent = text;
            input.placeholder = hint;
            input.targetGraphic = go.GetComponent<Image>();
            input.lineType = InputField.LineType.SingleLine;
            return input;
        }

        static Button Button(
            Transform parent,
            string name,
            string label,
            float x1,
            float y1,
            float x2,
            float y2,
            Color color,
            int fontSize)
        {
            var go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)
            );
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), x1, y1, x2, y2);
            var buttonImage = go.GetComponent<Image>();
            buttonImage.color = color;

            var button = go.GetComponent<Button>();

            var labelText = Text(
                go.transform,
                "Text",
                fontSize,
                TextAnchor.MiddleCenter,
                .02f, .02f, .98f, .98f
            );
            labelText.text = label;

            return button;
        }

        static void Stretch(
            RectTransform rt,
            float x1,
            float y1,
            float x2,
            float y2)
        {
            rt.anchorMin = new Vector2(x1, y1);
            rt.anchorMax = new Vector2(x2, y2);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
#endif
