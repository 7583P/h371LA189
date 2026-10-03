using UnityEngine;

namespace AnimeHillRacing.Racing3D
{
    // One definition for physics thresholds, HUD, tire sparks and release effects.
    public static class RacingDriftFeedback
    {
        public const float GreenCharge = .65f, CyanCharge = 1.55f, PurpleCharge = 2.7f;
        public static readonly Color Green = new Color(.12f, 1f, .34f);
        public static readonly Color Cyan = new Color(.08f, .94f, 1f);
        public static readonly Color Purple = new Color(.76f, .28f, 1f);
        public static Color ColorForStage(MiniTurboStage stage) =>
            stage == MiniTurboStage.UltraMiniTurbo ? Purple : stage == MiniTurboStage.SuperMiniTurbo ? Cyan : Green;
        public static string ChargeLabel(MiniTurboStage stage) =>
            stage == MiniTurboStage.UltraMiniTurbo ? "PURPLE / MAX TURBO - RELEASE DRIFT" :
            stage == MiniTurboStage.SuperMiniTurbo ? "CYAN / SUPER TURBO - RELEASE DRIFT" :
            stage == MiniTurboStage.MiniTurbo ? "GREEN / TURBO READY - RELEASE DRIFT" : "HOLD DRIFT + STEER TO CHARGE";
        public static MiniTurboStage StageForCharge(float charge) =>
            charge >= PurpleCharge ? MiniTurboStage.UltraMiniTurbo : charge >= CyanCharge ? MiniTurboStage.SuperMiniTurbo :
            charge >= GreenCharge ? MiniTurboStage.MiniTurbo : MiniTurboStage.None;
    }
}
