using System.Collections.Generic;
using AnimeHillRacing.Core;
using AnimeHillRacing.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace AnimeHillRacing.EditorTools
{
    public static class RuntimeSafetyVerifier
    {
        [MenuItem("Anime Hill Racing/Validation/Validate Runtime Safety")]
        public static void ValidateRuntimeSafety()
        {
            List<string> errors = new List<string>();

            if (SaveManager.TemporaryUnlimitedCurrencyEnabled)
                errors.Add("Temporary unlimited currency must be disabled.");

            ExpectLowFuel(errors, "exact five-second boundary", true, 10f, 2f, 5f);
            ExpectLowFuel(errors, "above five-second boundary", false, 10.01f, 2f, 5f);
            ExpectLowFuel(errors, "zero consumption", false, 1f, 0f, 5f);
            ExpectLowFuel(errors, "paused/negative consumption", false, 1f, -1f, 5f);
            ExpectLowFuel(errors, "empty fuel", false, 0f, 2f, 5f);
            ExpectLowFuel(errors, "non-finite fuel", false, float.PositiveInfinity, 2f, 5f);
            ExpectLowFuel(errors, "non-finite consumption", false, 1f, float.PositiveInfinity, 5f);
            ExpectLowFuel(errors, "normal consumption before threshold", false, 12.5f, 1f, 5f);
            ExpectLowFuel(errors, "thruster-rate threshold", true, 12.5f, 2.5f, 5f);
            ExpectGameOverCountdown(errors, RunEndReason.OutOfFuel, 2.99f, "OUT OF FUEL\n3");
            ExpectGameOverCountdown(errors, RunEndReason.DriverDown, 1.99f, "DRIVER DOWN\n2");
            ExpectGameOverCountdown(errors, RunEndReason.DriverDownAndOutOfFuel, .25f,
                "DRIVER DOWN\nOUT OF FUEL\n1");
            ValidateGarageRuntimeContract(errors);

            if (errors.Count > 0)
                throw new BuildFailedException(string.Join("\n", errors));

            Debug.Log("Runtime safety validation passed: real currency, low-fuel policy and Garage contract are valid.");
        }

        private static void ExpectLowFuel(List<string> errors, string scenario, bool expected,
            float currentFuel, float consumptionPerSecond, float thresholdSeconds)
        {
            bool actual = GameplayHUD.ShouldDisplayLowFuel(currentFuel, consumptionPerSecond,
                thresholdSeconds);
            if (actual != expected)
                errors.Add($"Low-fuel policy failed for {scenario}: expected {expected}, got {actual}.");
        }

        private static void ExpectGameOverCountdown(List<string> errors, RunEndReason reason,
            float remainingSeconds, string expected)
        {
            string actual = GameplayHUD.FormatGameOverCountdown(reason, remainingSeconds);
            if (actual != expected)
                errors.Add($"Game-over countdown failed for {reason}: expected {expected}, got {actual}.");
            if (!StylizedBitmapFont.SupportsAllVisibleGlyphs(actual, false))
                errors.Add($"Game-over countdown for {reason} contains glyphs outside the stylized atlas.");
        }

        private static void ValidateGarageRuntimeContract(List<string> errors)
        {
            System.Type bindings = typeof(HillAdventurerGarageBindings);
            string[] requiredBindings =
            {
                "startButton", "topNavigation", "garageBackground", "backgroundButtons",
                "shopMenuBackground", "defaultMenuBackground", "neonMenuBackground",
                "menuBackgroundButtons", "masterAudioToggle", "tuneIcons", "tuneCoinIcons",
                "tuneCardButtons", "tuneAffordableBackground",
                "tuneUnaffordableBackground", "confirmationIcon", "confirmationCoinIcon",
                "confirmationDescription"
            };
            for (int i = 0; i < requiredBindings.Length; i++)
                if (bindings.GetField(requiredBindings[i]) == null)
                    errors.Add("Garage binding is missing: " + requiredBindings[i]);

            System.Type garage = typeof(GarageUI);
            if (garage.GetMethod("AssignHillAdventurer",
                    new[] { typeof(HillAdventurerGarageBindings) }) == null)
                errors.Add("GarageUI.AssignHillAdventurer contract is missing.");
            if (garage.GetMethod("StartAdventure", System.Type.EmptyTypes) == null)
                errors.Add("GarageUI.StartAdventure contract is missing.");
            if (garage.GetMethod("SetMasterAudioEnabled", new[] { typeof(bool) }) == null)
                errors.Add("GarageUI.SetMasterAudioEnabled contract is missing.");

            System.Type preview = typeof(GaragePreviewRenderer);
            if (preview.GetMethod("SetTarget", new[] { typeof(UnityEngine.UI.RawImage) }) == null ||
                preview.GetProperty("PreviewRoot") == null)
                errors.Add("GaragePreviewRenderer reusable target contract is incomplete.");
        }
    }
}
