using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using SBW2.Core;
using SBW2.Data;

namespace SBW2.UI
{
    public class DaySummaryPanel : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Text summaryText;
        [SerializeField] Button closeButton;

        void Start()
        {
            closeButton.onClick.AddListener(Hide);
            panel.SetActive(false);
        }

        public void ShowLastDay()
        {
            var gm = GameplayManager.Instance;
            if (gm?.State == null) return;

            var d = gm.State.lastDay ?? new LastDayState();
            var sb = new StringBuilder();

            sb.AppendLine($"DAY {gm.State.day - 1} COMPLETE");
            sb.AppendLine();
            sb.AppendLine($"Revenue      {d.revenue}");
            sb.AppendLine($"Costs        {d.costs}");
            sb.AppendLine($"Profit       {d.profit:+#;-#;0}");
            sb.AppendLine($"Customers    {d.customers}");

            if (!string.IsNullOrWhiteSpace(d.incident))
            {
                sb.AppendLine();
                sb.AppendLine(d.incident);
            }

            var results = d.staffResults ?? Array.Empty<DailyStaffResult>();
            if (results.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine("STAFF");

                int limit = Mathf.Min(6, results.Length);
                for (int i = 0; i < limit; i++)
                {
                    var r = results[i];
                    sb.Append("• ");
                    sb.Append(r.name);
                    sb.Append(" · ");
                    sb.Append(OutcomeLabel(r.outcome));

                    if (r.customers > 0 || r.revenue > 0)
                        sb.Append($" · {r.customers} clients · +{r.revenue} Gold");

                    if (r.photoUnlocked)
                        sb.Append($" · PHOTO {r.photoCount}/{r.photoTotal}");
                    else if (r.photoTotal > 0 && r.outcome == "work")
                        sb.Append($" · photo {r.photoProgress}/3");

                    sb.AppendLine();
                }

                if (results.Length > limit)
                    sb.AppendLine($"+ {results.Length - limit} more staff results");
            }

            summaryText.text = sb.ToString();
            panel.SetActive(true);
        }

        string OutcomeLabel(string outcome)
        {
            if (outcome == "work") return "Worked";
            if (outcome == "rest") return "Rested";
            if (outcome == "trainCharm") return "Trained charisma";
            if (outcome == "trainRef") return "Trained refinement";
            if (outcome == "trainCon") return "Trained constitution";
            if (outcome == "sick") return "Sick / recovery";
            if (outcome == "noRoom") return "No room";
            if (outcome == "lowHealth") return "Low health";
            if (outcome == "absent") return "Absent";
            return outcome ?? "No result";
        }

        public void Hide()
        {
            panel.SetActive(false);
        }
    }
}
