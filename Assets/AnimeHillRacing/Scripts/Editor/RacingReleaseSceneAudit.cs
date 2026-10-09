#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AnimeHillRacing.EditorTools
{
    public static class RacingReleaseSceneAudit
    {
        [Serializable] sealed class SceneEvidence
        {
            public string scene;
            public int objects, missingScripts, missingMaterials, errorShaders, emptyMeshColliders;
            public int deferredRuntimeMaterials;
            public List<string> failures = new List<string>();
        }
        [Serializable] sealed class Report { public bool passed; public List<SceneEvidence> scenes = new List<SceneEvidence>(); }
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs();
            int flag = Array.IndexOf(args, "--closure-output");
            if (flag < 0 || flag + 1 >= args.Length) throw new ArgumentException("Missing audit output.");
            var report = new Report {passed = true};
            foreach (var configured in EditorBuildSettings.scenes)
            {
                if (!configured.enabled) continue;
                var scene = EditorSceneManager.OpenScene(configured.path, OpenSceneMode.Single);
                var evidence = new SceneEvidence {scene = configured.path};
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    {
                        evidence.objects++;
                        int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                        evidence.missingScripts += missing;
                        if (missing > 0) evidence.failures.Add(t.name + ": missing scripts=" + missing);
                        var renderer = t.GetComponent<Renderer>();
                        if (renderer != null) foreach (var material in renderer.sharedMaterials)
                        {
                            if (material == null)
                            {
                                // TimeTrial's unchanged generator assigns its configured material in Start.
                                // Validate that exact asset rather than hiding arbitrary null materials.
                                var generator=t.GetComponent<AnimeHillRacing.TimeTrial.TimeTrialCircuitGenerator>();
                                var definition=generator!=null?new SerializedObject(generator).FindProperty("definition").objectReferenceValue as AnimeHillRacing.TimeTrial.CircuitDefinition:null;
                                var deferred=definition!=null?definition.roadMaterial:null;
                                if(deferred!=null&&deferred.shader!=null&&deferred.shader.isSupported&&deferred.shader.name!="Hidden/InternalErrorShader")
                                    evidence.deferredRuntimeMaterials++;
                                else { evidence.missingMaterials++; evidence.failures.Add(t.name + ": missing material"); }
                            }
                            else if (material.shader == null || material.shader.name == "Hidden/InternalErrorShader" || !material.shader.isSupported)
                            { evidence.errorShaders++; evidence.failures.Add(t.name + ": unsupported/error shader"); }
                        }
                        var meshCollider = t.GetComponent<MeshCollider>();
                        if (meshCollider != null && meshCollider.sharedMesh == null) evidence.emptyMeshColliders++;
                    }
                report.scenes.Add(evidence);
                report.passed &= evidence.missingScripts == 0 && evidence.missingMaterials == 0 && evidence.errorShaders == 0;
            }
            Directory.CreateDirectory(args[flag + 1]);
            File.WriteAllText(Path.Combine(args[flag + 1], "scene-reference-audit.json"), JsonUtility.ToJson(report, true));
            // Do not save any authored scene during inspection.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (!report.passed) throw new InvalidDataException("Scene reference audit failed; see scene-reference-audit.json.");
            Debug.Log("RELEASE_SCENE_REFERENCE_AUDIT scenes=" + report.scenes.Count + " passed=true");
        }
    }
}
#endif
