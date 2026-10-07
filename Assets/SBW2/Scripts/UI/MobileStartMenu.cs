using UnityEngine;
using UnityEngine.UI;
using SBW2.Core;

namespace SBW2.UI
{
    public class MobileStartMenu : MonoBehaviour
    {
        [SerializeField] GameObject menuRoot;
        [SerializeField] GameObject gameRoot;
        [SerializeField] Text infoText;
        [SerializeField] Button continueButton;
        [SerializeField] Button easyButton;
        [SerializeField] Button normalButton;
        [SerializeField] Button hardButton;

        GameplayManager GM => GameplayManager.Instance;
        string pendingNewDifficulty;

        void Start()
        {
            continueButton.onClick.AddListener(ContinueGame);
            easyButton.onClick.AddListener(() => NewGame("easy"));
            normalButton.onClick.AddListener(() => NewGame("normal"));
            hardButton.onClick.AddListener(() => NewGame("hard"));

            gameRoot.SetActive(false);
            menuRoot.SetActive(true);

            if (GM != null)
            {
                GM.DataLoaded += Refresh;
                GM.Changed += Refresh;
            }

            Refresh();
        }

        void OnDestroy()
        {
            if (GM != null)
            {
                GM.DataLoaded -= Refresh;
                GM.Changed -= Refresh;
            }
        }

        void SetDifficultyButtons(bool enabled)
        {
            easyButton.interactable = enabled;
            normalButton.interactable = enabled;
            hardButton.interactable = enabled;
        }

        void Refresh()
        {
            if (GM == null)
            {
                infoText.text = "INITIALIZING…";
                continueButton.interactable = false;
                SetDifficultyButtons(false);
                return;
            }

            if (!GM.DataReady)
            {
                continueButton.interactable = false;
                SetDifficultyButtons(false);

                infoText.text = string.IsNullOrEmpty(GM.DataError)
                    ? "Loading v208 data…"
                    : "DATA ERROR\n" + GM.DataError;
                return;
            }

            SetDifficultyButtons(true);

            bool canContinue = GM.State != null;
            continueButton.interactable = canContinue;

            infoText.text = canContinue
                ? $"SAVE FOUND\nDay {GM.State.day}/{GM.State.maxDay} · Gold {GM.State.gold} · {GM.State.manager.ToUpper()}"
                : $"V208 READY · {GM.Roster.characters.Length} CHARACTERS\nChoose a difficulty to start.";
        }

        public void ShowTitle()
        {
            pendingNewDifficulty = null;
            gameRoot.SetActive(false);
            menuRoot.SetActive(true);
            Refresh();
        }

        void ContinueGame()
        {
            if (GM?.State == null) return;
            pendingNewDifficulty = null;
            menuRoot.SetActive(false);
            gameRoot.SetActive(true);
        }

        void NewGame(string difficulty)
        {
            if (GM == null || !GM.DataReady) return;

            if (GM.State != null && pendingNewDifficulty != difficulty)
            {
                pendingNewDifficulty = difficulty;
                infoText.text =
                    "SAVE EXISTS\n" +
                    "Tap NEW · " + difficulty.ToUpper() +
                    " again to replace the current save.";
                return;
            }

            pendingNewDifficulty = null;
            GM.NewGame(difficulty);

            if (GM.State == null)
            {
                infoText.text = "Could not create a new game.";
                return;
            }

            menuRoot.SetActive(false);
            gameRoot.SetActive(true);
        }
    }
}
