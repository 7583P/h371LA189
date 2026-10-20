namespace AnimeHillRacing.Racing3D
{
    // Shared by Unity presentation, archives and the source-linked QA harness.
    public static class SeasonalTitleRules
    {
        public static int TopBand(int officialPosition) => officialPosition < 1 || officialPosition > 1000 ? 0 :
            officialPosition <= 10 ? 10 : officialPosition <= 50 ? 50 : officialPosition <= 100 ? 100 : officialPosition <= 500 ? 500 : 1000;
        public static string Label(int season, int officialPosition) { int band = TopBand(officialPosition); return band == 0 ? "" : "CURRENT SEASON " + season + " TOP " + band; }
    }
}
