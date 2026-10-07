using UnityEngine;

namespace SBW2.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        RectTransform panel;
        Rect lastSafeArea;
        Vector2Int lastScreen;

        void Awake()
        {
            panel = GetComponent<RectTransform>();
            Apply();
        }

        void Update()
        {
            if (lastSafeArea != Screen.safeArea ||
                lastScreen.x != Screen.width ||
                lastScreen.y != Screen.height)
                Apply();
        }

        void Apply()
        {
            if (panel == null || Screen.width <= 0 || Screen.height <= 0) return;

            Rect safe = Screen.safeArea;
            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;

            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            panel.anchorMin = min;
            panel.anchorMax = max;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;

            lastSafeArea = safe;
            lastScreen = new Vector2Int(Screen.width, Screen.height);
        }
    }
}
