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
            SetPower(0f);
        }

        private void Awake() => CacheScales();

        private void CacheScales()
        {
            if (leftFlame != null) leftBaseScale = leftFlame.transform.localScale;
            if (rightFlame != null) rightBaseScale = rightFlame.transform.localScale;
        }

        public void SetPower(float normalizedPower)
        {
            power = Mathf.Clamp01(normalizedPower);
            Apply(leftFlame, leftBaseScale, 0f);
            Apply(rightFlame, rightBaseScale, 1.7f);
        }

        private void Apply(SpriteRenderer renderer, Vector3 baseScale, float phase)
        {
            if (renderer == null) return;
            bool visible = power > .008f;
            renderer.enabled = visible;
            if (!visible) return;

            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 19f + phase) * subtlePulse * power;
            renderer.transform.localScale = new Vector3(baseScale.x * (.78f + .22f * power),
                baseScale.y * (.18f + .82f * power) * pulse, baseScale.z);
            Color color = flameColor;
            color.a = Mathf.SmoothStep(0f, 1f, power);
            renderer.color = color;
        }
    }
}
