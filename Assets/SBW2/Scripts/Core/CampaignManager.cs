using System.Linq;
using UnityEngine;

namespace SBW2.Core
{
    public class CampaignManager : MonoBehaviour
    {
        public static CampaignManager Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public string CurrentStage()
        {
            var gm = GameplayManager.Instance;
            if (gm?.State == null) return "Loading";

            int day = gm.State.day;
            int owned = gm.State.staff.Count(x => x.owned);
            int properties = gm.State.properties.Count(x => x.owned);

            if (day <= 15) return "Opening";
            if (owned < 3) return "Recruitment";
            if (properties < 2) return "Expansion";
            if (day < 120) return "Growth";
            if (day < 220) return "Established";
            return "Endgame";
        }

        public float Progress01()
        {
            var gm = GameplayManager.Instance;
            if (gm?.State == null) return 0f;
            return Mathf.Clamp01((gm.State.day - 1f) / Mathf.Max(1f, gm.State.maxDay - 1f));
        }
    }
}
