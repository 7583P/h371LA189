using AnimeHillRacing.Core;
using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    public sealed class ArticulatedDriver2D : MonoBehaviour
    {
        // Scale only rendered pieces: seat anchors, articulated pose and head-impact collider stay unchanged.
        public const float CharacterVisualScaleMultiplier = 1.5f;

        [SerializeField] private SpriteRenderer torso;
        [SerializeField] private SpriteRenderer head;
        [SerializeField] private SpriteRenderer leftUpperArm;
        [SerializeField] private SpriteRenderer leftForearm;
        [SerializeField] private SpriteRenderer rightUpperArm;
        [SerializeField] private SpriteRenderer rightForearm;
        [SerializeField] private SpriteRenderer leftThigh;
        [SerializeField] private SpriteRenderer leftLowerLeg;
        [SerializeField] private SpriteRenderer rightThigh;
        [SerializeField] private SpriteRenderer rightLowerLeg;
        [SerializeField] private SpriteRenderer accessory;
        [SerializeField] private DriverHeadImpact headImpact;
        [SerializeField] private bool physicalReaction = true;

        private Rigidbody vehicleBody;
        private Transform torsoTransform;
        private Transform headTransform;
        private float wobbleAngle;
        private float wobbleVelocity;
        private float impactImpulse;
        private float previousAngularVelocity;
        private Quaternion torsoBaseRotation;
        private Quaternion headBaseRotation;
        private readonly Transform[] reactiveLimbs = new Transform[4];
        private readonly Quaternion[] reactiveLimbBaseRotations = new Quaternion[4];
        private Vector3 previousVelocity;

        public SpriteRenderer HeadRenderer => head;
        public SpriteRenderer AccessoryRenderer => accessory;
        public DriverHeadImpact HeadImpact => headImpact;

        private void Awake()
        {
            torsoTransform = torso != null ? torso.transform : null;
            headTransform = head != null ? head.transform : null;
        }

        private void LateUpdate()
        {
            if (!physicalReaction || vehicleBody == null || headTransform == null) return;
            float angular = vehicleBody.angularVelocity.z * Mathf.Rad2Deg;
            float angularAcceleration = (angular - previousAngularVelocity) / Mathf.Max(.001f, Time.deltaTime);
            previousAngularVelocity = angular;
            Vector3 acceleration = (vehicleBody.linearVelocity - previousVelocity) / Mathf.Max(.001f, Time.deltaTime);
            previousVelocity = vehicleBody.linearVelocity;
            float longitudinalReaction = -Vector3.Dot(acceleration, vehicleBody.transform.right) * .12f;
            float landingReaction = -acceleration.y * .022f;
            float target = Mathf.Clamp(-angular * .055f - angularAcceleration * .0015f +
                longitudinalReaction + landingReaction + impactImpulse, -16f, 16f);
            wobbleAngle = Mathf.SmoothDamp(wobbleAngle, target, ref wobbleVelocity, .075f, 240f, Time.deltaTime);
            impactImpulse = Mathf.MoveTowards(impactImpulse, 0f, 75f * Time.deltaTime);
            headTransform.localRotation = headBaseRotation * Quaternion.Euler(0f, 0f, wobbleAngle);
            if (torsoTransform != null)
                torsoTransform.localRotation = torsoBaseRotation * Quaternion.Euler(0f, 0f, wobbleAngle * .22f);
            for (int i = 0; i < reactiveLimbs.Length; i++)
                if (reactiveLimbs[i] != null)
                    reactiveLimbs[i].localRotation = reactiveLimbBaseRotations[i] *
                        Quaternion.Euler(0f, 0f, wobbleAngle * (i < 2 ? .16f : .08f));
        }

        public void TriggerHeadImpact(float impactSpeed)
        {
            if (!physicalReaction) return;
            float direction = vehicleBody != null && vehicleBody.angularVelocity.z < 0f ? -1f : 1f;
            impactImpulse = direction * Mathf.Clamp(impactSpeed * 2.2f, 5f, 16f);
            wobbleVelocity += direction * Mathf.Clamp(impactSpeed * 18f, 35f, 150f);
        }

        public void Apply(DriverDefinition driver, VehicleDefinition.DriverPose pose, Rigidbody body,
            bool enablePhysicalReaction)
        {
            DriverDefinition.CharacterAccessoryOption selected = driver != null
                ? driver.FindAccessory(SaveManager.GetSelectedCharacterAccessoryId(driver.DefaultAccessoryId))
                : null;
            Apply(driver, pose, selected, body, enablePhysicalReaction);
        }

        public void Apply(DriverDefinition driver, VehicleDefinition.DriverPose pose,
            DriverDefinition.CharacterAccessoryOption selectedAccessory, Rigidbody body,
            bool enablePhysicalReaction)
        {
            if (driver == null || pose == null) return;
            vehicleBody = body;
            physicalReaction = enablePhysicalReaction;
            transform.localPosition = new Vector3(pose.rootPosition.x, pose.rootPosition.y, transform.localPosition.z);
            transform.localScale = Vector3.one * Mathf.Max(.01f, pose.rootScale);
            ApplyPart(torso, driver.torso, pose.torso);
            ApplyPart(head, driver.head, pose.head);
            ApplyPart(leftUpperArm, driver.upperArm, pose.leftUpperArm);
            ApplyPart(leftForearm, driver.forearm, pose.leftForearm);
            ApplyPart(rightUpperArm, driver.upperArm, pose.rightUpperArm);
            ApplyPart(rightForearm, driver.forearm, pose.rightForearm);
            ApplyPart(leftThigh, driver.thigh, pose.leftThigh);
            ApplyPart(leftLowerLeg, driver.lowerLeg, pose.leftLowerLeg);
            ApplyPart(rightThigh, driver.thigh, pose.rightThigh);
            ApplyPart(rightLowerLeg, driver.lowerLeg, pose.rightLowerLeg);
            EnsureAccessoryRenderer();
            ApplyAccessory(selectedAccessory, pose.accessory);
            torsoTransform = torso != null ? torso.transform : null;
            headTransform = head != null ? head.transform : null;
            torsoBaseRotation = torsoTransform != null ? torsoTransform.localRotation : Quaternion.identity;
            headBaseRotation = headTransform != null ? headTransform.localRotation : Quaternion.identity;
            reactiveLimbs[0] = leftForearm != null ? leftForearm.transform : null;
            reactiveLimbs[1] = rightForearm != null ? rightForearm.transform : null;
            reactiveLimbs[2] = leftLowerLeg != null ? leftLowerLeg.transform : null;
            reactiveLimbs[3] = rightLowerLeg != null ? rightLowerLeg.transform : null;
            for (int i = 0; i < reactiveLimbs.Length; i++)
                reactiveLimbBaseRotations[i] = reactiveLimbs[i] != null
                    ? reactiveLimbs[i].localRotation : Quaternion.identity;
            previousVelocity = body != null ? body.linearVelocity : Vector3.zero;
            if (headImpact != null)
            {
                headImpact.Assign(this, body);
                headImpact.gameObject.SetActive(enablePhysicalReaction && pose.head.visible);
            }
        }

        public void AssignParts(SpriteRenderer torsoPart, SpriteRenderer headPart,
            SpriteRenderer leftUpperArmPart, SpriteRenderer leftForearmPart,
            SpriteRenderer rightUpperArmPart, SpriteRenderer rightForearmPart,
            SpriteRenderer leftThighPart, SpriteRenderer leftLowerLegPart,
            SpriteRenderer rightThighPart, SpriteRenderer rightLowerLegPart, DriverHeadImpact impact)
        {
            torso = torsoPart; head = headPart;
            leftUpperArm = leftUpperArmPart; leftForearm = leftForearmPart;
            rightUpperArm = rightUpperArmPart; rightForearm = rightForearmPart;
            leftThigh = leftThighPart; leftLowerLeg = leftLowerLegPart;
            rightThigh = rightThighPart; rightLowerLeg = rightLowerLegPart;
            headImpact = impact;
            Awake();
        }

        public void AssignAccessory(SpriteRenderer accessoryPart) => accessory = accessoryPart;

        private void EnsureAccessoryRenderer()
        {
            if (accessory != null) return;
            Transform existing = transform.Find("Accessory Anchor");
            if (existing == null)
            {
                var anchor = new GameObject("Accessory Anchor");
                anchor.transform.SetParent(transform, false);
                existing = anchor.transform;
            }
            accessory = existing.GetComponent<SpriteRenderer>();
            if (accessory == null) accessory = existing.gameObject.AddComponent<SpriteRenderer>();
        }

        private void ApplyAccessory(DriverDefinition.CharacterAccessoryOption option,
            VehicleDefinition.DriverPartPose pose)
        {
            if (accessory == null) return;
            Sprite sprite = option != null ? option.sprite : null;
            accessory.sprite = sprite;
            accessory.enabled = pose.visible && sprite != null;

            bool isScarf = option != null && (option.accessoryId == "scarf" ||
                option.displayName.IndexOf("scarf", System.StringComparison.OrdinalIgnoreCase) >= 0);

            Transform part = accessory.transform;
            Vector2 sharedOffset = option != null ? option.anchorOffset : Vector2.zero;

            if (isScarf && torso != null && head != null)
            {
                // Position at the neck base, sitting cleanly right above the upper torso
                Vector3 neckPos = Vector3.Lerp(torso.transform.localPosition, head.transform.localPosition, 0.36f);
                part.localPosition = new Vector3(neckPos.x + sharedOffset.x, neckPos.y + sharedOffset.y, part.localPosition.z);
                part.localRotation = Quaternion.Euler(0f, 0f, pose.rotation);
                accessory.sortingOrder = Mathf.Max(torso.sortingOrder, head.sortingOrder) + 1;
            }
            else
            {
                accessory.sortingOrder = pose.sortingOrder;
                part.localPosition = new Vector3(pose.position.x + sharedOffset.x,
                    pose.position.y + sharedOffset.y, part.localPosition.z);
                part.localRotation = Quaternion.Euler(0f, 0f, pose.rotation);
            }

            float sourceWidth = sprite != null ? Mathf.Max(.001f, sprite.bounds.size.x) : 1f;
            float fittedScale = option != null ? option.visibleWidth / sourceWidth : 1f;
            float scaleX = Mathf.Abs(pose.scale.x) > .0001f ? Mathf.Abs(pose.scale.x) : 1f;
            float scaleY = Mathf.Abs(pose.scale.y) > .0001f ? Mathf.Abs(pose.scale.y) : scaleX;
            part.localScale = new Vector3(fittedScale * scaleX * CharacterVisualScaleMultiplier,
                fittedScale * scaleY * CharacterVisualScaleMultiplier, 1f);

            var dynamics = accessory.GetComponent<CharacterScarfDynamics>();
            if (isScarf && accessory.enabled)
            {
                if (dynamics == null) dynamics = accessory.gameObject.AddComponent<CharacterScarfDynamics>();
                dynamics.enabled = true;
                dynamics.Initialize(accessory, vehicleBody, torso != null ? torso.transform : transform);
            }
            else if (dynamics != null)
            {
                dynamics.enabled = false;
            }
        }

        private static void ApplyPart(SpriteRenderer renderer, Sprite sprite,
            VehicleDefinition.DriverPartPose pose)
        {
            if (renderer == null) return;
            renderer.sprite = sprite;
            renderer.enabled = pose.visible && sprite != null;
            renderer.sortingOrder = pose.sortingOrder;
            Transform part = renderer.transform;
            part.localPosition = new Vector3(pose.position.x, pose.position.y, part.localPosition.z);
            part.localRotation = Quaternion.Euler(0f, 0f, pose.rotation);
            float scaleX = Mathf.Abs(pose.scale.x) > .0001f ? Mathf.Abs(pose.scale.x) : 1f;
            float scaleY = Mathf.Abs(pose.scale.y) > .0001f ? Mathf.Abs(pose.scale.y) : scaleX;
            part.localScale = new Vector3(scaleX * CharacterVisualScaleMultiplier,
                scaleY * CharacterVisualScaleMultiplier, 1f);
        }
    }

}
