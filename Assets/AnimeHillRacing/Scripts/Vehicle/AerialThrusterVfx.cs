using UnityEngine;

namespace AnimeHillRacing.Vehicle
{
    public sealed class AerialThrusterVfx : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer leftFlame;
        [SerializeField] private SpriteRenderer rightFlame;
        [SerializeField] private Color flameColor = new Color(.58f, .96f, 1f, 1f);
        [SerializeField, Range(0f, .08f)] private float subtlePulse = .025f;

        private Vector3 leftBaseScale = Vector3.one;
        private Vector3 rightBaseScale = Vector3.one;
        private float power;

        public void Assign(SpriteRenderer left, SpriteRenderer right)
        {
            leftFlame = left;
            rightFlame = right;
            CacheScales();
            SetPower(0f, 0f, 0f);
        }

        private void Awake() => CacheScales();

        private void CacheScales()
        {
            if (leftFlame != null) leftBaseScale = leftFlame.transform.localScale;
            if (rightFlame != null) rightBaseScale = rightFlame.transform.localScale;
        }

        public void SetPower(float leftPower, float rightPower, float tuneProgress)
        {
            float tune = Mathf.Clamp01(tuneProgress);
            power = Mathf.Max(Mathf.Clamp01(leftPower), Mathf.Clamp01(rightPower));
            Apply(leftFlame, leftBaseScale, Mathf.Clamp01(leftPower), tune, 0f);
            Apply(rightFlame, rightBaseScale, Mathf.Clamp01(rightPower), tune, 1.7f);
        }

        private void Apply(SpriteRenderer renderer, Vector3 baseScale, float thrusterPower,
            float tune, float phase)
        {
            if (renderer == null) return;
            bool visible = thrusterPower > .008f;
            renderer.enabled = visible;
            if (!visible) return;

            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 19f + phase) * subtlePulse * thrusterPower;
            float tuneWidth = Mathf.Lerp(.72f, 1.25f, tune);
            float tuneLength = Mathf.Lerp(.68f, 1.55f, tune);
            renderer.transform.localScale = new Vector3(
                baseScale.x * tuneWidth * (.74f + .26f * thrusterPower),
                baseScale.y * tuneLength * (.22f + .78f * thrusterPower) * pulse,
                baseScale.z);
            Color color = Color.Lerp(new Color(.64f, .88f, 1f, 1f), flameColor, tune);
            color.a = Mathf.Lerp(.58f, 1f, tune) * Mathf.SmoothStep(0f, 1f, thrusterPower);
            renderer.color = color;
        }
    }
}
