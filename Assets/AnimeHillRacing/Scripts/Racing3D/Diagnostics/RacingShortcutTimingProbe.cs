#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    // Opt-in QA observation only. Fixed-size state and buffered completed spans;
    // no file writes, route choices or authoritative commands during a race.
    public sealed class RacingShortcutTimingProbe : MonoBehaviour
    {
        [Serializable] public struct Span {
            public string racer,route;public bool shortcut;public int recoveries;public float seconds,averageKmh;
        }
        [Serializable] sealed class Report { public string classification="OBSERVED_REAL_ROUTE_PORTAL_TIMES_EQUAL_BUILD_NOT_RANDOMIZED_ROUTE_AB";public List<Span> spans; }
        struct Window { public bool active,shortcut,previousInside;public float started,speed;public int samples,recoveries; }
        RaceManager manager;RacerProgress[] racers;Window[] windows;TrackShortcut[] routes;
        readonly List<Span> spans=new List<Span>(256);
        public void Bind(RaceManager race)
        {
            manager=race;racers=new RacerProgress[race.ProgressTracker.Leaderboard.Count];
            for(int i=0;i<racers.Length;i++)racers[i]=race.ProgressTracker.Leaderboard[i];
            routes=race.ProgressTracker.TrackGraph.Shortcuts;windows=new Window[racers.Length*routes.Length];
        }
        void FixedUpdate()
        {
            if(manager==null||manager.State==RaceState.Countdown||manager.State==RaceState.Results)return;
            var graph=manager.ProgressTracker.TrackGraph;
            for(int c=0;c<racers.Length;c++)for(int r=0;r<routes.Length;r++) {
                var car=racers[c];var route=routes[r];int at=c*routes.Length+r;var w=windows[at];
                float length=Mathf.Repeat(graph.GetNode(route.exitNodeIndex).CumulativeDistance-graph.GetNode(route.entryNodeIndex).CumulativeDistance,graph.TotalLength);
                float along=Mathf.Repeat(car.DistanceAlongTrack-graph.GetNode(route.entryNodeIndex).CumulativeDistance,graph.TotalLength);
                bool inside=!car.Finished&&along<length;
                if(inside&&!w.previousInside){w.active=true;w.started=manager.RaceElapsed;w.speed=0;w.samples=0;w.shortcut=false;w.recoveries=car.Recovery.RecoveryCount;}
                if(w.active&&inside){w.shortcut|=car.Vehicle.ActiveShortcut==route;w.speed+=car.Vehicle.SpeedKmh;w.samples++;}
                if(w.active&&!inside) {
                    if(spans.Count<256)spans.Add(new Span{racer=car.Identity.displayName,route=route.shortcutId,shortcut=w.shortcut,recoveries=car.Recovery.RecoveryCount-w.recoveries,seconds=manager.RaceElapsed-w.started,averageKmh=w.samples>0?w.speed/w.samples:0});
                    w.active=false;
                }
                w.previousInside=inside;windows[at]=w;
            }
        }
        public void Export(string directory)=>File.WriteAllText(Path.Combine(directory,"shortcut-route-times.json"),JsonUtility.ToJson(new Report{spans=spans},true));
    }
}
#endif
