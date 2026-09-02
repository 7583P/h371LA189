using UnityEngine;

namespace AnimeHillRacing.Terrain
{
    /// <summary>
    /// A floating platform pinned to the water surface. Box2D contact torque makes it lean toward
    /// the loaded side, while the world-space hinge prevents the boat from being pushed underwater.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CityBoatPlatform : MonoBehaviour
    {
        private Rigidbody2D body;
        private HingeJoint2D waterAnchor;
        private BoxCollider2D deckCollider;
        private SpriteRenderer boatRenderer;
        private CityBridgeSettings settings;
        private float anchorX;
        private float anchorY;
        private float nextLaunchTime;

        private void Awake() => EnsureComponents();

        private void EnsureComponents()
        {
            if (body == null) body = GetComponent<Rigidbody2D>();
            if (body == null) body = gameObject.AddComponent<Rigidbody2D>();
            if (waterAnchor == null) waterAnchor = GetComponent<HingeJoint2D>();
            if (waterAnchor == null) waterAnchor = gameObject.AddComponent<HingeJoint2D>();
            if (deckCollider == null) deckCollider = GetComponent<BoxCollider2D>();
            if (deckCollider == null) deckCollider = gameObject.AddComponent<BoxCollider2D>();

            Transform visual = transform.Find("Boat Visual");
            if (visual == null)
            {
                visual = new GameObject("Boat Visual").transform;
                visual.SetParent(transform, false);
            }
            if (boatRenderer == null) boatRenderer = visual.GetComponent<SpriteRenderer>();
            if (boatRenderer == null) boatRenderer = visual.gameObject.AddComponent<SpriteRenderer>();
        }

        public void Configure(CityBridgeSettings bridgeSettings, Sprite sprite, Vector2 waterPosition,
            float visualWidth, bool flip)
        {
            EnsureComponents();
            settings = bridgeSettings;
            boatRenderer.sprite = sprite;
            boatRenderer.flipX = flip;
            boatRenderer.sortingOrder = Mathf.Max(settings.bridgeSortingOrder + 1, 6);
            boatRenderer.enabled = sprite != null;

            float sourceWidth = sprite != null ? Mathf.Max(.001f, sprite.bounds.size.x) : 1f;
            float sourceHeight = sprite != null ? Mathf.Max(.001f, sprite.bounds.size.y) : .4f;
            float scale = Mathf.Max(2.8f, visualWidth) / sourceWidth;
            float visualHeight = sourceHeight * scale;
            boatRenderer.transform.localPosition = new Vector3(0f, 0f, -.16f);
            boatRenderer.transform.localRotation = Quaternion.identity;
            boatRenderer.transform.localScale = Vector3.one * scale;

            anchorX = waterPosition.x;
            anchorY = waterPosition.y + visualHeight * .04f;
            transform.SetPositionAndRotation(new Vector3(anchorX, anchorY, 0f), Quaternion.identity);
            deckCollider.size = new Vector2(Mathf.Max(2.4f, visualWidth * .94f),
                Mathf.Max(.1f, settings.boatColliderThickness));
            deckCollider.offset = new Vector2(0f, visualHeight * settings.boatDeckHeightFraction);
            deckCollider.isTrigger = false;

            body.bodyType = RigidbodyType2D.Dynamic;
            body.simulated = true;
            body.useFullKinematicContacts = true;
            body.mass = Mathf.Max(.1f, settings.boatMass);
            body.gravityScale = 1f;
            body.linearDamping = 0f;
            body.angularDamping = 2.4f;
            body.constraints = RigidbodyConstraints2D.None;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.position = new Vector2(anchorX, anchorY);
            body.rotation = 0f;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            waterAnchor.enabled = false;
            waterAnchor.connectedBody = null;
            waterAnchor.autoConfigureConnectedAnchor = false;
            waterAnchor.anchor = Vector2.zero;
            waterAnchor.connectedAnchor = new Vector2(anchorX, anchorY);
            waterAnchor.useLimits = true;
            float maximumTilt = Mathf.Clamp(settings.boatMaximumTilt, 4f, 18f);
            waterAnchor.limits = new JointAngleLimits2D { min = -maximumTilt, max = maximumTilt };
            waterAnchor.enableCollision = false;
            waterAnchor.enabled = true;
            nextLaunchTime = 0f;
            gameObject.SetActive(true);
            body.WakeUp();
        }

        private void FixedUpdate()
        {
            if (settings == null || body == null || !body.simulated) return;

            float angleRadians = Mathf.DeltaAngle(0f, body.rotation) * Mathf.Deg2Rad;
            float angularVelocityRadians = body.angularVelocity * Mathf.Deg2Rad;
            float restoringTorque = -angleRadians * settings.boatCenteringTorque -
                                    angularVelocityRadians * settings.boatAngularDamping;
            body.AddTorque(restoringTorque * Mathf.Max(.1f, body.inertia), ForceMode2D.Force);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (settings == null || settings.boatLaunchImpulse <= 0f ||
                Time.fixedTime < nextLaunchTime || collision.rigidbody == null ||
                collision.rigidbody.bodyType != RigidbodyType2D.Dynamic ||
                collision.relativeVelocity.magnitude < 1f) return;

            float tilt = Mathf.Abs(Mathf.DeltaAngle(0f, body.rotation)) /
                         Mathf.Max(1f, settings.boatMaximumTilt);
            float impulse = settings.boatLaunchImpulse * Mathf.Lerp(.8f, 1.15f, tilt);
            float direction = Mathf.Abs(collision.rigidbody.linearVelocity.x) > .2f
                ? Mathf.Sign(collision.rigidbody.linearVelocity.x) : 1f;
            collision.rigidbody.AddForce(new Vector2(direction * impulse * .22f, impulse) *
                                         collision.rigidbody.mass, ForceMode2D.Impulse);
            nextLaunchTime = Time.fixedTime + .28f;
        }

        public void Deactivate()
        {
            settings = null;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.simulated = false;
            }
            if (waterAnchor != null) waterAnchor.enabled = false;
            gameObject.SetActive(false);
        }
    }
}
