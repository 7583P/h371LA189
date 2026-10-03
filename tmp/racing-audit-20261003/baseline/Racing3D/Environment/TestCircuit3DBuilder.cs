using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public sealed class TestCircuit3DBuilder : MonoBehaviour
    {
        [Header("Circuit Layout Parameters")]
        [SerializeField] private float roadWidth = 14f;
        [SerializeField] private Material roadMaterial;
        [SerializeField] private Material wallMaterial;
        [SerializeField] private Material shortcutMaterial;

        [Header("Generated References")]
        [SerializeField] private TrackSplineGraph graph;
        [SerializeField] private StartingGrid grid;

        public TrackSplineGraph Graph => graph;
        public StartingGrid Grid => grid;

        private void Awake()
        {
            if (graph == null || graph.NodeCount == 0)
            {
                BuildCircuit();
            }
        }

        public void BuildCircuit()
        {
            // Clear existing generated children
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }

            // 1. Authoring 16 Track Nodes forming a rich technical circuit:
            // - Straight (0..3)
            // - Gentle Curve (4..6)
            // - Sharp Hairpin (7..9)
            // - Elevation Rise / S-Curve (10..12)
            // - Drop & Return Straight (13..15)
            Vector3[] nodePositions = new[]
            {
                new Vector3(0f, 0f, 0f),        // Node 0: Start / Finish Line
                new Vector3(0f, 0f, 60f),       // Node 1: Main straight
                new Vector3(0f, 0f, 130f),      // Node 2: Main straight
                new Vector3(0f, 0f, 200f),      // Node 3: Braking zone into curve
                new Vector3(30f, 0f, 260f),     // Node 4: Gentle right curve entry
                new Vector3(90f, 0f, 290f),     // Node 5: Gentle curve apex
                new Vector3(160f, 0f, 270f),    // Node 6: Curve exit
                new Vector3(200f, 0f, 220f),    // Node 7: Entry to Hairpin
                new Vector3(220f, 0f, 160f),    // Node 8: Hairpin Apex (Drift Zone!)
                new Vector3(180f, 2f, 110f),    // Node 9: Hairpin Exit / Hill climb
                new Vector3(140f, 6f, 70f),     // Node 10: Hill peak / Technical S-Curve entry
                new Vector3(110f, 5f, 20f),     // Node 11: S-Curve flick left
                new Vector3(70f, 3f, -40f),     // Node 12: S-Curve flick right
                new Vector3(20f, 1f, -60f),     // Node 13: Elevation drop
                new Vector3(-40f, 0f, -40f),    // Node 14: Final turn entry
                new Vector3(-20f, 0f, -15f)     // Node 15: Final turn exit into straight
            };

            const int samplesPerNode = 6;
            Vector3[] sampledPositions = RacingTrackGeometry.SmoothLoop(nodePositions, samplesPerNode);
            TrackNode[] authoredNodes = new TrackNode[sampledPositions.Length];
            for (int sample = 0; sample < sampledPositions.Length; sample++)
            {
                int i = sample / samplesPerNode;
                int nextIdx = (i + 1) % nodePositions.Length;
                Vector3 fwd = (sampledPositions[(sample + 1) % sampledPositions.Length] - sampledPositions[(sample + sampledPositions.Length - 1) % sampledPositions.Length]).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;

                // Optimal apex offsets (outside - inside - outside)
                float optOffset = 0f;
                bool isDrift = false;
                float targetSpeed = 150f;

                if (i == 4) optOffset = -0.5f;       // Entry wide left
                else if (i == 5) { optOffset = 0.6f; targetSpeed = 120f; } // Apex tight right
                else if (i == 6) optOffset = -0.4f;      // Exit wide
                else if (i == 7) optOffset = -0.6f;      // Hairpin entry wide
                else if (i == 8) { optOffset = 0.75f; isDrift = true; targetSpeed = 75f; } // Hairpin apex
                else if (i == 9) optOffset = -0.5f;      // Hairpin exit
                else if (i == 11) { optOffset = -0.5f; targetSpeed = 95f; } // Chicane left
                else if (i == 12) { optOffset = 0.5f; targetSpeed = 100f; } // Chicane right

                authoredNodes[sample] = new TrackNode
                {
                    Position = sampledPositions[sample],
                    Forward = fwd,
                    Right = right,
                    Up = Vector3.up,
                    TrackWidth = roadWidth,
                    SafeLineOffset = 0f,
                    OptimalLineOffset = optOffset,
                    TargetSpeedKmh = targetSpeed,
                    IsDriftZone = isDrift,
                    CumulativeDistance = 0f
                };
            }

            // 2. Author a technical shortcut (Dirt cut cutting across nodes 10 to 13)
            TrackShortcut shortcut = new TrackShortcut
            {
                shortcutId = "chicane_dirt_cut",
                displayName = "CHICANE DIRT CUT",
                entryNodeIndex = 10 * samplesPerNode,
                exitNodeIndex = 13 * samplesPerNode,
                requiresBoost = true,
                minSkillRequired = 1.1f, // Internal shortcut has no production surface; exclude from bot navigation.
                riskLevel = 0.6f,
                pathPoints = new[]
                {
                    new Vector3(140f, 6f, 70f),
                    new Vector3(90f, 4f, 15f), // Direct jump through infield
                    new Vector3(20f, 1f, -60f)
                }
            };

            // 3. Setup Graph Component
            GameObject graphObj = new GameObject("TrackSplineGraph");
            graphObj.transform.SetParent(transform, false);
            graph = graphObj.AddComponent<TrackSplineGraph>();
            graph.InitializeFromNodes(authoredNodes, new[] { shortcut });

            // 4. Build Procedural Road Geometry & Colliders
            GameObject roadRoot = new GameObject("RoadGeometry");
            roadRoot.transform.SetParent(transform, false);

            Material roadMat = roadMaterial != null ? roadMaterial : new Material(Shader.Find("Standard")) { color = new Color(.18f,.2f,.24f) };
            RacingTrackGeometry.Ribbon(roadRoot.transform, "Continuous regression road", authoredNodes, -roadWidth * .5f, roadWidth * .5f, roadMat, RacingSurfaceKind.Road);
            /* Legacy slabs preserved in source for comparison; the continuous mesh avoids gaps at turns.
            for (int i = 0; i < authoredNodes.Length; i++)
            {
                int next = (i + 1) % authoredNodes.Length;
                Vector3 pA = authoredNodes[i].Position;
                Vector3 pB = authoredNodes[next].Position;
                Vector3 mid = (pA + pB) * 0.5f;
                float segLen = Vector3.Distance(pA, pB);
                Vector3 dir = (pB - pA).normalized;

                // Road segment slab
                GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slab.name = $"RoadSegment_{i:00}";
                slab.transform.SetParent(roadRoot.transform, false);
                slab.transform.position = mid - Vector3.up * 0.25f;
                slab.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                slab.transform.localScale = new Vector3(roadWidth, 0.5f, segLen + 0.5f);

                var r = slab.GetComponent<Renderer>();
                if (r != null)
                {
                    if (roadMaterial != null) r.material = roadMaterial;
                    else r.material.color = new Color(0.18f, 0.18f, 0.20f);
                }
            }

            */
            // 5. Build Starting Grid (12 positions at Start Line Node 0)
            GameObject gridObj = new GameObject("StartingGrid");
            gridObj.transform.SetParent(transform, false);
            gridObj.transform.position = authoredNodes[0].Position;
            gridObj.transform.rotation = Quaternion.LookRotation(authoredNodes[0].Forward, Vector3.up);
            grid = gridObj.AddComponent<StartingGrid>();

            // 6. Build Checkpoint Triggers
            GameObject checkRoot = new GameObject("Checkpoints");
            checkRoot.transform.SetParent(transform, false);

            int[] checkpointNodeIndices = new[] { 0, 2, 5, 8, 10, 12, 14 };
            for (int c = 0; c < checkpointNodeIndices.Length; c++)
            {
                int nIdx = checkpointNodeIndices[c] * samplesPerNode;
                TrackNode n = authoredNodes[nIdx];

                GameObject cpObj = new GameObject($"Checkpoint_{c:00}{(c == 0 ? "_FinishLine" : "")}");
                cpObj.transform.SetParent(checkRoot.transform, false);
                cpObj.transform.position = n.Position + Vector3.up * 2f;
                cpObj.transform.rotation = Quaternion.LookRotation(n.Forward, Vector3.up);

                BoxCollider box = cpObj.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(roadWidth * 2f, 8f, 12f);

                var trigger = cpObj.AddComponent<CheckpointTrigger3D>();
                trigger.Configure(c, c == 0);
            }

            // 7. Build Item Boxes
            GameObject itemBoxesRoot = new GameObject("ItemBoxes");
            itemBoxesRoot.transform.SetParent(transform, false);

            // Place item box rows at straightaway (node 1) and mid curve (node 6)
            int[] itemNodes = new[] { 1, 6 };
            for (int b = 0; b < itemNodes.Length; b++)
            {
                TrackNode n = authoredNodes[itemNodes[b] * samplesPerNode];
                for (int slot = -2; slot <= 2; slot += 2)
                {
                    Vector3 boxPos = n.Position + (n.Right * (slot * 2.2f)) + Vector3.up * 1.0f;
                    GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    box.name = $"ItemBox_Row{b}_{slot}";
                    box.transform.SetParent(itemBoxesRoot.transform, false);
                    box.transform.position = boxPos;
                    box.transform.localScale = Vector3.one * 1.1f;
                    box.GetComponent<Collider>().isTrigger = true;

                    var r = box.GetComponent<Renderer>();
                    if (r != null) r.material.color = new Color(1f, 0.85f, 0.1f, 0.8f);

                    box.AddComponent<ItemBox>();
                }
            }

            Debug.Log($"[TestCircuit3DBuilder] Successfully built technical 3D circuit with {authoredNodes.Length} nodes, 1 shortcut, {checkpointNodeIndices.Length} checkpoints, and 12-slot starting grid!");
        }
    }
}
