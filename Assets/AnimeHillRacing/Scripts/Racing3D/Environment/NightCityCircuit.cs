using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    // One authored route owns navigation, collision widths, checkpoints and the minimap.
    public static class NightCityCircuit
    {
        public const string Name = "NIGHT CITY / GRAND CIRCUIT";
        public const int SamplesPerCorner = 8;
        // Protect the crest approach and the landing after the final main jump.
        // Both authored cuts still rejoin before the next required gate.
        public static readonly int[] Checkpoints = { 0, 8, 40, 72, 104, 136, 168, 184 };
        public static readonly string[] Sectors = { "01 / NEON BOULEVARD", "02 / VERTICAL CITY", "03 / SKYLINE RUSH" };

        public static TrackNode[] CreateNodes()
        {
            Vector3[] control = {
                new Vector3(0,5,0), new Vector3(0,5,115), new Vector3(45,8,270),
                new Vector3(150,18,470), new Vector3(265,28,295), new Vector3(315,30,105),
                new Vector3(285,29,20), new Vector3(205,24,-35), new Vector3(160,14,-105),
                new Vector3(240,10,-165), new Vector3(430,9,-205), new Vector3(475,8,-295),
                new Vector3(315,7,-380), new Vector3(205,6,-400), new Vector3(150,7,-330),
                new Vector3(105,9,-245), new Vector3(15,10,-250), new Vector3(-30,12,-350),
                new Vector3(-125,15,-390), new Vector3(-225,12,-295), new Vector3(-245,10,-165),
                new Vector3(-180,7,-75), new Vector3(-90,5,-85), new Vector3(0,5,-70)
            };
            Vector3[] positions = RacingTrackGeometry.SmoothLoop(control, SamplesPerCorner);
            var nodes = new TrackNode[positions.Length];
            for (int i = 0; i < nodes.Length; i++)
            {
                Vector3 forward = (positions[(i+1)%nodes.Length]-positions[(i+nodes.Length-1)%nodes.Length]).normalized;
                Vector3 ahead = (positions[(i+3)%nodes.Length]-positions[i]).normalized;
                Vector3 behind = (positions[i]-positions[(i+nodes.Length-3)%nodes.Length]).normalized;
                float corner = Vector3.SignedAngle(behind, ahead, Vector3.up);
                float gallery = i > 100 && i < 147 ? Mathf.Sin(Mathf.PI*(i-100)/47) : 0;
                nodes[i] = new TrackNode {
                    Position = positions[i], Forward = forward, Right = Vector3.Cross(Vector3.up, forward).normalized,
                    Up = Vector3.up, TrackWidth = 30-5*gallery,
                    TargetSpeedKmh = 168-Mathf.Clamp(Mathf.Abs(corner)*1.15f, 0, 78),
                    SafeLineOffset = 0, OptimalLineOffset = Mathf.Sign(corner)*Mathf.Clamp01(Mathf.Abs(corner)/45)*.25f,
                    IsDriftZone = Mathf.Abs(corner)>12
                };
            }
            return nodes;
        }

        public static TrackShortcut[] CreateShortcuts(TrackNode[] nodes)
        {
            var sky = new TrackShortcut {
                shortcutId = "crest_cut", displayName = "SKYLINE LINK", entryNodeIndex = 10, exitNodeIndex = 38,
                requiresBoost = true, requiresJump = true, width = 12, launchSpeedKmh = 115,
                exitWidth = nodes[38].TrackWidth,
                launchNodeIndex = 10, gapStartNodeIndex = 11, landingNodeIndex = 13,
                minSkillRequired = .78f, riskLevel = .66f
            };
            var apex = new TrackShortcut {
                shortcutId = "return_drop", displayName = "APEX LINK", entryNodeIndex = 74, exitNodeIndex = 94,
                requiresBoost = false, requiresJump = false, width = 12,
                minSkillRequired = .40f, riskLevel = .32f
            };
            foreach (var route in new[] { sky, apex })
            {
                route.pathPoints = RacingTrackGeometry.ShortcutPath(nodes[route.entryNodeIndex], nodes[route.exitNodeIndex],false,route.requiresJump?.38f:.28f,route.requiresJump?42:0);
                if(route.requiresJump)
                {
                    // A level launch and landing sit between two supported climbs.
                    // Raising the whole approach to the future launch height put
                    // a 25% ramp in the curved entrance and threw cars off it.
                    var run=new float[route.pathPoints.Length];
                    for(int i=1;i<run.Length;i++)run[i]=run[i-1]+Vector3.ProjectOnPlane(route.pathPoints[i]-route.pathPoints[i-1],Vector3.up).magnitude;
                    float start=nodes[route.entryNodeIndex].Position.y,end=nodes[route.exitNodeIndex].Position.y;
                    float platform=Mathf.Lerp(start,end,.635f);
                    for(int i=1;i<24;i++)
                    {
                        route.pathPoints[i].y=i<9?Mathf.Lerp(start,platform,run[i]/run[9]):i<=14?platform:
                            Mathf.Lerp(platform,end,(run[i]-run[14])/(run[24]-run[14]));
                    }
                }
                RacingTrackGeometry.MatchRoadPortals(route, nodes,!route.requiresJump,route.requiresJump?12:1,route.requiresJump?.135f:.10f);
            }
            return new[] { sky, apex };
        }
    }
}
