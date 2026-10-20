using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace AnimeHillRacing.Racing3D
{
    // Packed scalar record, no strings, object references, physics or per-bot Update.
    // v4 appends competitive (CPPS), presence and session fields after the
    // unchanged v3 prefix. Hidden skill (latent/learning/specialties) and
    // visible rating (currentMmr) are independent fields.
    [Serializable, StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct CompactLeagueBot
    {
        public int id, personalitySeed;
        public int currentMmr, peakMmr, seasonStartingMmr, seasonPeakMmr;
        public int races, wins, podiums, seasonRaces, seasonWins, seasonPodiums;
        public int shortcutSuccesses, aerialSuccesses, landingSuccesses;
        public float bestLap, seasonBestLap;
        public ushort latent, learning, consistency, uncertainty;
        public short form;
        public byte style, activity, vehicle, wheels, character, accessory;
        public byte aggression, risk, drift, aerial, shortcut, item, racecraft, recovery;
        public ushort titleFlags, selectedTitle;
        public int lastLeaderboardPosition, selectedTitleSeason, lastSimulationBucket;
        // ---- v4 ----
        public byte placementGames, presence, profile, sessionRacesLeft;
        public byte boost, adaptation, nameStyle, flags;
        public short streak, chronotypeMinutes;
        public int provisionalRating, finishSum, dnfs, eventCounter, activeRace, firstSeason;
        public long nextEventTicks, stateSinceTicks, lastRaceTicks;
        public float habit;

        public const byte FlagNameAssigned = 1;
        public bool Ranked => placementGames >= CompetitiveRating.PlacementRaces;
        public BotPresence Presence => (BotPresence)presence;
        public float LatentSkill => latent/65535f;
        public float LearningPotential => learning/65535f;
        public float Consistency => consistency/65535f;
        public float Uncertainty => uncertainty/65535f;
        public float RecentForm => form/32767f;
        public float AverageFinish => races>0?finishSum/(float)races:0;
        // Effective hidden driving capability after experience. Never derived from MMR.
        public double DevelopedSkill => Math.Min(1.1,LatentSkill+LearningPotential*.045*(1-Math.Exp(-races/1200.0)));
        public static ushort Pack01(float value)=>(ushort)Math.Round(Math.Max(0,Math.Min(1,value))*65535);
        public static byte PackByte(float value)=>(byte)Math.Round(Math.Max(0,Math.Min(1,value))*255);
        public static short PackForm(float value)=>(short)Math.Round(Math.Max(-1,Math.Min(1,value))*32767);
    }

    // Exact v3 on-disk layout, read only by the v3 -> v4 migration.
    [Serializable, StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct CompactLeagueBotV3
    {
        public int id, personalitySeed;
        public int currentMmr, peakMmr, seasonStartingMmr, seasonPeakMmr;
        public int races, wins, podiums, seasonRaces, seasonWins, seasonPodiums;
        public int shortcutSuccesses, aerialSuccesses, landingSuccesses;
        public float bestLap, seasonBestLap;
        public ushort latent, learning, consistency, uncertainty;
        public short form;
        public byte style, activity, vehicle, wheels, character, accessory;
        public byte aggression, risk, drift, aerial, shortcut, item, racecraft, recovery;
        public ushort titleFlags, selectedTitle;
        public int lastLeaderboardPosition, selectedTitleSeason, lastSimulationBucket;
        public CompactLeagueBot Upgrade()=>new CompactLeagueBot{id=id,personalitySeed=personalitySeed,currentMmr=currentMmr,peakMmr=peakMmr,
            seasonStartingMmr=seasonStartingMmr,seasonPeakMmr=seasonPeakMmr,races=races,wins=wins,podiums=podiums,seasonRaces=seasonRaces,
            seasonWins=seasonWins,seasonPodiums=seasonPodiums,shortcutSuccesses=shortcutSuccesses,aerialSuccesses=aerialSuccesses,
            landingSuccesses=landingSuccesses,bestLap=bestLap,seasonBestLap=seasonBestLap,latent=latent,learning=learning,consistency=consistency,
            uncertainty=uncertainty,form=form,style=style,activity=activity,vehicle=vehicle,wheels=wheels,character=character,accessory=accessory,
            aggression=aggression,risk=risk,drift=drift,aerial=aerial,shortcut=shortcut,item=item,racecraft=racecraft,recovery=recovery,
            titleFlags=titleFlags,selectedTitle=selectedTitle,lastLeaderboardPosition=lastLeaderboardPosition,selectedTitleSeason=selectedTitleSeason,
            lastSimulationBucket=lastSimulationBucket,activeRace=-1,placementGames=CompetitiveRating.PlacementRaces};
    }

    // Dense on-disk season archive: 38 bytes/bot/season. Only the requested
    // racer's record is loaded into the profile/history UI.
    [Serializable, StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct CompactBotSeason
    {
        public int startingMmr, endingMmr, peakMmr, nextStartingMmr;
        public int races, wins, podiums;
        public float bestLap;
        public ushort selectedTitle;
        public int selectedTitleSeason;
    }

    [Serializable] public sealed class LeagueSeasonArchive
    {
        public int seasonNumber, population;
        public long startTimestamp, endTimestamp, ratingBefore;
        [NonSerialized] public CompactBotSeason[] records;
        public LeagueSeasonArchive CopyForSave(){var copy=(LeagueSeasonArchive)MemberwiseClone();copy.records=records==null?null:(CompactBotSeason[])records.Clone();return copy;}
    }

    [Serializable] public sealed class LegacyBotAlias {public int index;public string displayName;}

    public static class LeagueBotCatalog
    {
        public static readonly string[] Vehicles={"car-01","hill-adventurer-01","rally-01","formula-01","sport-bike-01","buggy-01","aerial-satelite-01"};
        public static readonly string[] Wheels={"wheel-default","wheel-sport","wheel-racing","wheel-plasma","wheel-carbon","wheel-whity","wheel-offroad"};
        public static readonly string[] Characters={"cobalt-driver","purple-driver","cyan-driver","ruby-driver"};
        public static readonly string[] Accessories={"none","signal-aerial","crystal-crest"};
        public static readonly string[] Titles={"","current-season-rank","night-city-veteran","shortcut-hunter","aerial-specialist","landing-master","beyond-apex","assassiner"};
        static readonly string[] First={"Apex","Volt","Nova","Turbo","Shadow","Iron","Blaze","Aero","Pixel","Echo","Viper","Cyber","Pulse","Storm","Hyper","Redline","Zero","Night","Torque","Lunar","Sky","Prism","Ember","Neon","Orbit","Cobalt","Jade","Sonic","Flux","Rift","Zen","Vector"};
        static readonly string[] Last={"Kite","Shift","Raven","Drive","Fox","Runner","Ace","Strike","Clad","Surge","GT","Track","Drift","Wing","Comet","Arc","Relay","Peak","Wave","Line","Dash","Bolt","Reef","Jet","Rush","Glide","Turn","Spark","Link","Ghost","Bloom","Core"};

        public static string Id(int populationSeed,int index)=>"localbot-"+((uint)populationSeed).ToString("x8")+"-"+index.ToString("x4");
        public static bool TryIndex(int populationSeed,string id,out int index)
        {
            index=-1;string prefix="localbot-"+((uint)populationSeed).ToString("x8")+"-";
            return id!=null && id.StartsWith(prefix,StringComparison.Ordinal) && int.TryParse(id.Substring(prefix.Length),NumberStyles.HexNumber,CultureInfo.InvariantCulture,out index);
        }
        // Generator v1 name: a pure function that was never persisted. Kept for
        // reading pre-CPPS saves; CPPS assigns persisted v2 names once.
        public static string LegacyName(int populationSeed,int index)
        {
            int permutation=unchecked((index*(((populationSeed&511)<<1)|1)+(populationSeed>>9))&1023);
            return First[permutation>>5]+Last[permutation&31]+(index<1024?"":"-"+(index/1024).ToString("X"));
        }
        public static string Name(LocalLeagueState state,int index)=>state.botNames!=null && index<state.botNames.Length && !string.IsNullOrEmpty(state.botNames[index])?state.botNames[index]:LegacyName(state.populationSeed,index);
        public static string Title(CompactLeagueBot bot)=>bot.selectedTitle>=32?
            (bot.selectedTitleSeason<0?"legacy-season:"+(bot.selectedTitleSeason-CppsMigration.LegacySeasonOffset):"season:"+bot.selectedTitleSeason)+":"+(bot.selectedTitle-32):Titles[Math.Min(Titles.Length-1,bot.selectedTitle)];
        public static byte Lookup(string[] table,string id){for(int i=0;i<table.Length;i++)if(table[i]==id)return (byte)i;return 0;}
        public static void UpdateTitles(ref CompactLeagueBot bot)
        {
            bot.titleFlags|=1<<1;
            if(bot.races>=50)bot.titleFlags|=1<<2;
            if(bot.shortcutSuccesses>=20)bot.titleFlags|=1<<3;
            if(bot.aerialSuccesses>=30)bot.titleFlags|=1<<4;
            if(bot.landingSuccesses>=50)bot.titleFlags|=1<<5;
            if(bot.peakMmr>=10000)bot.titleFlags|=1<<6;
        }
        public static LeagueSkillProfile Profile(CompactLeagueBot bot)
        {
            return new LeagueSkillProfile {latentSkill=bot.LatentSkill,consistency=bot.Consistency,learningPotential=bot.LearningPotential,
                preferredStyle=(AiRacingStyle)bot.style,aggression=bot.aggression/255f,riskTolerance=bot.risk/255f,
                driftSkill=bot.drift/255f,aerialSkill=bot.aerial/255f,shortcutSkill=bot.shortcut/255f,itemSkill=bot.item/255f,
                racecraft=bot.racecraft/255f,recoverySkill=bot.recovery/255f,boostSkill=bot.boost/255f,adaptation=bot.adaptation/255f};
        }
        public static LeagueCompetitor View(LocalLeagueState state,int index,string alias=null)
        {
            var b=state.bots[index];var titles=new List<string>(8);
            for(int i=1;i<Titles.Length;i++)if((b.titleFlags&(1<<i))!=0)titles.Add(Titles[i]);
            string selected=Title(b);if(b.selectedTitle>=32)titles.Add(selected);
            return new LeagueCompetitor {id=Id(state.populationSeed,index),displayName=alias??Name(state,index),
                personalitySeed=b.personalitySeed,currentMmr=b.currentMmr,peakMmr=b.peakMmr,seasonStartingMmr=b.seasonStartingMmr,
                seasonPeakMmr=b.seasonPeakMmr,races=b.races,wins=b.wins,podiums=b.podiums,seasonRaces=b.seasonRaces,seasonWins=b.seasonWins,
                seasonPodiums=b.seasonPodiums,shortcutSuccesses=b.shortcutSuccesses,aerialSuccesses=b.aerialSuccesses,landingSuccesses=b.landingSuccesses,
                bestLap=b.bestLap,seasonBestLap=b.seasonBestLap,uncertainty=b.Uncertainty,recentForm=b.RecentForm,profile=Profile(b),
                vehiclePreference=Vehicles[b.vehicle],wheelPreference=Wheels[b.wheels],characterPreference=Characters[b.character],
                accessoryPreference=Accessories[b.accessory],unlockedTitleIds=titles,selectedTitleId=selected,lastLeaderboardPosition=b.lastLeaderboardPosition,
                placementGames=b.placementGames,provisionalRating=b.provisionalRating,streak=b.streak,finishSum=b.finishSum,dnfs=b.dnfs,
                presence=b.presence,activityProfile=b.profile,drivingRating=LeagueSkillMapping.DrivingRating(b.DevelopedSkill),
                qualifiedSeason=(b.flags&SeasonQualification.QualifiedFlag)!=0?state.currentSeasonNumber:0};
        }
    }

    // Maps hidden developed skill (0..1.1) to the 0..10000 driving-capability
    // scale used by the physical AI profile. Visible MMR never feeds this.
    public static class LeagueSkillMapping
    {
        public static int DrivingRating(double developedSkill)
        {
            double t=Math.Max(0,Math.Min(1.06,(developedSkill-.05)/.85));
            return (int)Math.Round(Math.Pow(t,1.15)*10000);
        }
    }
}
