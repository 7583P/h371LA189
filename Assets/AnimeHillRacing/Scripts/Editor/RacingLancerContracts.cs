#if UNITY_EDITOR
using System;
using System.IO;
using AnimeHillRacing.Racing3D;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.EditorTools
{
    public static class RacingLancerContracts
    {
        [Serializable] sealed class RollEvidence { public int rollsPerPosition; public int[] lancerCounts; public float[] probability; }
        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();int at = Array.IndexOf(args, "--closure-output");
            if (at < 0 || at + 1 >= args.Length) throw new ArgumentException("Missing --closure-output");
            string output = args[at + 1];Directory.CreateDirectory(output);
            var report = new ClosureContractReport();
            var previous = SceneManager.GetActiveScene();
            if (Application.isBatchMode && string.IsNullOrEmpty(previous.path))
                previous = EditorSceneManager.OpenScene("Assets/AnimeHillRacing/Scenes/PreStart.unity");
            var fixture = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try {
                SceneManager.SetActiveScene(fixture);
                report.Check((int)RaceItemType.Lancer == 15 && (int)RaceItemType.Coin10 == 13 && (int)RaceItemType.MiniTurbo == 14,
                    "Lancer appends ID 15 without renumbering persisted inventory IDs");
                var config = Resources.Load<RacingItemBalanceConfiguration>("RacingItemBalanceConfiguration");
                report.Check(config != null && config.leader.Length == 2 && Array.TrueForAll(config.leader,e=>e.Type==RaceItemType.Coin10||e.Type==RaceItemType.ElectroSpike)&&config.front.Length==5&&!Array.Exists(config.front,e=>e.Type==RaceItemType.Lancer||e.Type==RaceItemType.HyperCharge),
                    "Leader is restricted to Coin/ordinary Rock; Golden Rock remains available behind the leader");
                report.Check(config != null && config.middle.Length == 8 && !Array.Exists(config.middle,e=>e.Type==RaceItemType.Lancer) && Array.Exists(config.middle,e=>e.Type==RaceItemType.MiniTurbo)&&Array.Exists(config.middle,e=>e.Type==RaceItemType.GoldenRock),
                    "Midfield P4-P8 mixes Mini Turbo, Golden Rock and tactical items without Lancer");
                int sum = 0, lancerWeight = 0;
                foreach (var entry in ItemDistributionTable.WeightsForPosition(12)) { sum += entry.Weight; if (entry.Type == RaceItemType.Lancer) lancerWeight += entry.Weight; }
                report.Check(sum == 100 && lancerWeight == 8, "Rear table has 100 nominal weight units, including eight for Lancer");
                var evidence = new RollEvidence { rollsPerPosition = 24000, lancerCounts = new int[12], probability = new float[12] };
                for (int place = 1; place <= 12; place++) {
                    var rng = new DeterministicRng(4207 + place);
                    for (int i = 0; i < evidence.rollsPerPosition; i++)
                        if (ItemDistributionTable.RollItem(new ItemRollContext { position = place, lap = 1, totalLaps = 3 }, rng) == RaceItemType.Lancer) evidence.lancerCounts[place - 1]++;
                    float probability = evidence.probability[place - 1] = evidence.lancerCounts[place - 1] / (float)evidence.rollsPerPosition;
                    report.Check(place <= ItemDistributionTable.MiddleLastPosition ? probability == 0 : probability > .01f,
                        "P" + place + ": Lancer " + (place <= ItemDistributionTable.MiddleLastPosition ? "cannot roll" : "is attainable") + " over 24000 deterministic rolls");
                    if (place >= 10) report.Check(probability > evidence.probability[place - 2], "Lancer probability increases at P" + place);
                }
                report.Check(evidence.probability[11] > evidence.probability[6] * 4 && evidence.probability[11] > .13f && evidence.probability[11] < .17f,
                    "Last-place Lancer chance is strongly biased toward the rear without a guaranteed reward");
                var rngHot = new DeterministicRng(12);ItemDistributionTable.RollItem(12, rngHot);
                long allocated = GC.GetAllocatedBytesForCurrentThread();for (int i = 0; i < 10000; i++) ItemDistributionTable.RollItem(7 + i % 6, rngHot);
                allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
                report.Check(allocated == 0, "10000 warmed rear rolls including Lancer allocate zero managed bytes");
                report.Check(!RaceItemUtility.IsShortcutBoost(RaceItemType.Lancer) && RaceItemUtility.LegacyFeatureFamily(15) == 1,
                    "Lancer cannot be reserved as shortcut fuel and preserves the installed model's boost family");
                var a = new GameObject("Lancer pickup mesh contract");var b = new GameObject("Lancer transformed mesh contract");
                try {
                    RacingItemArt.Build(a.transform, RaceItemType.Lancer);RacingLancerArt.Build(b.transform);
                    var aa = a.GetComponentsInChildren<MeshFilter>();var bb = b.GetComponentsInChildren<MeshFilter>();
                    bool shared = aa.Length >= 7 && aa.Length == bb.Length;int vertices = 0;
                    for (int i = 0; i < aa.Length; i++) {
                        shared &= aa[i].sharedMesh == bb[i].sharedMesh;vertices += aa[i].sharedMesh.vertexCount;
                    }
                    report.Check(shared && vertices > 250, "Pickup and transformed vehicle reuse the same faceted rocket/meteor meshes");
                    bool supported = true;foreach (var r in a.GetComponentsInChildren<MeshRenderer>()) supported &= r.enabled && r.sharedMaterial != null && r.sharedMaterial.shader.isSupported;
                    report.Check(supported && a.GetComponentsInChildren<Collider>().Length == 0 && b.GetComponentsInChildren<Rigidbody>().Length == 0,
                        "Lancer artwork uses supported visible materials and adds no physics geometry");
                } finally { UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b); }
                File.WriteAllText(Path.Combine(output, "lancer-probability.json"), JsonUtility.ToJson(evidence, true));
            } finally {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(fixture, true);
            }
            File.WriteAllText(Path.Combine(output, "lancer-contracts.json"), JsonUtility.ToJson(report, true));
            if (report.failed != 0) throw new InvalidOperationException("Lancer contracts failed: " + report.failed);
            Debug.Log("LANCER_CONTRACTS passed=" + report.passed + " failed=" + report.failed);
            // Only the affected item/product boundary checks, not the broad race,
            // physics, season or ML training suites already validated previously.
            RacingProductContracts.RunTargeted();
        }
    }
}
#endif
