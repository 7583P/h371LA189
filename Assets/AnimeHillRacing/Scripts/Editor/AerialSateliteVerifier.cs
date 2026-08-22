using System;
using AnimeHillRacing.Core;
using AnimeHillRacing.Vehicle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.Editor
{
    public static class AerialSateliteVerifier
    {
        [MenuItem("Anime Hill Racing/Validation/Verify Aerial Satelite")]
        public static void RunFromMenu() => Run(true);

        public static void Run(bool logResult)
        {
            const string root = "Assets/AnimeHillRacing";
            VehicleDefinition definition = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(
                root + "/VehicleData/AerialSateliteVehicle.asset");
            GameCatalog catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(root + "/VehicleData/GameCatalog.asset");
            if (definition == null || catalog?.upgradeEconomy == null)
                throw new InvalidOperationException("Aerial Satelite validation assets are missing.");

            ValidateEconomy(definition, catalog.upgradeEconomy);
            ValidateFuelAndFlight(definition);
            if (logResult)
                Debug.Log("AERIAL_SATELITE_VERIFICATION_OK|fuelNormal=60s|fuelThrusters=30s|takeoff=pass|autoFlight=pass|noFuelForce=off|maxUpgrade=120000");
        }

        private static void ValidateEconomy(VehicleDefinition definition, UpgradeEconomyDefinition economy)
        {
            int maximumObserved = 0;
            foreach (VehicleUpgradeType type in Enum.GetValues(typeof(VehicleUpgradeType)))
            {
                int maximum = definition.GetMaxTunes(type);
                for (int level = 1; level < maximum; level++)
                {
                    int cost = economy.GetCostForNextTune(definition, type, level);
                    if (cost > 120000)
                        throw new InvalidOperationException($"Aerial upgrade {type} exceeds 120000: {cost}.");
                    maximumObserved = Mathf.Max(maximumObserved, cost);
                }
            }
            if (maximumObserved != 120000)
                throw new InvalidOperationException($"Aerial maximum upgrade price is {maximumObserved}, expected 120000.");
        }

        private static void ValidateFuelAndFlight(VehicleDefinition definition)
        {
            Scene testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            GameObject rig = null;
            float previousTimeScale = Time.timeScale;
            try
            {
                Time.timeScale = 1f;
                rig = new GameObject("Aerial Satelite Physics Verification");
                SceneManager.MoveGameObjectToScene(rig, testScene);
                Rigidbody2D body = rig.AddComponent<Rigidbody2D>();
                rig.AddComponent<CapsuleCollider2D>();
                FuelSystem fuel = rig.AddComponent<FuelSystem>();
                rig.AddComponent<EngineController>();
                rig.AddComponent<TransmissionController>();
                VehiclePhysics2DController vehicle = rig.AddComponent<VehiclePhysics2DController>();
                AerialThrusterController aerial = rig.AddComponent<AerialThrusterController>();
                vehicle.AssignRuntimeReferences(definition, null, null, null);
                aerial.Initialize(vehicle, null, fuel);

                float forceStart = ReadTotalForce(body).y;
                Step(aerial, true, false, 12);
                float gasForce = ReadTotalForce(body).y - forceStart;
                if (aerial.State != AerialFlightState.Flying || gasForce <= 0f)
                    throw new InvalidOperationException("Aerial gas/automatic FlightMode validation failed.");

                aerial.Initialize(vehicle, null, fuel);
                forceStart = ReadTotalForce(body).y;
                Step(aerial, false, true, 12);
                float brakeForce = ReadTotalForce(body).y - forceStart;
                if (aerial.State != AerialFlightState.Flying || brakeForce <= 0f ||
                    Mathf.Abs(gasForce - brakeForce) > Mathf.Max(1f, gasForce * .02f))
                    throw new InvalidOperationException("Aerial brake flight thrust does not match gas thrust.");

                aerial.Initialize(vehicle, null, fuel);
                forceStart = ReadTotalForce(body).y;
                aerial.StepPhysics(.02f, true, true, true);
                if (aerial.State != AerialFlightState.Takeoff)
                    throw new InvalidOperationException("Aerial dual-pedal Takeoff state did not engage.");
                Step(aerial, true, true, 11);
                float boostForce = ReadTotalForce(body).y - forceStart;
                if (aerial.State != AerialFlightState.Flying ||
                    boostForce <= gasForce * 1.1f || boostForce <= body.mass * Physics2D.gravity.magnitude)
                    throw new InvalidOperationException("Aerial dual-pedal boost is not stronger than single-pedal thrust.");

                fuel.SetNormalized(0f);
                Vector2 forceBefore = ReadTotalForce(body);
                aerial.StepPhysics(.02f, true, true, true);
                Vector2 forceAfter = ReadTotalForce(body);
                if ((forceAfter - forceBefore).sqrMagnitude > .001f)
                    throw new InvalidOperationException("Aerial continues producing lift without fuel.");

                fuel.RefillFull();
                fuel.SetConsumptionMultiplier(1f);
                fuel.TickRun(60f, true);
                if (fuel.FuelNormalized > .001f)
                    throw new InvalidOperationException("Aerial normal fuel duration is not 60 seconds.");
                fuel.RefillFull();
                fuel.SetConsumptionMultiplier(2f);
                fuel.TickRun(30f, true);
                if (fuel.FuelNormalized > .001f)
                    throw new InvalidOperationException("Aerial thruster fuel duration is not 30 seconds.");
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                if (rig != null) UnityEngine.Object.DestroyImmediate(rig);
                EditorSceneManager.CloseScene(testScene, true);
            }
        }

        private static Vector2 ReadTotalForce(Rigidbody2D body)
        {
            var property = typeof(Rigidbody2D).GetProperty("totalForce");
            if (property == null)
                throw new InvalidOperationException("Unity Rigidbody2D.totalForce is unavailable for validation.");
            return (Vector2)property.GetValue(body);
        }

        private static void Step(AerialThrusterController aerial, bool gas, bool brake, int count)
        {
            for (int i = 0; i < count; i++)
                aerial.StepPhysics(.02f, true, gas, brake);
        }
    }
}
