using System;
using System.Collections.Generic;

namespace AnimeHillRacing.Racing3D
{
    // Lazy, exact lookup over existing persistent identities. Duplicated names
    // retain every matching bot; the current best standing is shown first.
    // No profiles, rating, qualification or synthetic activity are created.
    public sealed class LeagueIdentityLookup
    {
        readonly LocalLeagueEngine engine;
        readonly Dictionary<string,int> heads;
        readonly int[] next;
        public LeagueIdentityLookup(LocalLeagueEngine engine)
        {
            this.engine=engine??throw new ArgumentNullException(nameof(engine));
            heads=new Dictionary<string,int>(engine.Population,StringComparer.OrdinalIgnoreCase);
            next=new int[engine.Population];
            for(int i=0;i<next.Length;i++) {
                string name=engine.DisplayName(i);
                next[i]=heads.TryGetValue(name,out int previous)?previous:-1;
                heads[name]=i;
            }
        }
        public bool TryLocate(string query,out int rank,out int matches)
        {
            rank=-1;matches=0;query=(query??"").Trim();
            if(query.Length==0)return false;
            engine.RefreshHumanIndex();
            // IDs disambiguate duplicated display names and always take priority.
            int exact=engine.Rank(query.ToLowerInvariant());
            if(exact>=0){rank=exact;matches=1;return true;}
            if(query.Equals("PLAYER",StringComparison.OrdinalIgnoreCase)||query.Equals(engine.State.human.displayName,StringComparison.OrdinalIgnoreCase)) {
                rank=engine.Index.RankOf(engine.Population);matches=1;
            }
            if(heads.TryGetValue(query,out int node))for(int i=node;i>=0;i=next[i]) {
                int standing=engine.Index.RankOf(i);if(rank<0||standing<rank)rank=standing;matches++;
            }
            return rank>=0;
        }
    }
}
