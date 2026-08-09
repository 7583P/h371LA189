using UnityEngine;

namespace AnimeHillRacing.Core
{
    [DefaultExecutionOrder(-200)]
    public sealed class GameplayBootstrap : MonoBehaviour
    {
        [SerializeField] private GameModeType mode;
        [SerializeField] private string mapId;

        private void Awake()
        {
            GameSession.SelectedGameMode = mode;
            GameSession.SelectedMapId = mapId;
        }

        public void Configure(GameModeType gameMode, string selectedMapId)
        {
            mode = gameMode;
            mapId = selectedMapId;
        }
    }
}
