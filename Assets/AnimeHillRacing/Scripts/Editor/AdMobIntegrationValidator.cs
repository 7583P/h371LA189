#if UNITY_EDITOR
using System;
using AnimeHillRacing.Chests;
using AnimeHillRacing.Core;
using UnityEditor;
using UnityEngine;

namespace AnimeHillRacing.Editor
{
    /// <summary>Deterministic policy checks that can also run in CI with -executeMethod.</summary>
    public static class AdMobIntegrationValidator
    {
        private const string FrequencyKey = "AHR.Ads.CompletedRunsSinceInterstitial.V1";

        [MenuItem("Tools/Hill Adventurer/AdMob/Validate Integration Policy")]
        public static void ValidateBatch()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("AdMob policy validation requires Edit Mode.");

            bool hadFrequencyValue = PlayerPrefs.HasKey(FrequencyKey);
            int savedFrequencyValue = PlayerPrefs.GetInt(FrequencyKey, 0);
            try
            {
                PlayerPrefs.DeleteKey(FrequencyKey);
                int[] expectedReturns = { 1, 2, 3, 1, 2, 3 };
                int[] expectedStored = { 1, 2, 0, 1, 2, 0 };
                for (int i = 0; i < expectedReturns.Length; i++)
                {
                    int result = SaveManager.RegisterCompletedRunForInterstitial(3);
                    int stored = SaveManager.GetCompletedRunsSinceInterstitial(3);
                    if (result != expectedReturns[i] || stored != expectedStored[i])
                        throw new InvalidOperationException(
                            $"Interstitial frequency failed at run {i + 1}: returned {result}, stored {stored}.");
                }

                int callbacks = 0;
                bool rewarded = false;
                RewardedAdService.RegisterProvider(new DuplicateCallbackProvider());
                RewardedAdService.Show(success =>
                {
                    callbacks++;
                    rewarded = success;
                });
                if (callbacks != 1 || !rewarded)
                    throw new InvalidOperationException(
                        "Rewarded completion was not reduced to one successful callback.");

                callbacks = 0;
                rewarded = true;
                RewardedAdService.RegisterProvider(null);
                RewardedAdService.Show(success =>
                {
                    callbacks++;
                    rewarded = success;
                });
                if (callbacks != 1 || rewarded)
                    throw new InvalidOperationException(
                        "An unavailable rewarded provider did not fail exactly once.");
            }
            finally
            {
                RewardedAdService.RegisterProvider(null);
                if (hadFrequencyValue) PlayerPrefs.SetInt(FrequencyKey, savedFrequencyValue);
                else PlayerPrefs.DeleteKey(FrequencyKey);
                PlayerPrefs.Save();
            }

            Debug.Log("ADMOB_POLICY_VALIDATION_OK: sequence=1,2,AD,1,2,AD; rewarded callback=once");
        }

        private sealed class DuplicateCallbackProvider : IRewardedAdProvider
        {
            public bool IsReady => true;

            public void Show(Action<bool> completion)
            {
                completion(true);
                completion(true);
            }
        }
    }
}
#endif
