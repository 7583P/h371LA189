using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace AnimeHillRacing.Racing3D
{
    // Packed scalar record, no strings, object references, physics or per-bot Update.
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

        public float LatentSkill => latent/65535f;
        public float LearningPotential => learning/65535f;
        public float Consistency => consistency/65535f;
        public float Uncertainty => uncertainty/65535f;
        public float RecentForm => form/32767f;
        public static ushort Pack01(float value)=>(ushort)Math.Round(Math.Max(0,Math.Min(1,value))*65535);
        public static byte PackByte(float value)=>(byte)Math.Round(Math.Max(0,Math.Min(1,value))*255);
        public static short PackForm(float value)=>(short)Math.Round(Math.Max(-1,Math.Min(1,value))*32767);
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
        public long startTimestamp, endTimestamp;
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
        public static readonly string[] Titles={"","current-season-rank","night-city-veteran","shortcut-hunter","aerial-specialist","landing-master","beyond-apex"};
        static readonly string[] First={"Apex","Volt","Nova","Turbo","Shadow","Iron","Blaze","Aero","Pixel","Echo","Viper","Cyber","Pulse","Storm","Hyper","Redline","Zero","Night","Torque","Lunar","Sky","Prism","Ember","Neon","Orbit","Cobalt","Jade","Sonic","Flux","Rift","Zen","Vector"};
        static readonly string[] Last={"Kite","Shift","Raven","Drive","Fox","Runner","Ace","Strike","Clad","Surge","GT","Track","Drift","Wing","Comet","Arc","Relay","Peak","Wave","Line","Dash","Bolt","Reef","Jet","Rush","Glide","Turn","Spark","Link","Ghost","Bloom","Core"};

        public static string Id(int populationSeed,int index)=>"localbot-"+((uint)populationSeed).ToString("x8")+"-"+index.ToString("x4");
        public static bool TryIndex(int populationSeed,string id,out int index)
        {
            index=-1;string prefix="localbot-"+((uint)populationSeed).ToString("x8")+"-";
            return id!=null && id.StartsWith(prefix,StringComparison.Ordinal) && int.TryParse(id.Substring(prefix.Length),NumberStyles.HexNumber,CultureInfo.InvariantCulture,out index);
        }
        public static string Name(int populationSeed,int index)
        {
            int permutation=unchecked((index*(((populationSeed&511)<<1)|1)+(populationSeed>>9))&1023);
            return First[permutation>>5]+Last[permutation&31]+(index<1024?"":"-"+(index/1024).ToString("X"));
        }
        public static string Title(CompactLeagueBot bot)=>bot.selectedTitle>=32?
            "season:"+bot.selectedTitleSeason+":"+(bot.selectedTitle-32):Titles[Math.Min(Titles.Length-1,bot.selectedTitle)];
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
                racecraft=bot.racecraft/255f,recoverySkill=bot.recovery/255f};
        }
        public static LeagueCompetitor View(LocalLeagueState state,int index,string alias=null)
        {
            var b=state.bots[index];var titles=new List<string>(8);
            for(int i=1;i<Titles.Length;i++)if((b.titleFlags&(1<<i))!=0)titles.Add(Titles[i]);
            string selected=Title(b);if(b.selectedTitle>=32)titles.Add(selected);
            return new LeagueCompetitor {id=Id(state.populationSeed,index),displayName=alias??Name(state.populationSeed,index),
                personalitySeed=b.personalitySeed,currentMmr=b.currentMmr,peakMmr=b.peakMmr,seasonStartingMmr=b.seasonStartingMmr,
                seasonPeakMmr=b.seasonPeakMmr,races=b.races,wins=b.wins,podiums=b.podiums,seasonRaces=b.seasonRaces,seasonWins=b.seasonWins,
                seasonPodiums=b.seasonPodiums,shortcutSuccesses=b.shortcutSuccesses,aerialSuccesses=b.aerialSuccesses,landingSuccesses=b.landingSuccesses,
                bestLap=b.bestLap,seasonBestLap=b.seasonBestLap,uncertainty=b.Uncertainty,recentForm=b.RecentForm,profile=Profile(b),
                vehiclePreference=Vehicles[b.vehicle],wheelPreference=Wheels[b.wheels],characterPreference=Characters[b.character],
                accessoryPreference=Accessories[b.accessory],unlockedTitleIds=titles,selectedTitleId=selected,lastLeaderboardPosition=b.lastLeaderboardPosition};
        }
    }
}
