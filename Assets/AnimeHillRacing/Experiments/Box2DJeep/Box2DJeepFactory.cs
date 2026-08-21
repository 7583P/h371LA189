using UnityEngine;

namespace AnimeHillRacing.Experiments.Box2DJeep
{
    public static class Box2DJeepFactory
    {
        public const float ChassisStartY = 2f;
        public const float WheelStartY = 1.2f;
        public const float HalfWheelbase = 0.8f;
        public const float WheelRadius = 0.38f;

        public static Box2DJeepRig Create(float worldX, Material lineMaterial = null, bool includeVisuals = true)
        {
            GameObject world = new GameObject("Box2DJeepPOC");
            world.transform.position = Vector3.zero;

            GameObject terrainObject = new GameObject("Absolute Edge Terrain 2D");
            terrainObject.transform.SetParent(world.transform, false);
            terrainObject.transform.position = Vector3.zero;
            EdgeCollider2D edge = terrainObject.AddComponent<EdgeCollider2D>();
            edge.edgeRadius = 0f;
            edge.sharedMaterial = null;

            LineRenderer terrainLine = null;
            if (includeVisuals)
            {
                terrainLine = CreateLine(terrainObject, lineMaterial, new Color(0.28f, 0.74f, 0.31f), 0.08f, false);
            }

            AbsoluteEdgeTerrain2D terrain = terrainObject.AddComponent<AbsoluteEdgeTerrain2D>();

            GameObject chassisObject = new GameObject("Chassis 2D");
            chassisObject.transform.SetParent(world.transform, false);
            chassisObject.transform.position = new Vector3(worldX, ChassisStartY, 0f);
            Rigidbody2D chassis = chassisObject.AddComponent<Rigidbody2D>();
            chassis.bodyType = RigidbodyType2D.Dynamic;
            chassis.mass = 5f;
            chassis.linearDamping = 0.15f;
            chassis.angularDamping = 8f;
            chassis.gravityScale = 2.6f;
            chassis.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            chassis.interpolation = RigidbodyInterpolation2D.Interpolate;
            BoxCollider2D chassisCollider = chassisObject.AddComponent<BoxCollider2D>();
            chassisCollider.size = new Vector2(2.2f, 0.7f);
            chassisCollider.offset = new Vector2(0f, -0.12f);
            chassisCollider.sharedMaterial = null;
            if (includeVisuals)
            {
                CreateRectangle(chassisObject, lineMaterial, new Color(0.88f, 0.33f, 0.12f), chassisCollider.size, chassisCollider.offset);
            }

            WheelParts rear = CreateWheel(world.transform, "Rear Wheel 2D", new Vector2(worldX - HalfWheelbase, WheelStartY), chassis, new Vector2(-0.78f, -0.35f), lineMaterial, includeVisuals);
            WheelParts front = CreateWheel(world.transform, "Front Wheel 2D", new Vector2(worldX + HalfWheelbase, WheelStartY), chassis, new Vector2(0.78f, -0.35f), lineMaterial, includeVisuals);

            Box2DJeepRig rig = world.AddComponent<Box2DJeepRig>();
            rig.Configure(chassis, rear.body, front.body, rear.joint, front.joint, terrain);
            terrain.Configure(chassis.transform, terrainLine);
            return rig;
        }

        private static WheelParts CreateWheel(
            Transform parent,
            string name,
            Vector2 worldPosition,
            Rigidbody2D chassis,
            Vector2 connectedAnchor,
            Material lineMaterial,
            bool includeVisuals)
        {
            GameObject wheelObject = new GameObject(name);
            wheelObject.transform.SetParent(parent, false);
            wheelObject.transform.position = worldPosition;

            Rigidbody2D body = wheelObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.mass = 2f;
            body.linearDamping = 0f;
            body.angularDamping = 0.4f;
            body.gravityScale = 1f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            CircleCollider2D circle = wheelObject.AddComponent<CircleCollider2D>();
            circle.radius = WheelRadius;
            circle.sharedMaterial = null;

            WheelJoint2D joint = wheelObject.AddComponent<WheelJoint2D>();
            joint.connectedBody = chassis;
            joint.anchor = Vector2.zero;
            joint.autoConfigureConnectedAnchor = true;
            joint.connectedAnchor = connectedAnchor;
            joint.suspension = new JointSuspension2D
            {
                angle = 90f,
                frequency = 8f,
                dampingRatio = 0.95f
            };
            joint.enableCollision = false;
            joint.breakForce = Mathf.Infinity;
            joint.breakTorque = Mathf.Infinity;
            joint.motor = new JointMotor2D { motorSpeed = 0f, maxMotorTorque = 10000f };
            joint.useMotor = false;

            if (includeVisuals)
            {
                CreateCircle(wheelObject, lineMaterial, new Color(0.1f, 0.12f, 0.14f), WheelRadius);
            }

            return new WheelParts(body, joint);
        }

        private static void CreateRectangle(GameObject target, Material material, Color color, Vector2 size, Vector2 offset)
        {
            LineRenderer line = CreateLine(target, material, color, 0.065f, true);
            float halfX = size.x * 0.5f;
            float halfY = size.y * 0.5f;
            line.positionCount = 4;
            line.SetPosition(0, new Vector3(offset.x - halfX, offset.y - halfY, 0f));
            line.SetPosition(1, new Vector3(offset.x - halfX, offset.y + halfY, 0f));
            line.SetPosition(2, new Vector3(offset.x + halfX, offset.y + halfY, 0f));
            line.SetPosition(3, new Vector3(offset.x + halfX, offset.y - halfY, 0f));
        }

        private static void CreateCircle(GameObject target, Material material, Color color, float radius)
        {
            const int segments = 32;
            LineRenderer line = CreateLine(target, material, color, 0.06f, true);
            line.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }

        private static LineRenderer CreateLine(GameObject target, Material material, Color color, float width, bool loop)
        {
            LineRenderer line = target.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = loop;
            line.startWidth = width;
            line.endWidth = width;
            line.startColor = color;
            line.endColor = color;
            line.numCapVertices = 2;
            line.numCornerVertices = 2;
            line.material = material;
            return line;
        }

        private readonly struct WheelParts
        {
            public readonly Rigidbody2D body;
            public readonly WheelJoint2D joint;

            public WheelParts(Rigidbody2D wheelBody, WheelJoint2D wheelJoint)
            {
                body = wheelBody;
                joint = wheelJoint;
            }
        }
    }
}
