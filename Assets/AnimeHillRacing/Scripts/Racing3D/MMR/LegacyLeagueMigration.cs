using System;
using System.Collections.Generic;

namespace AnimeHillRacing.Racing3D
{
    // Read-only V2 compatibility DTO. It is never used by normal progression,
    // matchmaking, telemetry or writing. Existing old journals remain untouched.
    [Serializable] public sealed class LegacyLocalLeagueState
    {
        public int version,populationSeed,currentSeasonNumber,lastProcessedSeasonNumber,matchmakingSequence;
        public long installationTimestamp,seasonStartTimestamp,seasonEndTimestamp,lastKnownUtcTimestamp,lastLeagueSimulationUtc,simulationEventNumber,skippedCatchupEvents;
        public LeagueCompetitor human;
        public LeagueCompetitor[] bots;
        public List<string> recentOpponentIds;
    }
    public static class LegacyLeagueMigration
    {
        public static int Rating(int oldRating)=>Math.Max(0,oldRating)/10;
        static void Scale(LeagueCompetitor competitor)
        {
            competitor.currentMmr=Rating(competitor.currentMmr);competitor.peakMmr=Rating(competitor.peakMmr);
            competitor.seasonStartingMmr=Rating(competitor.seasonStartingMmr);competitor.seasonPeakMmr=Rating(competitor.seasonPeakMmr);
            foreach(var h in competitor.history){h.startingMmr=Rating(h.startingMmr);h.endingMmr=Rating(h.endingMmr);h.peakMmr=Rating(h.peakMmr);h.nextStartingMmr=Rating(h.nextStartingMmr);
                h.startingRank=MmrTierUtility.GetDisplayName(h.startingMmr);h.endingRank=MmrTierUtility.GetDisplayName(h.endingMmr);h.peakRank=MmrTierUtility.GetDisplayName(h.peakMmr);h.beyondPeakCounter=MmrTierUtility.GetBeyondCounter(h.peakMmr);}
        }
        public static LocalLeagueState Convert(LegacyLocalLeagueState old,int population)
        {
            if(old==null || old.version!=2 || old.human==null || old.bots==null || old.bots.Length!=1000 || old.installationTimestamp<=0)throw new ArgumentException("Invalid legacy local league.");
            var state=LocalLeagueEngine.Create(old.installationTimestamp,old.populationSeed,Math.Max(population,old.bots.Length));
            state.currentSeasonNumber=old.currentSeasonNumber;state.lastProcessedSeasonNumber=old.lastProcessedSeasonNumber;state.seasonStartTimestamp=old.seasonStartTimestamp;state.seasonEndTimestamp=old.seasonEndTimestamp;
            state.lastKnownUtcTimestamp=old.lastKnownUtcTimestamp;state.lastLeagueSimulationUtc=old.lastLeagueSimulationUtc;state.simulationEventNumber=old.simulationEventNumber;state.skippedCatchupEvents=old.skippedCatchupEvents;state.matchmakingSequence=old.matchmakingSequence;
            state.human=old.human.CopyForSave();Scale(state.human);state.recentOpponentIds=new List<string>(old.recentOpponentIds??new List<string>());
            for(int i=0;i<old.bots.Length;i++){
                var source=old.bots[i];if(source==null || source.id!=LeagueBotCatalog.Id(old.populationSeed,i) || source.profile==null)throw new ArgumentException("Invalid persistent legacy identity.");
                var c=source.CopyForSave();Scale(c);var p=c.profile;var b=state.bots[i];
                b.personalitySeed=c.personalitySeed;b.currentMmr=c.currentMmr;b.peakMmr=c.peakMmr;b.seasonStartingMmr=c.seasonStartingMmr;b.seasonPeakMmr=c.seasonPeakMmr;
                b.races=c.races;b.wins=c.wins;b.podiums=c.podiums;b.seasonRaces=c.seasonRaces;b.seasonWins=c.seasonWins;b.seasonPodiums=c.seasonPodiums;
                b.shortcutSuccesses=c.shortcutSuccesses;b.aerialSuccesses=c.aerialSuccesses;b.landingSuccesses=c.landingSuccesses;b.bestLap=c.bestLap;b.seasonBestLap=c.seasonBestLap;
                b.latent=CompactLeagueBot.Pack01(p.latentSkill);b.learning=CompactLeagueBot.Pack01(p.learningPotential);b.consistency=CompactLeagueBot.Pack01(p.consistency);b.uncertainty=CompactLeagueBot.Pack01(c.uncertainty);b.form=CompactLeagueBot.PackForm(c.recentForm);b.style=(byte)p.preferredStyle;
                b.aggression=CompactLeagueBot.PackByte(p.aggression);b.risk=CompactLeagueBot.PackByte(p.riskTolerance);b.drift=CompactLeagueBot.PackByte(p.driftSkill);b.aerial=CompactLeagueBot.PackByte(p.aerialSkill);b.shortcut=CompactLeagueBot.PackByte(p.shortcutSkill);b.item=CompactLeagueBot.PackByte(p.itemSkill);b.racecraft=CompactLeagueBot.PackByte(p.racecraft);b.recovery=CompactLeagueBot.PackByte(p.recoverySkill);
                b.vehicle=LeagueBotCatalog.Lookup(LeagueBotCatalog.Vehicles,c.vehiclePreference);b.wheels=LeagueBotCatalog.Lookup(LeagueBotCatalog.Wheels,c.wheelPreference);
                b.character=LeagueBotCatalog.Lookup(LeagueBotCatalog.Characters,c.characterPreference);b.accessory=LeagueBotCatalog.Lookup(LeagueBotCatalog.Accessories,c.accessoryPreference);b.lastLeaderboardPosition=c.lastLeaderboardPosition;
                b.titleFlags=0;foreach(string id in c.unlockedTitleIds){byte title=LeagueBotCatalog.Lookup(LeagueBotCatalog.Titles,id);if(title>0)b.titleFlags|=(ushort)(1<<title);}
                if(c.selectedTitleId!=null && c.selectedTitleId.StartsWith("season:",StringComparison.Ordinal)){var parts=c.selectedTitleId.Split(':');if(parts.Length==3 && int.TryParse(parts[1],out int season) && int.TryParse(parts[2],out int band)){b.selectedTitle=(ushort)(32+band);b.selectedTitleSeason=season;}}
                else b.selectedTitle=LeagueBotCatalog.Lookup(LeagueBotCatalog.Titles,c.selectedTitleId);
                state.bots[i]=b;state.botAliases.Add(new LegacyBotAlias{index=i,displayName=c.displayName});
                foreach(var h in c.history){
                    var archive=state.archives.Find(a=>a.seasonNumber==h.seasonNumber);
                    if(archive==null){archive=new LeagueSeasonArchive{seasonNumber=h.seasonNumber,population=state.population,startTimestamp=h.seasonStartTimestamp,endTimestamp=h.seasonEndTimestamp,records=new CompactBotSeason[state.population]};state.archives.Add(archive);}
                    archive.records[i]=new CompactBotSeason{startingMmr=h.startingMmr,endingMmr=h.endingMmr,peakMmr=h.peakMmr,nextStartingMmr=h.nextStartingMmr,races=h.races,wins=h.wins,podiums=h.podiums,bestLap=h.bestLap,selectedTitle=b.selectedTitle,selectedTitleSeason=b.selectedTitleSeason};
                }
            }
            state.archives.Sort((a,b)=>a.seasonNumber.CompareTo(b.seasonNumber));while(state.archives.Count>20)state.archives.RemoveAt(0);
            if(!LocalLeagueStore.Valid(state))throw new ArgumentException("Legacy migration produced contradictory state.");return state;
        }
    }
}
