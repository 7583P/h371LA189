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
        private Rigidbody2D vehicleBody2D;
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
            if (!physicalReaction || (vehicleBody == null && vehicleBody2D == null) || headTransform == null) return;
            float angular = vehicleBody != null ? vehicleBody.angularVelocity.z * Mathf.Rad2Deg : (vehicleBody2D != null ? vehicleBody2D.angularVelocity : 0f);
            float angularAcceleration = (angular - previousAngularVelocity) / Mathf.Max(.001f, Time.deltaTime);
            previousAngularVelocity = angular;
            Vector3 currentVelocity = vehicleBody != null ? vehicleBody.linearVelocity : (vehicleBody2D != null ? (Vector3)vehicleBody2D.linearVelocity : Vector3.zero);
            Vector3 acceleration = (currentVelocity - previousVelocity) / Mathf.Max(.001f, Time.deltaTime);
            previousVelocity = currentVelocity;
            Transform vehicleTransform = vehicleBody != null ? vehicleBody.transform : (vehicleBody2D != null ? vehicleBody2D.transform : transform.root);
            float longitudinalReaction = -Vector3.Dot(acceleration, vehicleTransform.right) * .12f;
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
            float angularW = vehicleBody != null ? vehicleBody.angularVelocity.z : (vehicleBody2D != null ? vehicleBody2D.angularVelocity : 0f);
            float direction = angularW < 0f ? -1f : 1f;
            impactImpulse = direction * Mathf.Clamp(impactSpeed * 2.2f, 5f, 16f);
            wobbleVelocity += direction * Mathf.Clamp(impactSpeed * 18f, 35f, 150f);
        }

        public void Apply(DriverDefinition driver, VehicleDefinition.DriverPose pose, Rigidbody body,
            bool enablePhysicalReaction)
        {
            Apply(driver, pose, body, null, enablePhysicalReaction);
        }

        public void Apply(DriverDefinition driver, VehicleDefinition.DriverPose pose, Rigidbody body,
            Rigidbody2D body2D, bool enablePhysicalReaction)
        {
            DriverDefinition.CharacterAccessoryOption selected = driver != null
                ? driver.FindAccessory(SaveManager.GetSelectedCharacterAccessoryId(driver.DefaultAccessoryId))
                : null;
            Apply(driver, pose, selected, body, body2D, enablePhysicalReaction);
        }

        public void Apply(DriverDefinition driver, VehicleDefinition.DriverPose pose,
            DriverDefinition.CharacterAccessoryOption selectedAccessory, Rigidbody body,
            bool enablePhysicalReaction)
        {
            Apply(driver, pose, selectedAccessory, body, null, enablePhysicalReaction);
        }

        public void Apply(DriverDefinition driver, VehicleDefinition.DriverPose pose,
            DriverDefinition.CharacterAccessoryOption selectedAccessory, Rigidbody body,
            Rigidbody2D body2D, bool enablePhysicalReaction)
        {
            if (driver == null || pose == null) return;
            vehicleBody = body;
            vehicleBody2D = body2D;
            physicalReaction = enablePhysicalReaction;
            DriverDefinition reference = driver.PoseReference;
            transform.localPosition = new Vector3(pose.rootPosition.x, pose.rootPosition.y, transform.localPosition.z);
            float rootScale = Mathf.Max(.01f, pose.rootScale);
            transform.localScale = Vector3.one * rootScale;

            // DriverPartPose.scale is an effective visual scale shared by every vehicle. The root scale
            // still contracts or expands the articulated pose around its seat anchor, but must not make
            // identical part scale values render at different sizes in different vehicles.
            float visualScaleCompensation = 1f / rootScale;
            ApplyPart(torso, driver.torso, reference.torso, pose.torso, visualScaleCompensation);
            ApplyPart(head, driver.head, reference.head, pose.head, visualScaleCompensation);
            ApplyPart(leftUpperArm, driver.upperArm, reference.upperArm, pose.leftUpperArm, visualScaleCompensation);
            ApplyPart(leftForearm, driver.forearm, reference.forearm, pose.leftForearm, visualScaleCompensation);
            ApplyPart(rightUpperArm, driver.upperArm, reference.upperArm, pose.rightUpperArm, visualScaleCompensation);
            ApplyPart(rightForearm, driver.forearm, reference.forearm, pose.rightForearm, visualScaleCompensation);
            ApplyPart(leftThigh, driver.thigh, reference.thigh, pose.leftThigh, visualScaleCompensation);
            ApplyPart(leftLowerLeg, driver.lowerLeg, reference.lowerLeg, pose.leftLowerLeg, visualScaleCompensation);
            ApplyPart(rightThigh, driver.thigh, reference.thigh, pose.rightThigh, visualScaleCompensation);
            ApplyPart(rightLowerLeg, driver.lowerLeg, reference.lowerLeg, pose.rightLowerLeg, visualScaleCompensation);
            headImpact?.ConfigureHeadCollider(head);
            EnsureAccessoryRenderer();
            VehicleDefinition.DriverPartPose accessoryPose = pose.accessory;
            bool hasAccessoryOverride = selectedAccessory != null &&
                pose.TryGetAccessoryPose(selectedAccessory.accessoryId, out accessoryPose);
            ApplyAccessory(selectedAccessory, accessoryPose, visualScaleCompensation, hasAccessoryOverride);
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
            previousVelocity = body != null ? body.linearVelocity : (body2D != null ? (Vector3)body2D.linearVelocity : Vector3.zero);
            if (headImpact != null)
            {
                headImpact.Assign(this, body, body2D);
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
            VehicleDefinition.DriverPartPose pose, float visualScaleCompensation, bool hasVehicleOverride)
        {
            if (accessory == null) return;
            Sprite sprite = option != null ? option.sprite : null;
            accessory.sprite = sprite;
            accessory.enabled = pose.visible && sprite != null;

            bool isScarf = option != null && (option.accessoryId == "scarf" ||
                option.displayName.IndexOf("scarf", System.StringComparison.OrdinalIgnoreCase) >= 0);

            Transform part = accessory.transform;
            Vector2 sharedOffset = option != null ? option.anchorOffset : Vector2.zero;
            if (isScarf && !hasVehicleOverride && torso != null && head != null)
            {
                // Preserve the legacy neck placement until this vehicle receives an authored scarf override.
                Vector3 neckPosition = Vector3.Lerp(torso.transform.localPosition,
                    head.transform.localPosition, .36f);
                part.localPosition = new Vector3(neckPosition.x + sharedOffset.x,
                    neckPosition.y + sharedOffset.y, part.localPosition.z);
                accessory.sortingOrder = Mathf.Max(torso.sortingOrder, head.sortingOrder) + 1;
            }
            else
            {
                part.localPosition = new Vector3(pose.position.x + sharedOffset.x,
                    pose.position.y + sharedOffset.y, part.localPosition.z);
                accessory.sortingOrder = pose.sortingOrder;
            }
            part.localRotation = Quaternion.Euler(0f, 0f, pose.rotation);

            float sourceWidth = sprite != null ? Mathf.Max(.001f, sprite.bounds.size.x) : 1f;
            float fittedScale = option != null ? option.visibleWidth / sourceWidth : 1f;
            float scaleX = Mathf.Abs(pose.scale.x) > .0001f ? Mathf.Abs(pose.scale.x) : 1f;
            float scaleY = Mathf.Abs(pose.scale.y) > .0001f ? Mathf.Abs(pose.scale.y) : scaleX;
            part.localScale = new Vector3(
                fittedScale * scaleX * CharacterVisualScaleMultiplier * visualScaleCompensation,
                fittedScale * scaleY * CharacterVisualScaleMultiplier * visualScaleCompensation, 1f);

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

        private static void ApplyPart(SpriteRenderer renderer, Sprite sprite, Sprite referenceSprite,
            VehicleDefinition.DriverPartPose pose, float visualScaleCompensation)
        {
            if (renderer == null) return;
            renderer.sprite = sprite;
            renderer.enabled = pose.visible && sprite != null;
            renderer.sortingOrder = pose.sortingOrder;
            Transform part = renderer.transform;
            part.localRotation = Quaternion.Euler(0f, 0f, pose.rotation);
            float scaleX = Mathf.Abs(pose.scale.x) > .0001f ? Mathf.Abs(pose.scale.x) : 1f;
            float scaleY = Mathf.Abs(pose.scale.y) > .0001f ? Mathf.Abs(pose.scale.y) : scaleX;
            Vector2 normalization = ReferenceScale(sprite, referenceSprite);
            part.localScale = new Vector3(
                scaleX * normalization.x * CharacterVisualScaleMultiplier * visualScaleCompensation,
                scaleY * normalization.y * CharacterVisualScaleMultiplier * visualScaleCompensation, 1f);

            Vector2 pivotCorrection = PivotCorrection(sprite, referenceSprite, normalization);
            pivotCorrection = Vector2.Scale(pivotCorrection,
                new Vector2(scaleX, scaleY) *
                (CharacterVisualScaleMultiplier * visualScaleCompensation));
            pivotCorrection = Quaternion.Euler(0f, 0f, pose.rotation) * pivotCorrection;
            part.localPosition = new Vector3(pose.position.x + pivotCorrection.x,
                pose.position.y + pivotCorrection.y, part.localPosition.z);
        }

        private static Vector2 ReferenceScale(Sprite sprite, Sprite referenceSprite)
        {
            if (sprite == null || referenceSprite == null || sprite == referenceSprite) return Vector2.one;
            Vector2 source = sprite.bounds.size;
            Vector2 reference = referenceSprite.bounds.size;
            return new Vector2(reference.x / Mathf.Max(.001f, source.x),
                reference.y / Mathf.Max(.001f, source.y));
        }

        private static Vector2 PivotCorrection(Sprite sprite, Sprite referenceSprite, Vector2 normalization)
        {
            if (sprite == null || referenceSprite == null || sprite == referenceSprite) return Vector2.zero;
            return (Vector2)referenceSprite.bounds.center - Vector2.Scale(sprite.bounds.center, normalization);
        }
    }

}
