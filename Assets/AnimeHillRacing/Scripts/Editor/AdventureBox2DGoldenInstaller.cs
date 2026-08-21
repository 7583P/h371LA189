using AnimeHillRacing.Vehicle;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.EditorTools
{
    public static class AdventureBox2DGoldenInstaller
    {
        private const string AutomaticInstallSessionKey = "AHR.AdventureBox2DGoldenInstaller.v3";
        private static readonly string[] VehicleDefinitionPaths =
        {
            "Assets/AnimeHillRacing/VehicleData/MainVehicle.asset",
            "Assets/AnimeHillRacing/VehicleData/HillAdventurerVehicle.asset"
        };

        [InitializeOnLoadMethod]
        private static void QueueAutomaticInstall()
        {
            if (SessionState.GetBool(AutomaticInstallSessionKey, false)) return;
            SessionState.SetBool(AutomaticInstallSessionKey, true);
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetBool(AutomaticInstallSessionKey, false);
                    return;
                }
                InstallGoldenBox2D();
            };
        }

        [MenuItem("Anime Hill Racing/Experiments/Install Box2D in Golden Adventure")]
        public static void InstallGoldenBox2D()
        {
            int converted = 0;
            for (int i = 0; i < VehicleDefinitionPaths.Length; i++)
            {
                VehicleDefinition definition = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(VehicleDefinitionPaths[i]);
                if (definition == null || definition.gameplayPrefab2D == null) continue;
                string prefabPath = AssetDatabase.GetAssetPath(definition.gameplayPrefab2D);
                if (ConvertPrefab(prefabPath, definition)) converted++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            int valid = 0;
            for (int i = 0; i < VehicleDefinitionPaths.Length; i++)
            {
                VehicleDefinition definition = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(VehicleDefinitionPaths[i]);
                GameObject prefab = definition != null ? definition.gameplayPrefab2D : null;
                bool prefabValid = prefab != null && prefab.GetComponent<Rigidbody2D>() != null &&
                                   prefab.GetComponent<VehiclePhysics2DController>() != null &&
                                   prefab.GetComponentsInChildren<Suspension2DController>(true).Length == 2;
                if (prefabValid) valid++;
                Debug.Log($"ADVENTURE_BOX2D_GOLDEN_PREFAB|vehicle={definition?.vehicleId}|valid={prefabValid}");
            }
            Debug.Log($"ADVENTURE_BOX2D_GOLDEN_INSTALLED|converted={converted}|valid={valid}" +
                      $"|fixedDt={Time.fixedDeltaTime:F3}|gravity={Physics2D.gravity}" +
                      $"|velocityIterations={Physics2D.velocityIterations}|positionIterations={Physics2D.positionIterations}" +
                      $"|contactOffset={Physics2D.defaultContactOffset:F3}|maxTranslationSpeed={Physics2D.maxTranslationSpeed:F0}");
        }

        private static bool ConvertPrefab(string prefabPath, VehicleDefinition definition)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                VehiclePhysics2DController existing = root.GetComponent<VehiclePhysics2DController>();
                if (existing != null)
                {
                    ConfigureSerializedReferences(existing, definition,
                        FindSuspension2D(root, "front"), FindSuspension2D(root, "rear"));
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    return false;
                }

                VehicleVisual2D visual = root.GetComponentInChildren<VehicleVisual2D>(true);
                if (visual == null)
                    throw new System.InvalidOperationException($"{prefabPath} has no VehicleVisual2D.");

                CapsuleCollider oldCapsule = root.GetComponent<CapsuleCollider>();
                Vector2 capsuleOffset = oldCapsule != null ? (Vector2)oldCapsule.center : Vector2.zero;
                Vector2 capsuleSize = oldCapsule != null
                    ? new Vector2(oldCapsule.height, oldCapsule.radius * 2f)
                    : new Vector2(Mathf.Max(1f, definition.visualWidth * .75f), .8f);

                SuspensionController[] oldSuspensions = root.GetComponentsInChildren<SuspensionController>(true);
                for (int i = 0; i < oldSuspensions.Length; i++)
                    if (oldSuspensions[i] != null) Object.DestroyImmediate(oldSuspensions[i].gameObject);

                DestroyComponents<ConfigurableJoint>(root);
                DestroyComponent<MovementPlaneConstraint>(root);
                DestroyComponent<VehicleController>(root);
                DestroyComponent<CapsuleCollider>(root);
                DestroyComponent<Rigidbody>(root);

                Rigidbody2D chassis = root.AddComponent<Rigidbody2D>();
                chassis.bodyType = RigidbodyType2D.Dynamic;
                chassis.mass = definition.mass;
                chassis.linearDamping = .05f;
                chassis.angularDamping = definition.twoWheelAngularDamping;
                chassis.interpolation = RigidbodyInterpolation2D.Interpolate;
                chassis.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                CapsuleCollider2D chassisCollider = root.AddComponent<CapsuleCollider2D>();
                chassisCollider.direction = CapsuleDirection2D.Horizontal;
                chassisCollider.offset = capsuleOffset;
                chassisCollider.size = capsuleSize;

                VehiclePhysics2DController controller = root.AddComponent<VehiclePhysics2DController>();
                Suspension2DController rear = CreateWheel(root.transform, "Rear Suspension 2D",
                    definition.RearWheelAnchor, definition.wheelRadius, visual.RearWheelSprite, chassis);
                Suspension2DController front = CreateWheel(root.transform, "Front Suspension 2D",
                    definition.FrontWheelAnchor, definition.wheelRadius, visual.FrontWheelSprite, chassis);
                ConfigureSerializedReferences(controller, definition, front, rear);

                visual.Assign(controller, visual.BodyRenderer != null ? visual.BodyRenderer.transform : visual.transform,
                    visual.FrontWheelSprite, visual.RearWheelSprite, visual.DriverAnchor, definition.wheelRadius);
                ConvertDriverHeadCollider(root);

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log($"ADVENTURE_BOX2D_PREFAB_CONVERTED|vehicle={definition.vehicleId}|prefab={prefabPath}");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Suspension2DController CreateWheel(Transform root, string name, float anchorX,
            float radius, Transform wheelVisual, Rigidbody2D chassis)
        {
            GameObject wheelObject = new GameObject(name);
            wheelObject.transform.SetParent(root, false);
            wheelObject.transform.localPosition = new Vector3(anchorX, -.45f, 0f);

            Rigidbody2D wheelBody = wheelObject.AddComponent<Rigidbody2D>();
            wheelBody.bodyType = RigidbodyType2D.Dynamic;
            wheelBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            wheelBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D wheelCollider = wheelObject.AddComponent<CircleCollider2D>();
            wheelCollider.radius = Mathf.Max(.1f, radius);

            WheelJoint2D joint = wheelObject.AddComponent<WheelJoint2D>();
            joint.connectedBody = chassis;
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = Vector2.zero;
            joint.connectedAnchor = new Vector2(anchorX, -.45f);
            joint.enableCollision = false;

            Suspension2DController suspension = wheelObject.AddComponent<Suspension2DController>();
            suspension.AssignWheelVisual(wheelVisual);
            return suspension;
        }

        private static void ConfigureSerializedReferences(VehiclePhysics2DController controller,
            VehicleDefinition definition, Suspension2DController front, Suspension2DController rear)
        {
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("definition").objectReferenceValue = definition;
            serialized.FindProperty("frontSuspension").objectReferenceValue = front;
            serialized.FindProperty("rearSuspension").objectReferenceValue = rear;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static Suspension2DController FindSuspension2D(GameObject root, string token)
        {
            Suspension2DController[] suspensions = root.GetComponentsInChildren<Suspension2DController>(true);
            for (int i = 0; i < suspensions.Length; i++)
                if (suspensions[i].name.ToLowerInvariant().Contains(token)) return suspensions[i];
            return null;
        }

        private static void ConvertDriverHeadCollider(GameObject root)
        {
            DriverHeadImpact[] impacts = root.GetComponentsInChildren<DriverHeadImpact>(true);
            for (int i = 0; i < impacts.Length; i++)
            {
                SphereCollider sphere = impacts[i].GetComponent<SphereCollider>();
                if (sphere == null) continue;
                float radius = sphere.radius;
                bool trigger = sphere.isTrigger;
                Object.DestroyImmediate(sphere);
                CircleCollider2D circle = impacts[i].gameObject.AddComponent<CircleCollider2D>();
                circle.radius = radius;
                circle.isTrigger = trigger;
            }
        }

        private static void DestroyComponent<T>(GameObject root) where T : Component
        {
            T component = root.GetComponent<T>();
            if (component != null) Object.DestroyImmediate(component);
        }

        private static void DestroyComponents<T>(GameObject root) where T : Component
        {
            T[] components = root.GetComponents<T>();
            for (int i = components.Length - 1; i >= 0; i--)
                if (components[i] != null) Object.DestroyImmediate(components[i]);
        }
    }
}
