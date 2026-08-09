namespace AnimeHillRacing.Core
{
    public enum GameModeType
    {
        Adventure2_5D,
        Adventure3D,
        TimeTrial3D
    }

    public static class GameModeNames
    {
        public static string DisplayName(GameModeType mode)
        {
            switch (mode)
            {
                case GameModeType.Adventure2_5D: return "ADVENTURE";
                case GameModeType.Adventure3D: return "3D";
                default: return "TIME TRIAL";
            }
        }
    }
}
