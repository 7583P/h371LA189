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
                Debug.Log("AERIAL_SATELITE_VERIFICATION_OK|fuelNormal=60s|fuelSingleThruster=24s|fuelDualThrusters=20s|groundDualPedal=pass|oneWheelLock=pass|independentAirPedals=pass|thrust=50-100|noFuelForce=off|maxUpgrade=120000");
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
                vehicle.UpgradeState.turboTune = 1;
                aerial.Initialize(vehicle, null, fuel);

                float forceStart = ReadTotalForce(body).y;
                Step(aerial, true, false, 12);
                float gasForce = ReadTotalForce(body).y - forceStart;
                if (aerial.State != AerialFlightState.Flying || gasForce <= 0f ||
                    aerial.CurrentRightPower <= .5f || aerial.CurrentLeftPower > .001f)
                    throw new InvalidOperationException("Aerial gas/automatic FlightMode validation failed.");

                aerial.Initialize(vehicle, null, fuel);
                forceStart = ReadTotalForce(body).y;
                Step(aerial, false, true, 12);
                float brakeForce = ReadTotalForce(body).y - forceStart;
                if (aerial.State != AerialFlightState.Flying || brakeForce <= 0f ||
                    aerial.CurrentLeftPower <= .5f || aerial.CurrentRightPower > .001f ||
                    Mathf.Abs(gasForce - brakeForce) > Mathf.Max(1f, gasForce * .02f))
                    throw new InvalidOperationException("Aerial brake flight thrust does not match gas thrust.");

                aerial.Initialize(vehicle, null, fuel);
                forceStart = ReadTotalForce(body).y;
                Step(aerial, true, true, 12);
                float boostForce = ReadTotalForce(body).y - forceStart;
                if (aerial.State != AerialFlightState.Flying ||
                    boostForce <= gasForce * 1.1f || boostForce <= body.mass * Physics2D.gravity.magnitude)
                    throw new InvalidOperationException("Aerial dual-pedal boost is not stronger than single-pedal thrust.");

                vehicle.UpgradeState.turboTune = definition.turboMaxTunes;
                aerial.Initialize(vehicle, null, fuel);
                forceStart = ReadTotalForce(body).y;
                Step(aerial, true, true, 12);
                float maximumTuneForce = ReadTotalForce(body).y - forceStart;
                if (maximumTuneForce < boostForce * 1.85f)
                    throw new InvalidOperationException(
                        $"Aerial maximum thruster tune is not close to twice base authority: {maximumTuneForce / Mathf.Max(1f, boostForce):0.00}x.");

                fuel.SetNormalized(0f);
                Vector2 forceBefore = ReadTotalForce(body);
                aerial.StepPhysics(.02f, true, true, true);
                Vector2 forceAfter = ReadTotalForce(body);
                if ((forceAfter - forceBefore).sqrMagnitude > .001f)
                    throw new InvalidOperationException("Aerial continues producing lift without fuel.");

                if (!AerialThrusterController.IsThrustPermitted(true, true, true,
                        true, true, true, AerialFlightState.Grounded))
                    throw new InvalidOperationException(
                        "Aerial blocks dual-pedal thrust while both wheels touch ground.");
                if (AerialThrusterController.IsThrustPermitted(true, true, false,
                        true, true, true, AerialFlightState.Landing))
                    throw new InvalidOperationException(
                        "Aerial permits ground thrust while only one wheel touches ground.");
                if (AerialThrusterController.IsThrustPermitted(true, true, true,
                        true, false, true, AerialFlightState.Grounded) ||
                    AerialThrusterController.IsThrustPermitted(true, true, true,
                        false, true, true, AerialFlightState.Grounded))
                    throw new InvalidOperationException(
                        "Aerial permits ground thrust without both pedals pressed.");
                if (!AerialThrusterController.IsThrustPermitted(true, false, false,
                        true, false, true, AerialFlightState.Flying))
                    throw new InvalidOperationException("Aerial blocks valid fully-airborne thrust.");
                if (!Mathf.Approximately(AerialThrusterController.ResolveConsumptionMultiplier(1f, 0f), 2.5f) ||
                    !Mathf.Approximately(AerialThrusterController.ResolveConsumptionMultiplier(0f, 1f), 2.5f) ||
                    !Mathf.Approximately(AerialThrusterController.ResolveConsumptionMultiplier(1f, 1f), 3f))
                    throw new InvalidOperationException("Aerial thruster fuel multipliers are incorrect.");

                fuel.RefillFull();
                fuel.SetConsumptionMultiplier(1f);
                fuel.TickRun(60f, true);
                if (fuel.FuelNormalized > .001f)
                    throw new InvalidOperationException("Aerial normal fuel duration is not 60 seconds.");
                fuel.RefillFull();
                fuel.SetConsumptionMultiplier(2.5f);
                fuel.TickRun(24f, true);
                if (fuel.FuelNormalized > .001f)
                    throw new InvalidOperationException("Aerial single-thruster fuel duration is not 24 seconds.");
                fuel.RefillFull();
                fuel.SetConsumptionMultiplier(3f);
                fuel.TickRun(20f, true);
                if (fuel.FuelNormalized > .001f)
                    throw new InvalidOperationException("Aerial dual-thruster fuel duration is not 20 seconds.");
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
