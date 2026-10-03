#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public static class BlacklineContracts
    {
        public static int Pure(string output)
        {
            var lines=new List<string>();int failed=0;
            void Check(bool valid,string label){lines.Add((valid?"PASS ":"FAIL ")+label);if(!valid)failed++;}
            int[] ratings={1000,10000,25000,50000,75000,90000,100000,125000};
            foreach(int rating in ratings)
            {
                bool seeded=true,builds=true,skills=true,human=true;int exactTotal=0,allMeta=0,smallest=11;
                for(int seed=1;seed<=128;seed++)
                {
                    var lobby=MatchmakingService.GenerateRankedLobby(rating,"car-01","wheel-offroad","CUSTOM HUMAN",seed*7919);
                    var repeat=MatchmakingService.GenerateRankedLobby(rating,"car-01","wheel-offroad","CUSTOM HUMAN",seed*7919);
                    var grid=new HashSet<int>();int exact=0;
                    human&=lobby.Length==12&&lobby[0].isHuman&&lobby[0].vehicleId=="car-01"&&lobby[0].wheelId=="wheel-offroad";
                    for(int i=0;i<lobby.Length;i++)
                    {
                        seeded&=JsonUtility.ToJson(lobby[i])==JsonUtility.ToJson(repeat[i]);grid.Add(lobby[i].gridPosition);
                        if(i==0)continue;
                        if(lobby[i].buildCategory==MetaBuildCategory.Meta){exact++;builds&=lobby[i].vehicleId==MetaProgressionConfig.MetaVehicleId&&lobby[i].wheelId==MetaProgressionConfig.MetaWheelId;}
                        var p=AiSkillModel.CreateProfile(lobby[i].mmr,lobby[i].personalitySeed);
                        foreach(var field in typeof(AiRacerProfile).GetFields())if(field.FieldType==typeof(float))
                        {float value=(float)field.GetValue(p);skills&=float.IsFinite(value)&&value>=0&&value<1;}
                    }
                    builds&=grid.Count==12;exactTotal+=exact;smallest=Mathf.Min(smallest,exact);if(exact==11)allMeta++;
                }
                Check(seeded,rating+" MMR / all lobby identities reproduce RaceSeed");
                Check(human&&builds,rating+" MMR / unique slots, exact META IDs, human build preserved");
                Check(skills,rating+" MMR / finite imperfect execution profiles");
                if(rating>=75000)Check(smallest>=10&&allMeta>=80,rating+" MMR / elite convergence (min="+smallest+", all META="+allMeta+"/128)");
                if(rating==1000)Check(exactTotal<128*4,"Low MMR retains build variety");
            }
            Check(RacingDriftFeedback.StageForCharge(.649f)==MiniTurboStage.None&&RacingDriftFeedback.StageForCharge(.65f)==MiniTurboStage.MiniTurbo&&RacingDriftFeedback.StageForCharge(1.55f)==MiniTurboStage.SuperMiniTurbo&&RacingDriftFeedback.StageForCharge(2.7f)==MiniTurboStage.UltraMiniTurbo,"Drift thresholds: green -> cyan -> purple");
            float previous=0;bool monotonic=true;
            for(int mmr=0;mmr<=125000;mmr+=1000){float p=MetaProgressionConfig.EvaluateMetaProbability(mmr);monotonic&=p>=previous;previous=p;}
            Check(monotonic,"META adoption probability is continuous and monotonic");
            var low=AiSkillModel.CreateProfile(25000,42);var high=AiSkillModel.CreateProfile(75000,42);
            Check(high.throttleSkill>.96f&&high.shortcutExecution>.97f&&high.racingLineSkill-low.racingLineSkill>.35f,"75k has elite execution and a substantial gap over 25k");
            File.WriteAllLines(Path.Combine(output,"pure-contracts.txt"),lines);return failed;
        }

        public static int World(RaceManager manager,string output)
        {
            var lines=new List<string>();int failed=0;
            void Check(bool valid,string label){lines.Add((valid?"PASS ":"FAIL ")+label);if(!valid)failed++;}
            var graph=manager.ProgressTracker.TrackGraph;bool fair=true;
            foreach(var car in manager.SpawnedVehicles)
                fair&=JsonUtility.ToJson(car.Stats)==JsonUtility.ToJson(RacingStatsCalculator.Calculate(car.VehicleId,car.WheelId))&&Mathf.Abs(car.Body.mass-car.Stats.WeightKg)<.01f;
            Check(fair,"All racer stats and mass come from the shared player/AI calculator");
            Physics.SyncTransforms();
            var overlap=new Collider[48];
            var floorHits=new RaycastHit[24];
            foreach(var route in graph.Shortcuts)
            {
                bool gates=true;foreach(int gate in graph.CheckpointNodeIndices)gates&=gate<=route.entryNodeIndex||gate>=route.exitNodeIndex;
                Check(gates,route.shortcutId+" / every mandatory gate is preserved");
                Check(Vector3.Distance(route.pathPoints[0],graph.GetNode(route.entryNodeIndex).Position)<.001f&&Vector3.Distance(route.pathPoints[24],graph.GetNode(route.exitNodeIndex).Position)<.001f,route.shortcutId+" / navigation and collision use exact endpoints");
                bool deck=true,clear=true;float length=0;
                for(int i=0;i<24;i++)
                {
                    Vector3 a=route.pathPoints[i],b=route.pathPoints[i+1],p=(a+b)*.5f;length+=Vector3.Distance(a,b);
                    if(route.requiresJump&&i>=route.gapStartNodeIndex&&i<route.landingNodeIndex)continue;
                    bool supported=false;int floors=Physics.RaycastNonAlloc(p+Vector3.up*.6f,Vector3.down,floorHits,1.2f,~0,QueryTriggerInteraction.Ignore);
                    for(int f=0;f<floors;f++)supported|=floorHits[f].collider.attachedRigidbody==null&&floorHits[f].normal.y>.6f&&Mathf.Abs(floorHits[f].point.y-p.y)<.55f;
                    deck&=supported;
                    int count=Physics.OverlapBoxNonAlloc(p+Vector3.up*.9f,new Vector3(.7f,.18f,1.2f),overlap,Quaternion.LookRotation(b-a,Vector3.up),~0,QueryTriggerInteraction.Ignore);
                    for(int c=0;c<count;c++)if(overlap[c].attachedRigidbody==null&&overlap[c].GetComponent<RacingSurface>()==null)clear=false;
                }
                Check(deck,route.shortcutId+" / grounded decks outside intentional jump gap");
                Check(clear,route.shortcutId+" / car corridor clear of scenery");
                float main=graph.GetNode(route.exitNodeIndex).CumulativeDistance-graph.GetNode(route.entryNodeIndex).CumulativeDistance;
                lines.Add(route.shortcutId+" / main="+main.ToString("F1")+"m route="+length.ToString("F1")+"m saved="+(main-length).ToString("F1")+"m width="+route.width);
            }
            File.WriteAllLines(Path.Combine(output,"world-contracts.txt"),lines);return failed;
        }
    }
}
#endif
