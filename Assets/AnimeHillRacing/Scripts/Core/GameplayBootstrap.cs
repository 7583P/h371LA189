using UnityEngine;

namespace AnimeHillRacing.Core
{
    [DefaultExecutionOrder(-200)]
    public sealed class GameplayBootstrap : MonoBehaviour
    {
        [SerializeField] private GameModeType mode;
        [SerializeField] private string mapId;
        [SerializeField] private bool preserveSelectedMap;

        private void Awake()
        {
            GameSession.SelectedGameMode = mode;
            if (!preserveSelectedMap || string.IsNullOrEmpty(GameSession.SelectedMapId))
                GameSession.SelectedMapId = mapId;
            if (mode == GameModeType.Adventure2_5D) DisableLegacySunDisc();
        }

        private static void DisableLegacySunDisc()
        {
            GameObject sunDisc = GameObject.Find("Sun");
            // Old Adventure scenes contained a yellow primitive named Sun. Keep the separate directional
            // "Sun Light" used for lighting; only remove the visible mesh if a legacy scene still has it.
            if (sunDisc != null && sunDisc.GetComponent<Light>() == null)
                sunDisc.SetActive(false);
        }

        public void Configure(GameModeType gameMode, string selectedMapId, bool preserveMapSelection = false)
        {
            mode = gameMode;
            mapId = selectedMapId;
            preserveSelectedMap = preserveMapSelection;
        }
    }
}
