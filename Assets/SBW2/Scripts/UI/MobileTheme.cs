using UnityEngine;

namespace SBW2.UI
{
    public static class MobileTheme
    {
        // V5 "Velvet Gold" — warm, glamorous and readable.
        // The UI stays dark enough for photography, but replaces the cold navy
        // dashboard look with plum glass, champagne gold and vivid rose.
        public static readonly Color Background = new Color(0.055f, 0.025f, 0.075f);
        public static readonly Color Panel = new Color(0.105f, 0.055f, 0.135f, 0.94f);
        public static readonly Color PanelAlt = new Color(0.145f, 0.070f, 0.165f, 0.95f);
        public static readonly Color PanelStrong = new Color(0.085f, 0.040f, 0.110f, 0.985f);
        public static readonly Color Card = new Color(0.155f, 0.080f, 0.185f, 0.94f);
        public static readonly Color CardAlt = new Color(0.205f, 0.105f, 0.230f, 0.96f);

        public static readonly Color Text = new Color(1.000f, 0.970f, 0.925f);
        public static readonly Color Muted = new Color(0.885f, 0.785f, 0.850f);
        public static readonly Color Faint = new Color(0.670f, 0.535f, 0.650f);

        public static readonly Color Accent = new Color(1.000f, 0.725f, 0.270f);
        public static readonly Color AccentSoft = new Color(1.000f, 0.245f, 0.610f);
        public static readonly Color AccentDeep = new Color(0.720f, 0.185f, 0.590f);
        public static readonly Color Overlay = new Color(0.050f, 0.020f, 0.070f, 0.72f);

        public static readonly Color Button = new Color(0.270f, 0.125f, 0.320f, 0.98f);
        public static readonly Color ButtonHover = new Color(0.930f, 0.245f, 0.590f);
        public static readonly Color ButtonPressed = new Color(0.600f, 0.120f, 0.470f);
        public static readonly Color Disabled = new Color(0.145f, 0.095f, 0.155f, 0.72f);

        public static readonly Color TopBar = new Color(0.095f, 0.045f, 0.115f, 0.97f);
        public static readonly Color NavBar = new Color(0.080f, 0.035f, 0.105f, 0.985f);
        public static readonly Color Success = new Color(0.420f, 0.900f, 0.650f);

        public static readonly Color GlowRose = new Color(1.000f, 0.180f, 0.580f, 0.30f);
        public static readonly Color GlowGold = new Color(1.000f, 0.690f, 0.230f, 0.24f);
        public static readonly Color GlassEdge = new Color(1.000f, 0.835f, 0.700f, 0.34f);

        public const int HeaderFont = 42;
        public const int BodyFont = 27;
        public const int ButtonFont = 23;
    }
}
