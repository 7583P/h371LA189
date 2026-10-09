using System;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public static class RacingPositionAuditTests
    {
        public static bool RunAll(Action<string> logPass, Action<string> logFail)
        {
            int passed = 0, total = 0;

            void Check(bool condition, string testName)
            {
                total++;
                if (condition)
                {
                    passed++;
                    logPass?.Invoke("PASS: " + testName);
                }
                else
                {
                    logFail?.Invoke("FAIL: " + testName);
                }
            }

            var root = new GameObject("AuditTestRoot");
            var graph = root.AddComponent<TrackSplineGraph>();
            var nodes = new TrackNode[6];
            for (int i = 0; i < 6; i++)
            {
                float ang = i * Mathf.PI * 2f / 6f;
                nodes[i] = new TrackNode
                {
                    Position = new Vector3(Mathf.Cos(ang) * 100f, 0, Mathf.Sin(ang) * 100f),
                    Forward = new Vector3(-Mathf.Sin(ang), 0, Mathf.Cos(ang)),
                    Up = Vector3.up,
                    Right = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)),
                    TrackWidth = 15f
                };
            }
            graph.InitializeFromNodes(nodes);

            var tracker = root.AddComponent<TrackProgressTracker>();
            tracker.Initialize(graph, 3, 6);

            var carGoA = new GameObject("CarA");
            var carA = carGoA.AddComponent<RacingVehiclePhysics>();
            var recA = carGoA.AddComponent<VehicleRecovery>();
            var progA = tracker.RegisterRacer(new RacerIdentity("a", "Racer A", "car-01", "wheel-default", 1000, false, 1, 1), carA, recA);

            var carGoB = new GameObject("CarB");
            var carB = carGoB.AddComponent<RacingVehiclePhysics>();
            var recB = carGoB.AddComponent<VehicleRecovery>();
            var progB = tracker.RegisterRacer(new RacerIdentity("b", "Racer B", "car-01", "wheel-default", 1000, false, 2, 2), carB, recB);

            // Test 1: Lap 3 racer MUST be sorted ahead of Lap 2 racer regardless of distance
            progA.CurrentLap = 3;
            progA.TotalDistance = 2000f; // Lower distance
            progA.CheckpointsPassedCount = 1;

            progB.CurrentLap = 2;
            progB.TotalDistance = 2500f; // Higher distance
            progB.CheckpointsPassedCount = 4;

            tracker.UpdateProgress(10f);
            Check(progA.RacePosition == 1 && progB.RacePosition == 2, "Lap 3 racer sorted ahead of Lap 2 racer despite lower distance");

            // Test 2: Same lap sorting respects checkpoints and distance
            progA.CurrentLap = 2;
            progA.CheckpointsPassedCount = 3;
            progA.TotalDistance = 1400f;

            progB.CurrentLap = 2;
            progB.CheckpointsPassedCount = 1;
            progB.TotalDistance = 1200f;

            tracker.UpdateProgress(20f);
            Check(progA.RacePosition == 1 && progB.RacePosition == 2, "Same-lap sorting respects checkpoint count and total distance");

            // Test 3: Authored shortcuts cannot grant unvisited required checkpoints.
            tracker.Initialize(graph, 3, 6);
            progA = tracker.RegisterRacer(new RacerIdentity("a", "Racer A", "car-01", "wheel-default", 1000, false, 1, 1), carA, recA);
            tracker.NotifyCheckpoint(carA, 0, true, 1f); // Start crossed
            Check(progA.HasCrossedStart && progA.LastCheckpointIndex == 0, "Gate 0 crossed to start race");

            tracker.NotifyCheckpoint(carA, 2, false, 5f); // Forward skip from 0 to 2
            Check(progA.LastCheckpointIndex == 0 && progA.CheckpointsPassedCount == 0, "Unvisited shortcut checkpoint skip is rejected");

            // Test 4: Invalid early finish line bypass rejected
            tracker.Initialize(graph, 3, 6);
            progA = tracker.RegisterRacer(new RacerIdentity("a", "Racer A", "car-01", "wheel-default", 1000, false, 1, 1), carA, recA);
            tracker.NotifyCheckpoint(carA, 0, true, 1f); // Start
            tracker.NotifyCheckpoint(carA, 1, false, 2f); // Only 1 gate
            tracker.NotifyCheckpoint(carA, 0, true, 3f); // Attempt finish
            Check(progA.CurrentLap == 1 && !progA.Finished, "Invalid early finish line bypass is rejected");

            // Test 5: Recovery preserves valid progress and does not regress lap
            tracker.Initialize(graph, 3, 6);
            progA = tracker.RegisterRacer(new RacerIdentity("a", "Racer A", "car-01", "wheel-default", 1000, false, 1, 1), carA, recA);
            tracker.NotifyCheckpoint(carA, 0, true, 1f);
            for (int g = 1; g < 6; g++) tracker.NotifyCheckpoint(carA, g, false, 1f + g);
            tracker.NotifyCheckpoint(carA, 0, true, 8f); // Completed Lap 1 -> enters Lap 2
            Check(progA.CurrentLap == 2, "Completed Lap 1, now on Lap 2");

            progA.SnapshotSafeState(Vector3.zero, Quaternion.identity, 0, 50f, 650f);
            tracker.NotifyVehicleRecovered(carA);
            Check(progA.CurrentLap == 2 && progA.TotalDistance >= 650f, "Recovery preserves Lap 2 and does not regress total distance");

            // Test 6: NormalizedProgress computation
            tracker.UpdateProgress(30f);
            Check(progA.NormalizedProgress > 0f && progA.NormalizedProgress <= 1f, "NormalizedProgress is bounded between 0 and 1");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(carGoA);
            UnityEngine.Object.DestroyImmediate(carGoB);
            UnityEngine.Object.DestroyImmediate(root);

            return passed == total;
        }
    }
}
