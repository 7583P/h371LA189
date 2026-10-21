using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AnimeHillRacing.Racing3D;

namespace CompetitiveQA
{
    // Interoperability regression for real Unity receipt metadata. These DTO
    // fixtures are never settled, saved as profiles or treated as physical races.
    public static class ReleaseJsonContracts
    {
        public static int Run(string output)
        {
            string target=Path.Combine(output,"release-json-contracts.json");
            if(File.Exists(target))throw new IOException("Preserve serialization contracts");
            int passed=0;var failures=new List<string>();var successes=new List<string>();
            void Check(bool ok,string name){if(ok){passed++;successes.Add(name);}else failures.Add(name);}
            var numeric=Json.From<RaceSettlementRecord>("{\"raceId\":\"serialization-fixture\",\"outcomes\":[0,1,2,255]}");
            Check(numeric.outcomes.SequenceEqual(new byte[]{0,1,2,255}),"Native Unity numeric byte arrays preserve all byte values");
            string base64=Convert.ToBase64String(numeric.outcomes);
            var legacy=Json.From<RaceSettlementRecord>("{\"outcomes\":\""+base64+"\"}");
            Check(legacy.outcomes.SequenceEqual(numeric.outcomes),"Legacy CLI base64 journals remain readable");
            string serialized=Json.To(numeric);
            using(var document=JsonDocument.Parse(serialized))
                Check(document.RootElement.GetProperty("outcomes").ValueKind==JsonValueKind.String&&document.RootElement.GetProperty("outcomes").GetString()==base64,"CLI writer remains byte-compatible with existing fixture format");
            Check(Json.From<RaceSettlementRecord>(serialized).outcomes.SequenceEqual(numeric.outcomes),"Receipt metadata round trip preserves outcomes");
            Check(Json.From<RaceSettlementRecord>("{\"outcomes\":[]}").outcomes.Length==0,"Empty native byte array remains empty");
            Check(Json.From<RaceSettlementRecord>("{\"outcomes\":\"\"}").outcomes.Length==0,"Empty legacy base64 remains readable");
            Check(Json.From<RaceSettlementRecord>("{\"outcomes\":null}").outcomes==null,"Null retains normal serializer semantics");
            foreach(string value in new[]{"[-1]","[256]","[1.5]","[\"1\"]","{}","true","[1,2"}) {
                bool rejected=false;
                try {Json.From<RaceSettlementRecord>("{\"outcomes\":"+value+"}");}
                catch(JsonException){rejected=true;}
                Check(rejected,"Reject invalid byte metadata: "+value);
            }
            File.WriteAllText(target,Json.Pretty(new{classification="QA_SERIALIZER_INTEROPERABILITY_NOT_GAMEPLAY_OR_NEW_PROFILES",passed,failed=failures.Count,successes,failures}));
            Console.WriteLine($"Release JSON interoperability: {passed} passed, {failures.Count} failed");
            return failures.Count==0?0:1;
        }
    }
}
