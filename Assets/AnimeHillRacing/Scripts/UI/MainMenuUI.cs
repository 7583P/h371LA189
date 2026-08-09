using AnimeHillRacing.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    public sealed class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Text coinsText;

        private void OnEnable()
        {
            if (coinsText != null)
                coinsText.text = $"COINS\n{SaveManager.TotalCoins:N0}";
        }

        public void Play()
        {
            GameSession.OpenTuningOnPreStart = false;
            GameSession.SelectedGameMode = GameModeType.Adventure2_5D;
            SceneManager.LoadScene("PreStart");
        }

        public void ThreeD()
        {
            GameSession.OpenTuningOnPreStart = false;
            GameSession.SelectedGameMode = GameModeType.Adventure3D;
            SceneManager.LoadScene("PreStart");
        }

        public void Vehicles() => Play();

        public void Garage() => SceneManager.LoadScene("Garage");

        public void TimeTrial()
        {
            GameSession.SelectedGameMode = GameModeType.TimeTrial3D;
            GameSession.SelectedCircuitId = "procedural-test-circuit";
            SceneManager.LoadScene("TimeTrial3D");
        }

        public void Tuning()
        {
            // Legacy event compatibility: tuning now lives exclusively in Garage.
            SceneManager.LoadScene("Garage");
        }

        public void Assign(Text totalCoins) => coinsText = totalCoins;
    }
}
