using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace AnimeHillRacing.UI
{
    public sealed class TransmissionUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text selectionText;
        [SerializeField] private GameManager gameManager;

        public void Show(TransmissionMode selected)
        {
            root.SetActive(true);
            selectionText.text = $"TRANSMISSION: {selected.ToString().ToUpperInvariant()}\nChoose below, or press M then GAS";
        }

        public void Hide() => root.SetActive(false);
        public void SelectAuto() => gameManager?.BeginRun(TransmissionMode.Auto);
        public void SelectManual() => gameManager?.BeginRun(TransmissionMode.Manual);

        public void Assign(GameObject targetRoot, Text targetText, GameManager manager)
        {
            root = targetRoot;
            selectionText = targetText;
            gameManager = manager;
        }
    }
}
