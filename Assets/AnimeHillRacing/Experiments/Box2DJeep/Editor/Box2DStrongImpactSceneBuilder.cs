using System;
using AnimeHillRacing.Experiments.Box2DJeep;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.EditorTools
{
    [InitializeOnLoad]
    public static class Box2DStrongImpactSceneBuilder
    {
        private const string RootFolder = "Assets/AnimeHillRacing/Experiments/Box2DJeep";
        private const string ScenePath = RootFolder + "/Box2DStrongImpactPlayMode.unity";
        private const string MaterialPath = RootFolder + "/Box2DJeepLine.mat";
        private const string VerifyPhaseKey = "AnimeHillRacing.Box2DStrongImpact.VerifyPhase";

        static Box2DStrongImpactSceneBuilder()
        {
            if (SessionState.GetInt(VerifyPhaseKey, 0) != 0)
            {
                EditorApplication.update -= ContinuePlayModeVerification;
                EditorApplication.update += ContinuePlayModeVerification;
            }
        }

        [MenuItem("Tools/Anime Hill Racing/Build Strong Impact Play Mode Scene")]
        public static void Build()
        {
            Material lineMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (lineMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                {
                    throw new InvalidOperationException("Sprites/Default shader was not found.");
                }
                lineMaterial = new Material(shader) { name = "Box2D Jeep Line" };
                AssetDatabase.CreateAsset(lineMaterial, MaterialPath);
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Box2DJeepRig rig = Box2DJeepFactory.Create(0f, lineMaterial, true);
            rig.gameObject.AddComponent<Box2DStrongImpactPlayMode>();

            GameObject cameraObject = new GameObject("Strong Impact Follow Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.018f, 0.035f, 0.075f, 1f);
            cameraObject.tag = "MainCamera";
            SimpleBox2DCamera follow = cameraObject.AddComponent<SimpleBox2DCamera>();
            follow.Configure(rig.Chassis.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("Could not save the strong-impact Play Mode scene.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("BOX2D_STRONG_IMPACT_SCENE_READY|scene=" + ScenePath);
        }

        public static void VerifyPlayMode()
        {
            Build();
            SessionState.SetInt(VerifyPhaseKey, 1);
            EditorApplication.update -= ContinuePlayModeVerification;
            EditorApplication.update += ContinuePlayModeVerification;
            AdventurePlayModeStartScene.UseSceneForNextPlayMode(ScenePath);
            EditorApplication.isPlaying = true;
        }

        private static void ContinuePlayModeVerification()
        {
            int phase = SessionState.GetInt(VerifyPhaseKey, 0);
            if (phase == 1 && EditorApplication.isPlaying)
            {
                Box2DStrongImpactPlayMode scenario = UnityEngine.Object.FindFirstObjectByType<Box2DStrongImpactPlayMode>();
                if (scenario == null || !scenario.Completed)
                {
                    return;
                }

                Debug.Log(
                    $"BOX2D_STRONG_IMPACT_PLAY_VERIFIED|rearBelow={scenario.RearBelowSeconds:F3}|" +
                    $"frontBelow={scenario.FrontBelowSeconds:F3}|rearUnder={scenario.RearWheelUnder}|" +
                    $"frontUnder={scenario.FrontWheelUnder}");
                SessionState.SetInt(VerifyPhaseKey, 2);
                EditorApplication.isPlaying = false;
                return;
            }

            if (phase == 2 && !EditorApplication.isPlaying)
            {
                SessionState.EraseInt(VerifyPhaseKey);
                EditorApplication.update -= ContinuePlayModeVerification;
                Debug.Log("BOX2D_STRONG_IMPACT_PLAYMODE_VERIFY_END");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
        }
    }
}
