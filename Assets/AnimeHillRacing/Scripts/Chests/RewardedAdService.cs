using System;
using UnityEngine;

namespace AnimeHillRacing.Chests
{
    /// <summary>
    /// Production ad SDK adapters register here and must invoke completion on Unity's main thread.
    /// With no provider, rewards stay unavailable; opening a UI or running a development build must
    /// never synthesize a successful rewarded-ad callback.
    /// </summary>
    public interface IRewardedAdProvider
    {
        bool IsReady { get; }
        void Show(Action<bool> completion);
    }

    public static class RewardedAdService
    {
        private static IRewardedAdProvider provider;

        public static bool IsReady
        {
            get
            {
                try
                {
                    return provider != null && provider.IsReady;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("[AdMob] Rewarded readiness check failed: " + exception.Message);
                    return false;
                }
            }
        }

        public static void RegisterProvider(IRewardedAdProvider value) => provider = value;

        public static void Show(Action<bool> completion)
        {
            if (completion == null) return;
            IRewardedAdProvider activeProvider = provider;
            bool completionSent = false;
            void CompleteOnce(bool success)
            {
                if (completionSent) return;
                completionSent = true;
                try
                {
                    completion(success);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            if (activeProvider == null)
            {
                CompleteOnce(false);
                return;
            }

            try
            {
                if (!activeProvider.IsReady)
                {
                    CompleteOnce(false);
                    return;
                }
                activeProvider.Show(CompleteOnce);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[AdMob] Rewarded provider failed: " + exception.Message);
                CompleteOnce(false);
            }
        }
    }
}
