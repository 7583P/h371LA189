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
        }

        public void Configure(GameModeType gameMode, string selectedMapId, bool preserveMapSelection = false)
        {
            mode = gameMode;
            mapId = selectedMapId;
            preserveSelectedMap = preserveMapSelection;
        }
    }
}
