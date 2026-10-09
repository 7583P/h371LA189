using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    public sealed partial class RacingVehiclePhysics
    {
        public const float MiniHopImpulse = 2.8f;
        public const float MiniHopCooldown = .40f;
        float miniHopElapsed = -1f, miniHopCooldown;
        Vector3 miniHopNormal = Vector3.up;
        public bool IsMiniHopping => miniHopElapsed >= 0;
        public int MiniHopsPerformed { get; private set; }
        public event System.Action OnMiniHop;

        void StepMiniHop(VehicleCommands cmd, float dt)
        {
            miniHopCooldown = Mathf.Max(0, miniHopCooldown - dt);
            if (IsMiniHopping)
            {
                miniHopElapsed += dt;
                if ((isGrounded && miniHopElapsed >= .12f) || miniHopElapsed >= .38f ||
                    spinoutTimer > 0 || IsLancerActive || (recovery != null && recovery.IsRecovering))
                    miniHopElapsed = -1f;
            }
            if (cmd.MiniHop && !cmd.Jump && isGrounded && !isDrifting && miniHopCooldown <= 0 &&
                spinoutTimer <= 0 && !IsLancerActive && (recovery == null || !recovery.IsRecovering))
            {
                miniHopNormal = groundNormal;
                miniHopElapsed = 0;
                miniHopCooldown = MiniHopCooldown;
                // A short impulse, never a transform teleport or a stat rewrite.
                body.AddForce(miniHopNormal * MiniHopImpulse, ForceMode.VelocityChange);
                isGrounded = false;
                coyoteTimer = 0;
                MiniHopsPerformed++;
                OnMiniHop?.Invoke();
                RacingFeedback.Play(RacingCue.DriftHop);
            }
            if (IsMiniHopping)
                body.AddForce(Vector3.down * 10f, ForceMode.Acceleration);
        }

        void ApplyMiniHopSteering(VehicleCommands cmd)
        {
            float yaw = cmd.Steering * stats.HandlingRadPerSec * .60f * Mathf.Clamp01(SpeedMps / 8f);
            body.AddTorque(miniHopNormal * (yaw - Vector3.Dot(body.angularVelocity, miniHopNormal)) * 12f,
                ForceMode.Acceleration);
            // Throttle remains throttle; W must not pitch a tiny technical hop.
            Vector3 tiltVelocity = Vector3.ProjectOnPlane(body.angularVelocity, miniHopNormal);
            body.AddTorque(Vector3.Cross(transform.up, miniHopNormal) * 28f * stats.Stability - tiltVelocity * 7f,
                ForceMode.Acceleration);
        }

        void CancelMiniHop() { miniHopElapsed = -1f; miniHopCooldown = 0; }
    }
}
