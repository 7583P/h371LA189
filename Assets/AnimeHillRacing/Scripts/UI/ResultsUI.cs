using AnimeHillRacing.Core;
using AnimeHillRacing.TimeTrial;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    public sealed class ResultsUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleText;
        [SerializeField] private Text resultsText;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private TimeTrialManager timeTrialManager;

        private const float ResultsBackgroundAlpha = .5f;

        public Text TitleText => titleText;
        public Text StatsText => resultsText;

        public void Show(GameManager manager)
        {
            gameManager = manager;
            ApplyBackgroundTransparency();
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            string reason = manager.EndReason == RunEndReason.DriverDown ? "DRIVER DOWN" :
                manager.EndReason == RunEndReason.Crashed ? "CRASHED" : "OUT OF FUEL";
            if (titleText != null) titleText.text = reason;
            resultsText.text = $"DISTANCE\n<size=34>{manager.Distance:N0} m</size>\n\n" +
                               $"BEST DISTANCE   {manager.BestDistance:N0} m\n" +
                               $"COINS EARNED   +{manager.RunCoins:N0}\n" +
                               $"TOTAL COINS   {manager.SavedTotalCoins:N0}\n\n" +
                               $"FLIPS   {manager.RunFlips}\n" +
                               $"BACKFLIPS   {manager.RunBackflips}\n" +
                               $"STUNT BONUS   {manager.StuntBonus}\n\n" +
                               $"MAX SPEED   {manager.MaxSpeedKmh:0} km/h\n" +
                               $"{GameModeNames.DisplayName(manager.Mode)}   -   {manager.MapId.ToUpperInvariant()}";
        }

        public void ShowTimeTrial(TimeTrialManager manager)
        {
            timeTrialManager = manager;
            ApplyBackgroundTransparency();
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            if (titleText != null) titleText.text = $"FINISHED - {manager.CircuitName}";
            resultsText.text = $"TOTAL TIME\n<size=34>{TimeTrialManager.FormatTime(manager.ElapsedTime)}</size>\n\n" +
                               $"BEST LAP   {TimeTrialManager.FormatTime(manager.BestLap)}\n" +
                               $"MAX SPEED   {manager.MaxSpeedKmh:0} km/h";
        }

        public void HideImmediate()
        {
            if (root != null) root.SetActive(false);
        }

        public void Retry()
        {
            if (timeTrialManager != null) timeTrialManager.Retry(); else gameManager?.Retry();
        }

        public void Menu()
        {
            if (timeTrialManager != null) timeTrialManager.MainMenu(); else gameManager?.MainMenu();
        }

        public void Assign(GameObject targetRoot, Text targetTitle, Text targetText, GameManager manager)
        {
            root = targetRoot;
            titleText = targetTitle;
            resultsText = targetText;
            gameManager = manager;
            ApplyBackgroundTransparency();
        }

        private void ApplyBackgroundTransparency()
        {
            Image background = root != null ? root.GetComponent<Image>() : GetComponent<Image>();
            if (background == null) return;
            Color color = background.color;
            color.a = ResultsBackgroundAlpha;
            background.color = color;
        }

        public void AssignTimeTrial(TimeTrialManager manager) => timeTrialManager = manager;
    }
}
