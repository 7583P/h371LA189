using System.Collections;
using AnimeHillRacing.Core;
using AnimeHillRacing.Terrain;
using AnimeHillRacing.Vehicle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AnimeHillRacing.EditorTools
{
    public static class AdventurePhysics2DAudit
    {
        public struct AuditReport
        {
            public string vehicleName;
            public float restSagFrontPercent;
            public float restSagRearPercent;
            public float maxLaunchPitchDegrees;
            public float zeroToFiftyTimeSeconds;
            public float zeroToOneHundredTimeSeconds;
            public float maxSpeedKmh;
            public bool isStaticStable;
            public bool airControlFunctional;
            public bool airControlDisabledOnGround;
            public string summary;
        }

        public static AuditReport RunJeepAudit()
        {
            AuditReport report = new AuditReport { vehicleName = "JEEP" };

            // 1. Load Adventure2_5D scene
            EditorSceneManager.OpenScene("Assets/AnimeHillRacing/Scenes/Adventure2_5D.unity");

            // 2. Select Jeep in SaveManager / Catalog
            VehicleDefinition jeepDef = AssetDatabase.LoadAssetAtPath<VehicleDefinition>("Assets/AnimeHillRacing/VehicleData/MainVehicle.asset");
            if (jeepDef == null)
            {
                report.summary = "Failed: MainVehicle.asset not found";
                return report;
            }

            SaveManager.SelectedVehicleId = jeepDef.vehicleId;
            SaveManager.SelectedGameMode = GameModeType.Adventure2_5D;

            // 3. Inspect Prefab structure
            GameObject prefab = jeepDef.gameplayPrefab2D;
            if (prefab == null)
            {
                report.summary = "Failed: gameplayPrefab2D is null";
                return report;
            }

            Rigidbody2D body2d = prefab.GetComponent<Rigidbody2D>();
            VehiclePhysics2DController ctrl2d = prefab.GetComponent<VehiclePhysics2DController>();
            Suspension2DController[] susList = prefab.GetComponentsInChildren<Suspension2DController>(true);

            if (body2d == null || ctrl2d == null || susList.Length < 2)
            {
                report.summary = $"Failed: 2D physics components missing on {prefab.name} (Body2D: {body2d != null}, Ctrl2D: {ctrl2d != null}, Suspensions: {susList.Length})";
                return report;
            }

            report.summary = "Jeep_2D prefab validated with Rigidbody2D and 2 Suspension2DControllers";
            return report;
        }
    }
}
