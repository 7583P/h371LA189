using System;
using UnityEngine;

#if UNITY_ANDROID || UNITY_EDITOR
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

namespace AnimeHillRacing.Core
{
    /// <summary>Google Play Games v2 adapter. Every failure leaves local gameplay available.</summary>
    public sealed class GooglePlayGamesProvider : IAuthenticationProvider, ILeaderboardProvider
    {
        private bool automaticAttempted;
        private bool clientActivated;
        private ExternalAuthenticationState state = ExternalAuthenticationState.NotInitialized;
        private string lastError = string.Empty;

        public bool IsAvailable => Application.platform == RuntimePlatform.Android;

        public bool IsConfigured
        {
            get
            {
#if UNITY_ANDROID || UNITY_EDITOR
                PlayGamesSettings settings = PlayGamesSettings.LoadInstance();
                return settings != null && !string.IsNullOrWhiteSpace(settings.AppId);
#else
                return false;
#endif
            }
        }

        public bool IsAuthenticated
        {
            get
            {
#if UNITY_ANDROID
                return clientActivated && PlayGamesPlatform.Instance.IsAuthenticated();
#else
                return false;
#endif
            }
        }

        public ExternalAuthenticationState State => state;
        public string LastError => lastError;
        public event Action StateChanged;

        public void Authenticate(bool interactive)
        {
            if (state == ExternalAuthenticationState.Authenticating) return;
            if (!interactive && automaticAttempted) return;
            if (!interactive) automaticAttempted = true;

            if (!IsAvailable)
            {
                SetState(ExternalAuthenticationState.Failed,
                    "GOOGLE PLAY GAMES IS ONLY AVAILABLE IN AN ANDROID PLAYER");
                return;
            }
            if (!IsConfigured)
            {
                SetState(ExternalAuthenticationState.Failed,
                    "GOOGLE PLAY GAMES APP ID NOT CONFIGURED");
                return;
            }

#if UNITY_ANDROID
            ActivateClient();
            if (PlayGamesPlatform.Instance.IsAuthenticated())
            {
                SetState(ExternalAuthenticationState.Authenticated, string.Empty);
                return;
            }

            SetState(ExternalAuthenticationState.Authenticating, string.Empty);
            Debug.Log(interactive
                ? "[GPGS] Manual authentication started"
                : "[GPGS] Authentication started");
            Action<SignInStatus> completed = HandleAuthentication;
            if (interactive)
                PlayGamesPlatform.Instance.ManuallyAuthenticate(completed);
            else
                PlayGamesPlatform.Instance.Authenticate(completed);
#endif
        }

        public bool TryShowLeaderboard(string mapId, string leaderboardId,
            out string unavailableReason)
        {
            if (string.IsNullOrWhiteSpace(leaderboardId))
            {
                unavailableReason = "LEADERBOARD ID NOT CONFIGURED";
                return false;
            }
            if (!IsAvailable || !IsConfigured)
            {
                unavailableReason = "GOOGLE PLAY GAMES NOT CONFIGURED";
                return false;
            }
            if (!IsAuthenticated)
            {
                unavailableReason = "GOOGLE PLAY NOT CONNECTED";
                return false;
            }

#if UNITY_ANDROID
            Debug.Log($"[Leaderboard] Opening native UI for {SafeMapId(mapId)}");
            PlayGamesPlatform.Instance.ShowLeaderboardUI(leaderboardId,
                status => Debug.Log($"[Leaderboard] Native UI closed: {status}"));
#endif
            unavailableReason = string.Empty;
            return true;
        }

        public void SubmitScore(string mapId, string leaderboardId, long scoreMeters,
            Action<bool> completed)
        {
            if (string.IsNullOrWhiteSpace(leaderboardId) || scoreMeters < 0 || !IsAuthenticated)
            {
                completed?.Invoke(false);
                return;
            }

#if UNITY_ANDROID
            string safeMapId = SafeMapId(mapId);
            Debug.Log($"[Leaderboard] Submit {safeMapId}: {scoreMeters} m");
            PlayGamesPlatform.Instance.ReportScore(scoreMeters, leaderboardId, succeeded =>
            {
                Debug.Log(succeeded
                    ? $"[Leaderboard] Submission success: {safeMapId}"
                    : $"[Leaderboard] Submission failed: {safeMapId}");
                completed?.Invoke(succeeded);
            });
#else
            completed?.Invoke(false);
#endif
        }

#if UNITY_ANDROID
        private void ActivateClient()
        {
            if (clientActivated) return;
            PlayGamesPlatform.Activate();
            clientActivated = true;
        }

        private void HandleAuthentication(SignInStatus result)
        {
            if (result == SignInStatus.Success && PlayGamesPlatform.Instance.IsAuthenticated())
            {
                Debug.Log("[GPGS] Authentication success");
                SetState(ExternalAuthenticationState.Authenticated, string.Empty);
                return;
            }

            string reason = result == SignInStatus.Canceled
                ? "GOOGLE PLAY SIGN-IN CANCELLED"
                : $"GOOGLE PLAY SIGN-IN FAILED ({result})";
            Debug.LogWarning($"[GPGS] Authentication failed: {result}");
            SetState(ExternalAuthenticationState.Failed, reason);
        }
#endif

        private void SetState(ExternalAuthenticationState next, string error)
        {
            bool changed = state != next || lastError != (error ?? string.Empty);
            state = next;
            lastError = error ?? string.Empty;
            if (changed) StateChanged?.Invoke();
        }

        private static string SafeMapId(string mapId) =>
            string.IsNullOrWhiteSpace(mapId) ? "unknown-map" : mapId;
    }
}
