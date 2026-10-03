using System;
using System.Collections;
using AnimeHillRacing.Chests;
using AnimeHillRacing.Core;
using AnimeHillRacing.TimeTrial;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AnimeHillRacing.Ads
{
    /// <summary>
    /// Application-wide owner of Google Mobile Ads objects and presentation policy.
    /// Gameplay systems report authoritative lifecycle changes; scene inspection is used only to
    /// decide whether the current screen is an allowed banner location.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class AdMobManager : MonoBehaviour, IRewardedAdProvider
    {
        public const int InterstitialFrequency = 3;
        public const float BannerVisibleSeconds = 300f;
        public const float BannerHiddenSeconds = 120f;

        private const float RewardCloseGraceSeconds = 2f;
        private const float FullScreenWatchdogSeconds = 600f;
        private const float InterstitialAfterRewardedCooldownSeconds = 30f;
        private const float AdFreshnessSeconds = 55f * 60f;
        private const float InitialRetrySeconds = 10f;
        private const float MaximumRetrySeconds = 300f;
        private const float LoadWatchdogSeconds = 60f;

        private static AdMobManager instance;
        private static bool consentStatusFailureLogged;

        private AdMobConfiguration configuration;
        private RewardedAd rewardedAd;
        private RewardedAd presentedRewardedAd;
        private InterstitialAd interstitialAd;
        private InterstitialAd presentedInterstitialAd;
        private BannerView bannerView;
        private Action<bool> rewardedCompletion;
        private Coroutine rewardCloseGraceRoutine;

        private bool initialized;
        private bool initializationStarted;
        private bool consentUpdateStarted;
        private bool consentFormInProgress;
        private bool shuttingDown;
        private bool rewardedLoadInProgress;
        private bool interstitialLoadInProgress;
        private bool bannerLoadInProgress;
        private bool bannerLoaded;
        private bool bannerShowing;
        private bool fullScreenAdInProgress;
        private bool rewardedEarned;
        private bool gameplayActive;
        private bool bannerLocationAllowed;
        private bool runRegisteredForCurrentGameplay;
        private bool applicationPaused;

        private int rewardedLoadFailures;
        private int interstitialLoadFailures;
        private int bannerLoadFailures;
        private int initializationFailures;
        private int rewardedLoadGeneration;
        private int interstitialLoadGeneration;
        private int bannerLoadGeneration;
        private int bannerScreenWidth;

        private double applicationStartedRealtime;
        private double rewardedLoadedRealtime;
        private double interstitialLoadedRealtime;
        private double rewardedLoadStartedRealtime;
        private double interstitialLoadStartedRealtime;
        private double bannerLoadStartedRealtime;
        private double nextRewardedLoadRealtime;
        private double nextInterstitialLoadRealtime;
        private double nextBannerLoadRealtime;
        private double nextInitializationRealtime;
        private double fullScreenStartedRealtime;
        private double lastRewardedClosedRealtime = double.NegativeInfinity;

        public static AdMobManager Instance => instance;

        public bool IsReady => initialized && !shuttingDown && !fullScreenAdInProgress &&
                               !consentFormInProgress &&
                               rewardedAd != null && rewardedAd.CanShowAd();

        public static bool PrivacyOptionsRequired
        {
            get
            {
                try
                {
                    return ConsentInformation.PrivacyOptionsRequirementStatus ==
                           PrivacyOptionsRequirementStatus.Required;
                }
                catch
                {
                    return false;
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            consentStatusFailureLogged = false;
            RewardedAdService.RegisterProvider(null);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreatePersistentManager()
        {
            if (instance != null) return;
            var root = new GameObject("Hill Adventurer AdMob");
            DontDestroyOnLoad(root);
            root.AddComponent<AdMobManager>();
        }

        public static void SetGameplayActive(bool active)
        {
            if (instance != null) instance.ApplyGameplayActive(active);
        }

        public static void NotifyRunCompleted()
        {
            if (instance != null) instance.RegisterCompletedRun();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            applicationStartedRealtime = Time.realtimeSinceStartupAsDouble;
            configuration = Resources.Load<AdMobConfiguration>("AdMobConfiguration");
            RewardedAdService.RegisterProvider(this);
            SceneManager.sceneLoaded += OnSceneLoaded;

            if (configuration == null)
            {
                Debug.LogError("[AdMob] Resources/AdMobConfiguration is missing; ads are disabled");
                return;
            }
            if (!configuration.ValidateCurrentPlatform(out string error))
            {
                Debug.LogError("[AdMob] " + error + "; ads are disabled");
                return;
            }

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
            BeginConsentFlow();
#else
            Debug.Log("[AdMob] Ads are disabled on this platform");
#endif
        }

        /// <summary>
        /// Opens Google's privacy choices form. A menu settings/privacy button can call this
        /// without needing to know about the UMP SDK.
        /// </summary>
        public static void ShowPrivacyOptions(Action<bool> completion = null)
        {
            if (instance == null || instance.shuttingDown || instance.consentFormInProgress)
            {
                SafeComplete(completion, false);
                return;
            }
            instance.ShowPrivacyOptionsInternal(completion);
        }

        private void BeginConsentFlow()
        {
            if (consentUpdateStarted || shuttingDown) return;
            consentUpdateStarted = true;

            // A valid choice from a previous launch may already allow initialization while UMP
            // refreshes its status. No ad is requested if UMP has not authorized requests.
            if (ConsentAllowsAds()) InitializeSdk();

            try
            {
                ConsentInformation.Update(new ConsentRequestParameters(), updateError =>
                    RunOnUnityThread(() =>
                    {
                        if (shuttingDown) return;
                        if (updateError != null)
                        {
                            Debug.LogWarning("[AdMob] Consent update failed: " +
                                             FormatError(updateError));
                            if (ConsentAllowsAds()) InitializeSdk();
                            return;
                        }

                        if (ConsentAllowsAds())
                        {
                            InitializeSdk();
                            return;
                        }

                        ShowRequiredConsentForm();
                    }));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[AdMob] Consent update could not start: " + exception.Message);
                if (ConsentAllowsAds()) InitializeSdk();
            }
        }

        private void ShowRequiredConsentForm()
        {
            if (consentFormInProgress || shuttingDown) return;
            consentFormInProgress = true;
            UpdateBannerVisibility();
            try
            {
                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                    RunOnUnityThread(() =>
                    {
                        consentFormInProgress = false;
                        if (shuttingDown) return;
                        if (formError != null)
                            Debug.LogWarning("[AdMob] Consent form failed: " +
                                             FormatError(formError));
                        if (ConsentAllowsAds()) InitializeSdk();
                        else Debug.Log("[AdMob] Ads unavailable until consent permits requests");
                        UpdateBannerVisibility();
                    }));
            }
            catch (Exception exception)
            {
                consentFormInProgress = false;
                Debug.LogWarning("[AdMob] Consent form could not open: " + exception.Message);
                if (ConsentAllowsAds()) InitializeSdk();
                UpdateBannerVisibility();
            }
        }

        private void ShowPrivacyOptionsInternal(Action<bool> completion)
        {
            consentFormInProgress = true;
            UpdateBannerVisibility();
            try
            {
                ConsentForm.ShowPrivacyOptionsForm(formError => RunOnUnityThread(() =>
                {
                    consentFormInProgress = false;
                    if (formError != null)
                        Debug.LogWarning("[AdMob] Privacy options failed: " + FormatError(formError));
                    SafeComplete(completion, formError == null);
                    UpdateBannerVisibility();
                }));
            }
            catch (Exception exception)
            {
                consentFormInProgress = false;
                Debug.LogWarning("[AdMob] Privacy options could not open: " + exception.Message);
                SafeComplete(completion, false);
                UpdateBannerVisibility();
            }
        }

        private void InitializeSdk()
        {
            if (initializationStarted || initialized || shuttingDown) return;
            initializationStarted = true;
            try
            {
                MobileAds.Initialize(status => RunOnUnityThread(() =>
                {
                    if (shuttingDown) return;
                    if (status == null)
                    {
                        initializationStarted = false;
                        ScheduleRetry(ref initializationFailures, ref nextInitializationRealtime);
                        Debug.LogWarning("[AdMob] SDK initialization failed; ads remain optional");
                        return;
                    }

                    initializationFailures = 0;
                    initialized = true;
                    Debug.Log(configuration.useTestAds
                        ? "[AdMob] SDK initialized (TEST ADS)"
                        : "[AdMob] SDK initialized (PRODUCTION ADS)");
                    RequestRewardedAd();
                    RequestInterstitialAd();
                    RequestBannerAd();
                }));
            }
            catch (Exception exception)
            {
                initializationStarted = false;
                ScheduleRetry(ref initializationFailures, ref nextInitializationRealtime);
                Debug.LogWarning("[AdMob] SDK initialization could not start: " + exception.Message);
            }
        }

        private void Update()
        {
            // A desktop player never starts the mobile consent flow. Do not
            // query UMP every frame there: its unsupported client throws.
            if (shuttingDown || !consentUpdateStarted) return;
            double now = Time.realtimeSinceStartupAsDouble;
            if (!initialized)
            {
                if (!initializationStarted && now >= nextInitializationRealtime && ConsentAllowsAds())
                    InitializeSdk();
                return;
            }

            if (fullScreenAdInProgress && now - fullScreenStartedRealtime >= FullScreenWatchdogSeconds)
                RecoverFromFullScreenTimeout();

            if (rewardedLoadInProgress && now - rewardedLoadStartedRealtime >= LoadWatchdogSeconds)
            {
                rewardedLoadGeneration++;
                rewardedLoadInProgress = false;
                ScheduleRetry(ref rewardedLoadFailures, ref nextRewardedLoadRealtime);
                Debug.LogWarning("[AdMob] Rewarded load timed out; retry scheduled");
            }
            if (interstitialLoadInProgress &&
                now - interstitialLoadStartedRealtime >= LoadWatchdogSeconds)
            {
                interstitialLoadGeneration++;
                interstitialLoadInProgress = false;
                ScheduleRetry(ref interstitialLoadFailures, ref nextInterstitialLoadRealtime);
                Debug.LogWarning("[AdMob] Interstitial load timed out; retry scheduled");
            }
            if (bannerLoadInProgress && now - bannerLoadStartedRealtime >= LoadWatchdogSeconds)
            {
                ScheduleRetry(ref bannerLoadFailures, ref nextBannerLoadRealtime);
                Debug.LogWarning("[AdMob] Banner load timed out; recreating after retry");
                DestroyBanner();
            }

            if (rewardedAd != null &&
                (!rewardedAd.CanShowAd() || now - rewardedLoadedRealtime >= AdFreshnessSeconds) &&
                presentedRewardedAd == null)
            {
                DestroyRewarded(rewardedAd);
                rewardedAd = null;
                RequestRewardedAd();
            }
            else if (rewardedAd == null && !rewardedLoadInProgress && now >= nextRewardedLoadRealtime)
                RequestRewardedAd();

            if (interstitialAd != null &&
                (!interstitialAd.CanShowAd() || now - interstitialLoadedRealtime >= AdFreshnessSeconds) &&
                presentedInterstitialAd == null)
            {
                DestroyInterstitial(interstitialAd);
                interstitialAd = null;
                RequestInterstitialAd();
            }
            else if (interstitialAd == null && !interstitialLoadInProgress &&
                     now >= nextInterstitialLoadRealtime)
                RequestInterstitialAd();

            if (bannerView == null && !bannerLoadInProgress && now >= nextBannerLoadRealtime)
                RequestBannerAd();
            else if (bannerView != null && !bannerLoaded && !bannerLoadInProgress &&
                     now >= nextBannerLoadRealtime)
                LoadExistingBanner();

            if (bannerView != null && !fullScreenAdInProgress && Screen.width != bannerScreenWidth)
            {
                Debug.Log("[AdMob] Screen width changed; recreating adaptive banner");
                DestroyBanner();
                RequestBannerAd();
            }

            UpdateBannerVisibility();
        }

        public void Show(Action<bool> completion)
        {
            if (completion == null) return;
            if (!IsReady)
            {
                SafeComplete(completion, false);
                if (initialized) RequestRewardedAd();
                return;
            }

            RewardedAd ad = rewardedAd;
            rewardedAd = null;
            presentedRewardedAd = ad;
            rewardedCompletion = completion;
            rewardedEarned = false;
            BeginFullScreenAd();

            try
            {
                ad.Show(reward => RunOnUnityThread(() => OnRewardEarned(ad, reward)));
                Debug.Log("[AdMob] Rewarded shown");
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[AdMob] Rewarded failed to show: " + exception.Message);
                CompleteRewardedRequest(false);
                FinishPresentedRewarded(ad);
            }
        }

        private void RequestRewardedAd()
        {
            if (!initialized || shuttingDown || rewardedLoadInProgress || rewardedAd != null ||
                string.IsNullOrWhiteSpace(configuration.RewardedAdUnitId)) return;

            rewardedLoadInProgress = true;
            rewardedLoadStartedRealtime = Time.realtimeSinceStartupAsDouble;
            int generation = ++rewardedLoadGeneration;
            try
            {
                RewardedAd.Load(configuration.RewardedAdUnitId, new AdRequest(), (ad, error) =>
                    RunOnUnityThread(() =>
                    {
                        if (generation != rewardedLoadGeneration || shuttingDown)
                        {
                            DestroyRewarded(ad);
                            return;
                        }

                        rewardedLoadInProgress = false;
                        if (error != null || ad == null)
                        {
                            DestroyRewarded(ad);
                            ScheduleRetry(ref rewardedLoadFailures, ref nextRewardedLoadRealtime);
                            Debug.LogWarning("[AdMob] Rewarded load failed: " + FormatError(error));
                            return;
                        }

                        rewardedLoadFailures = 0;
                        rewardedLoadedRealtime = Time.realtimeSinceStartupAsDouble;
                        rewardedAd = ad;
                        RegisterRewardedEvents(ad);
                        Debug.Log("[AdMob] Rewarded loaded");
                    }));
            }
            catch (Exception exception)
            {
                rewardedLoadInProgress = false;
                ScheduleRetry(ref rewardedLoadFailures, ref nextRewardedLoadRealtime);
                Debug.LogWarning("[AdMob] Rewarded load could not start: " + exception.Message);
            }
        }

        private void RegisterRewardedEvents(RewardedAd ad)
        {
            ad.OnAdFullScreenContentOpened += () => RunOnUnityThread(() =>
            {
                if (presentedRewardedAd == ad) Debug.Log("[AdMob] Rewarded opened");
            });
            ad.OnAdFullScreenContentClosed += () =>
                RunOnUnityThread(() => BeginRewardedCloseGrace(ad));
            ad.OnAdFullScreenContentFailed += error => RunOnUnityThread(() =>
            {
                if (presentedRewardedAd != ad) return;
                Debug.LogWarning("[AdMob] Rewarded failed to present: " + FormatError(error));
                CompleteRewardedRequest(false);
                FinishPresentedRewarded(ad);
            });
        }

        private void OnRewardEarned(RewardedAd ad, Reward reward)
        {
            if (presentedRewardedAd != ad || rewardedEarned) return;
            rewardedEarned = true;
            Debug.Log("[AdMob] Reward granted");
            CompleteRewardedRequest(true);
        }

        private void BeginRewardedCloseGrace(RewardedAd ad)
        {
            if (presentedRewardedAd != ad || rewardCloseGraceRoutine != null) return;
            rewardCloseGraceRoutine = StartCoroutine(FinishRewardedAfterGrace(ad));
        }

        private IEnumerator FinishRewardedAfterGrace(RewardedAd ad)
        {
            // A small grace period protects mediated adapters that deliver the earned-reward
            // callback just after their close callback. No reward is inferred from closing.
            yield return new WaitForSecondsRealtime(RewardCloseGraceSeconds);
            rewardCloseGraceRoutine = null;
            if (presentedRewardedAd != ad) yield break;
            if (!rewardedEarned) CompleteRewardedRequest(false);
            FinishPresentedRewarded(ad);
        }

        private void CompleteRewardedRequest(bool success)
        {
            Action<bool> completion = rewardedCompletion;
            rewardedCompletion = null;
            if (completion != null) SafeComplete(completion, success);
        }

        private void FinishPresentedRewarded(RewardedAd ad)
        {
            if (presentedRewardedAd != ad) return;
            if (rewardCloseGraceRoutine != null)
            {
                StopCoroutine(rewardCloseGraceRoutine);
                rewardCloseGraceRoutine = null;
            }
            DestroyRewarded(ad);
            presentedRewardedAd = null;
            rewardedEarned = false;
            lastRewardedClosedRealtime = Time.realtimeSinceStartupAsDouble;
            EndFullScreenAd();
            RequestRewardedAd();
        }

        private void RequestInterstitialAd()
        {
            if (!initialized || shuttingDown || interstitialLoadInProgress || interstitialAd != null ||
                string.IsNullOrWhiteSpace(configuration.InterstitialAdUnitId)) return;

            interstitialLoadInProgress = true;
            interstitialLoadStartedRealtime = Time.realtimeSinceStartupAsDouble;
            int generation = ++interstitialLoadGeneration;
            try
            {
                InterstitialAd.Load(configuration.InterstitialAdUnitId, new AdRequest(), (ad, error) =>
                    RunOnUnityThread(() =>
                    {
                        if (generation != interstitialLoadGeneration || shuttingDown)
                        {
                            DestroyInterstitial(ad);
                            return;
                        }

                        interstitialLoadInProgress = false;
                        if (error != null || ad == null)
                        {
                            DestroyInterstitial(ad);
                            ScheduleRetry(ref interstitialLoadFailures, ref nextInterstitialLoadRealtime);
                            Debug.LogWarning("[AdMob] Interstitial load failed: " + FormatError(error));
                            return;
                        }

                        interstitialLoadFailures = 0;
                        interstitialLoadedRealtime = Time.realtimeSinceStartupAsDouble;
                        interstitialAd = ad;
                        RegisterInterstitialEvents(ad);
                        Debug.Log("[AdMob] Interstitial loaded");
                    }));
            }
            catch (Exception exception)
            {
                interstitialLoadInProgress = false;
                ScheduleRetry(ref interstitialLoadFailures, ref nextInterstitialLoadRealtime);
                Debug.LogWarning("[AdMob] Interstitial load could not start: " + exception.Message);
            }
        }

        private void RegisterInterstitialEvents(InterstitialAd ad)
        {
            ad.OnAdFullScreenContentOpened += () => RunOnUnityThread(() =>
            {
                if (presentedInterstitialAd == ad) Debug.Log("[AdMob] Interstitial opened");
            });
            ad.OnAdFullScreenContentClosed += () =>
                RunOnUnityThread(() => FinishPresentedInterstitial(ad, null));
            ad.OnAdFullScreenContentFailed += error =>
                RunOnUnityThread(() => FinishPresentedInterstitial(ad, error));
        }

        private void RegisterCompletedRun()
        {
            if (!gameplayActive || runRegisteredForCurrentGameplay)
            {
                Debug.LogWarning("[AdMob] Ignored duplicate completed-run notification");
                return;
            }

            runRegisteredForCurrentGameplay = true;
            gameplayActive = false;
            int progress = SaveManager.RegisterCompletedRunForInterstitial(InterstitialFrequency);
            Debug.Log($"[AdMob] Completed run {progress}/{InterstitialFrequency}");
            UpdateBannerVisibility();
            if (progress == InterstitialFrequency) TryShowDueInterstitial();
        }

        private void TryShowDueInterstitial()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (fullScreenAdInProgress || consentFormInProgress || gameplayActive ||
                now - lastRewardedClosedRealtime < InterstitialAfterRewardedCooldownSeconds)
            {
                Debug.Log("[AdMob] Third-run interstitial skipped due to presentation safety");
                RequestInterstitialAd();
                return;
            }
            if (interstitialAd == null || !interstitialAd.CanShowAd())
            {
                Debug.Log("[AdMob] Third-run interstitial unavailable; continuing without blocking");
                if (interstitialAd != null)
                {
                    DestroyInterstitial(interstitialAd);
                    interstitialAd = null;
                }
                RequestInterstitialAd();
                return;
            }

            InterstitialAd ad = interstitialAd;
            interstitialAd = null;
            presentedInterstitialAd = ad;
            BeginFullScreenAd();
            try
            {
                Debug.Log("[AdMob] Showing third-run interstitial");
                ad.Show();
            }
            catch (Exception exception)
            {
                FinishPresentedInterstitial(ad, exception);
            }
        }

        private void FinishPresentedInterstitial(InterstitialAd ad, object error)
        {
            if (presentedInterstitialAd != ad) return;
            if (error != null)
                Debug.LogWarning("[AdMob] Interstitial failed to present: " + error);
            else
                Debug.Log("[AdMob] Interstitial closed");
            DestroyInterstitial(ad);
            presentedInterstitialAd = null;
            EndFullScreenAd();
            RequestInterstitialAd();
        }

        private void RequestBannerAd()
        {
            if (!initialized || shuttingDown || bannerView != null || bannerLoadInProgress ||
                string.IsNullOrWhiteSpace(configuration.BannerAdUnitId)) return;

            try
            {
                int safeWidth = MobileAds.Utils.GetDeviceSafeWidth();
                if (safeWidth <= 0) safeWidth = AdSize.FullWidth;
                AdSize adaptiveSize =
                    AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(safeWidth);
                BannerView newBanner = new BannerView(configuration.BannerAdUnitId, adaptiveSize,
                    AdPosition.Bottom);
                bannerView = newBanner;
                bannerScreenWidth = Screen.width;
                int generation = ++bannerLoadGeneration;
                newBanner.OnBannerAdLoaded += () => RunOnUnityThread(() =>
                {
                    if (generation != bannerLoadGeneration || bannerView != newBanner) return;
                    bannerLoadInProgress = false;
                    bannerLoaded = true;
                    bannerLoadFailures = 0;
                    Debug.Log("[AdMob] Banner loaded");
                    UpdateBannerVisibility();
                });
                newBanner.OnBannerAdLoadFailed += error => RunOnUnityThread(() =>
                {
                    if (generation != bannerLoadGeneration || bannerView != newBanner) return;
                    bannerLoadInProgress = false;
                    bannerLoaded = false;
                    ScheduleRetry(ref bannerLoadFailures, ref nextBannerLoadRealtime);
                    Debug.LogWarning("[AdMob] Banner load failed: " + FormatError(error));
                });
                newBanner.Hide();
                LoadExistingBanner();
            }
            catch (Exception exception)
            {
                DestroyBanner();
                ScheduleRetry(ref bannerLoadFailures, ref nextBannerLoadRealtime);
                Debug.LogWarning("[AdMob] Banner creation failed: " + exception.Message);
            }
        }

        private void LoadExistingBanner()
        {
            if (bannerView == null || bannerLoadInProgress || shuttingDown) return;
            bannerLoadInProgress = true;
            bannerLoadStartedRealtime = Time.realtimeSinceStartupAsDouble;
            try
            {
                bannerView.LoadAd(new AdRequest());
            }
            catch (Exception exception)
            {
                bannerLoadInProgress = false;
                bannerLoaded = false;
                ScheduleRetry(ref bannerLoadFailures, ref nextBannerLoadRealtime);
                Debug.LogWarning("[AdMob] Banner load could not start: " + exception.Message);
            }
        }

        private void UpdateBannerVisibility()
        {
            if (bannerView == null || !bannerLoaded) return;
            double elapsed = Math.Max(0d, Time.realtimeSinceStartupAsDouble - applicationStartedRealtime);
            double cyclePosition = elapsed % (BannerVisibleSeconds + BannerHiddenSeconds);
            bool cycleVisible = cyclePosition < BannerVisibleSeconds;
            bool shouldShow = cycleVisible && bannerLocationAllowed && !gameplayActive &&
                              !fullScreenAdInProgress && !consentFormInProgress &&
                              !applicationPaused;
            if (shouldShow == bannerShowing) return;

            if (shouldShow)
            {
                try
                {
                    bannerView.Show();
                    bannerShowing = true;
                    Debug.Log("[AdMob] Banner visible");
                }
                catch (Exception exception)
                {
                    HandleBannerRuntimeError("show", exception);
                }
            }
            else
            {
                try
                {
                    bannerView.Hide();
                    bannerShowing = false;
                    Debug.Log("[AdMob] Banner hidden");
                }
                catch (Exception exception)
                {
                    HandleBannerRuntimeError("hide", exception);
                }
            }
        }

        private void ApplyGameplayActive(bool active)
        {
            if (active && !gameplayActive)
                runRegisteredForCurrentGameplay = false;
            gameplayActive = active;
            if (active && bannerShowing)
            {
                try
                {
                    bannerView?.Hide();
                    bannerShowing = false;
                    Debug.Log("[AdMob] Banner hidden because gameplay started");
                }
                catch (Exception exception)
                {
                    HandleBannerRuntimeError("hide for gameplay", exception);
                }
                return;
            }
            UpdateBannerVisibility();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            bool containsGameplayAuthority = FindFirstObjectByType<GameManager>() != null ||
                                             FindFirstObjectByType<TimeTrialManager>() != null;
            bannerLocationAllowed = !containsGameplayAuthority;
            if (containsGameplayAuthority) ApplyGameplayActive(true);
            else ApplyGameplayActive(false);
        }

        private void BeginFullScreenAd()
        {
            fullScreenAdInProgress = true;
            fullScreenStartedRealtime = Time.realtimeSinceStartupAsDouble;
            UpdateBannerVisibility();
        }

        private void EndFullScreenAd()
        {
            fullScreenAdInProgress = false;
            fullScreenStartedRealtime = 0d;
            UpdateBannerVisibility();
        }

        private void RecoverFromFullScreenTimeout()
        {
            Debug.LogWarning("[AdMob] Full-screen callback timed out; recovering without blocking the game");
            if (presentedRewardedAd != null)
            {
                CompleteRewardedRequest(false);
                FinishPresentedRewarded(presentedRewardedAd);
                return;
            }
            if (presentedInterstitialAd != null)
            {
                FinishPresentedInterstitial(presentedInterstitialAd, "callback timeout");
                return;
            }
            EndFullScreenAd();
        }

        private void OnApplicationPause(bool paused)
        {
            applicationPaused = paused;
            UpdateBannerVisibility();
        }

        private void OnApplicationFocus(bool focused)
        {
            applicationPaused = !focused;
            UpdateBannerVisibility();
        }

        private void OnApplicationQuit() => shuttingDown = true;

        private void OnDestroy()
        {
            if (instance != this) return;
            shuttingDown = true;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            RewardedAdService.RegisterProvider(null);
            CompleteRewardedRequest(false);
            DestroyRewarded(rewardedAd);
            DestroyRewarded(presentedRewardedAd);
            DestroyInterstitial(interstitialAd);
            DestroyInterstitial(presentedInterstitialAd);
            DestroyBanner();
            instance = null;
        }

        private void DestroyBanner()
        {
            bannerLoadGeneration++;
            bannerLoadInProgress = false;
            bannerLoaded = false;
            bannerShowing = false;
            if (bannerView == null) return;
            try
            {
                bannerView.Destroy();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[AdMob] Banner destroy failed: " + exception.Message);
            }
            bannerView = null;
        }

        private static void DestroyRewarded(RewardedAd ad)
        {
            if (ad == null) return;
            try
            {
                ad.Destroy();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[AdMob] Rewarded destroy failed: " + exception.Message);
            }
        }

        private static void DestroyInterstitial(InterstitialAd ad)
        {
            if (ad == null) return;
            try
            {
                ad.Destroy();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[AdMob] Interstitial destroy failed: " + exception.Message);
            }
        }

        private void HandleBannerRuntimeError(string operation, Exception exception)
        {
            Debug.LogWarning($"[AdMob] Banner {operation} failed: {exception.Message}");
            DestroyBanner();
            if (initialized && !shuttingDown)
                ScheduleRetry(ref bannerLoadFailures, ref nextBannerLoadRealtime);
        }

        private static void ScheduleRetry(ref int failureCount, ref double nextRetryRealtime)
        {
            failureCount = Mathf.Min(failureCount + 1, 8);
            float delay = Mathf.Min(MaximumRetrySeconds,
                InitialRetrySeconds * Mathf.Pow(2f, failureCount - 1));
            nextRetryRealtime = Time.realtimeSinceStartupAsDouble + delay;
        }

        private static string FormatError(object error) => error != null ? error.ToString() : "unknown error";

        private static bool ConsentAllowsAds()
        {
            try
            {
                return ConsentInformation.CanRequestAds();
            }
            catch (Exception exception)
            {
                if (!consentStatusFailureLogged)
                {
                    consentStatusFailureLogged = true;
                    Debug.LogWarning("[AdMob] Consent status check failed: " + exception.Message);
                }
                return false;
            }
        }

        private static void RunOnUnityThread(Action action)
        {
            if (action != null) MobileAdsEventExecutor.ExecuteInUpdate(action);
        }

        private static void SafeComplete(Action<bool> completion, bool success)
        {
            if (completion == null) return;
            try
            {
                completion(success);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
