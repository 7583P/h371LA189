using AnimeHillRacing.Core;
using UnityEngine;
using UnityEngine.UI;
using AnimeHillRacing.TimeTrial;

namespace AnimeHillRacing.UI
{
    public sealed class ResultsUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text resultsText;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private TimeTrialManager timeTrialManager;

        public void Show(GameManager manager)
        {
            gameManager = manager;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            string transmission = manager.Vehicle.Transmission.Mode.ToString().ToUpperInvariant();
            string reason = manager.EndReason == RunEndReason.DriverDown ? "DRIVER DOWN" :
                manager.EndReason == RunEndReason.Crashed ? "CRASHED" : "OUT OF FUEL";
            resultsText.text = $"<size=34>RUN OVER — {reason}</size>\n\n" +
                               $"DISTANCE   {manager.Distance:N0} m        BEST   {manager.BestDistance:N0} m\n" +
                               $"COINS   +{manager.RunCoins:N0}        TOTAL   {manager.SavedTotalCoins:N0}\n" +
                               $"MAX SPEED   {manager.MaxSpeedKmh:0} km/h        TRANSMISSION   {transmission}\n" +
                               $"{GameModeNames.DisplayName(manager.Mode)}   —   {manager.MapId.ToUpperInvariant()}";
        }

        public void ShowTimeTrial(TimeTrialManager manager)
        {
            timeTrialManager = manager;
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            string transmission = manager.Vehicle.Transmission.Mode.ToString().ToUpperInvariant();
            resultsText.text = $"<size=34>FINISHED — {manager.CircuitName}</size>\n\n" +
                               $"TOTAL TIME   {TimeTrialManager.FormatTime(manager.ElapsedTime)}\n" +
                               $"BEST LAP   {TimeTrialManager.FormatTime(manager.BestLap)}\n" +
                               $"MAX SPEED   {manager.MaxSpeedKmh:0} km/h        TRANSMISSION   {transmission}";
        }

        public void HideImmediate()
        {
            if (root != null)
                root.SetActive(false);
        }

        public void Retry()
        {
            if (timeTrialManager != null) timeTrialManager.Retry(); else gameManager?.Retry();
        }
        public void Menu()
        {
            if (timeTrialManager != null) timeTrialManager.MainMenu(); else gameManager?.MainMenu();
        }

        public void Assign(GameObject targetRoot, Text targetText, GameManager manager)
        {
            root = targetRoot;
            resultsText = targetText;
            gameManager = manager;
        }

        public void AssignTimeTrial(GameObject targetRoot, Text targetText, TimeTrialManager manager)
        {
            root = targetRoot; resultsText = targetText; timeTrialManager = manager;
        }
    }
}
