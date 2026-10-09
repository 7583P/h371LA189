#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using AnimeHillRacing.Racing3D;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.EditorTools
{
    [InitializeOnLoad]
    public static class RouteDiscoveryReportRunner
    {
        private const string RequestPath = "Temp/RunRouteDiscovery.request";
        private const string OutputReportPath = "Temp/RouteDiscoveryReport.txt";

        static RouteDiscoveryReportRunner()
        {
            EditorApplication.update -= CheckDiscoveryRequest;
            EditorApplication.update += CheckDiscoveryRequest;
        }

        private static void CheckDiscoveryRequest()
        {
            if (!File.Exists(RequestPath)) return;
            try { File.Delete(RequestPath); } catch {}
            ExecuteDiscovery();
        }

        [MenuItem("Anime Hill Racing/AI/Run Route Discovery Lab")]
        public static void ExecuteDiscovery()
        {
            var builderGo = new GameObject("RouteDiscovery_Builder");
            var builder = builderGo.AddComponent<CanyonRushBuilder>();
            builder.Build();
            var graph = builder.Graph;

            var discovered = RacingRouteDiscoveryLab.DiscoverRoutes(graph, out string report);
            Directory.CreateDirectory("Temp");
            File.WriteAllText(OutputReportPath, report);

            Debug.Log($"<color=cyan>[RouteDiscoveryLab] Completed: Discovered {discovered.Count} valid routes. Report written to {OutputReportPath}</color>");

            UnityEngine.Object.DestroyImmediate(builderGo);
        }
    }
}
#endif
