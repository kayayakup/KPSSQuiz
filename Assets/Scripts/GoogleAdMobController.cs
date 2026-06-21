using UnityEngine;
using GoogleMobileAds.Api;
using System;
using System.Collections;
using System.Collections.Generic;

public class GoogleAdMobController : MonoBehaviour
{
    public static GoogleAdMobController Instance;

    private BannerView bannerView;
    private InterstitialAd interstitial;
    private RewardedAd rewardedAd;

    private bool isBannerLoaded = false;
    private bool isLoadingBanner = false;
    private bool _isLoadingInterstitial = false;
    private bool _isLoadingRewarded = false;

    // ── Main-thread dispatcher ──────────────────────────────────────────────
    // AdMob callbacks fire on a background Java thread. Touching Unity objects
    // (UI, MonoBehaviour, etc.) from that thread causes a native SIGABRT crash.
    // We enqueue actions here and drain them in Update() on the main thread.
    private readonly Queue<Action> _mainThreadQueue = new Queue<Action>();
    private readonly object _queueLock = new object();

    private void DispatchToMainThread(Action action)
    {
        lock (_queueLock)
        {
            _mainThreadQueue.Enqueue(action);
        }
    }

    private void DrainMainThreadQueue()
    {
        while (true)
        {
            Action action;
            lock (_queueLock)
            {
                if (_mainThreadQueue.Count == 0) break;
                action = _mainThreadQueue.Dequeue();
            }
            try { action?.Invoke(); }
            catch (Exception e) { Debug.LogError($"[AdMob] Main-thread action threw: {e}"); }
        }
    }
    // ────────────────────────────────────────────────────────────────────────

    // Test IDs – replace with your own for production
    public string bannerID = "ca-app-pub-3940256099942544/6300978111"; // Default test ID
    public string interstitialID = "ca-app-pub-3940256099942544/1033173712"; // Default test ID
    public string rewardedID = "ca-app-pub-3940256099942544/5224354917"; // Google test ID for Android

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // MobileAds.Initialize also fires its callback on a background thread.
        MobileAds.Initialize(initStatus =>
        {
            DispatchToMainThread(() =>
            {
                StartCoroutine(CreateBannerCoroutine());
                LoadInterstitial();
                LoadRewardedAd();
            });
        });
    }

    private void Update()
    {
        DrainMainThreadQueue();
    }

    // -----------------------------------------------------------
    // BANNER: Create once and reuse
    // -----------------------------------------------------------
    private IEnumerator CreateBannerCoroutine()
    {
        // Wait one frame to ensure the activity is fully set up
        yield return null;

        if (bannerView != null)
        {
            // If a banner already exists, just load a new ad into it
            LoadAdIntoBanner();
            yield break;
        }

        // Create new banner
        bannerView = new BannerView(bannerID, AdSize.Banner, AdPosition.Bottom);

        // Subscribe to events
        bannerView.OnBannerAdLoaded += OnBannerLoaded;
        bannerView.OnBannerAdLoadFailed += OnBannerLoadFailed;

        // Load the first ad
        LoadAdIntoBanner();
    }

    private void LoadAdIntoBanner()
    {
        if (bannerView == null)
        {
            Debug.LogWarning("BannerView is null, can't load ad.");
            return;
        }

        if (isLoadingBanner)
        {
            Debug.Log("Banner load already in progress, skipping.");
            return;
        }

        isLoadingBanner = true;
        AdRequest request = new AdRequest();
        bannerView.LoadAd(request);
    }

    private void OnBannerLoaded()
    {
        Debug.Log("Banner loaded successfully.");
        isBannerLoaded = true;
        isLoadingBanner = false;
    }

    private void OnBannerLoadFailed(LoadAdError error)
    {
        Debug.LogError($"Banner failed to load: {error.GetMessage()}");
        isBannerLoaded = false;
        isLoadingBanner = false;

        // Retry after 10 seconds (without destroying the banner)
        Invoke(nameof(RetryBannerLoad), 10f);
    }

    private void RetryBannerLoad()
    {
        if (bannerView != null && !isLoadingBanner)
        {
            LoadAdIntoBanner();
        }
    }

    // Show banner if loaded; otherwise start loading
    public void ShowBanner()
    {
        if (bannerView == null)
        {
            StartCoroutine(CreateBannerCoroutine());
            return;
        }

        if (isBannerLoaded)
        {
            bannerView.Show();
        }
        else
        {
            Debug.Log("Banner not ready, loading now...");
            if (!isLoadingBanner)
                LoadAdIntoBanner();
        }
    }

    public void HideBanner()
    {
        if (bannerView != null)
        {
            bannerView.Hide();
        }
    }

    // Clean up when the game ends
    private void OnDestroy()
    {
        if (bannerView != null)
        {
            // Unsubscribe to avoid memory leaks
            bannerView.OnBannerAdLoaded -= OnBannerLoaded;
            bannerView.OnBannerAdLoadFailed -= OnBannerLoadFailed;
            bannerView.Destroy();
            bannerView = null;
        }

        if (interstitial != null)
        {
            interstitial.Destroy();
            interstitial = null;
        }

        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }
    }

    // -----------------------------------------------------------
    // INTERSTITIAL
    // -----------------------------------------------------------
    public void LoadInterstitial()
    {
        if (_isLoadingInterstitial)
        {
            Debug.Log("[AdMob] Interstitial already loading, skipping.");
            return;
        }

        if (interstitial != null)
        {
            interstitial.Destroy();
            interstitial = null;
        }

        _isLoadingInterstitial = true;
        InterstitialAd.Load(interstitialID, new AdRequest(),
            (InterstitialAd ad, LoadAdError error) =>
            {
                _isLoadingInterstitial = false;
                if (error != null)
                {
                    Debug.LogError($"Interstitial failed: {error.GetMessage()}");
                    DispatchToMainThread(() => Invoke(nameof(LoadInterstitial), 10f));
                    return;
                }
                interstitial = ad;
                Debug.Log("Interstitial loaded.");
            });
    }

    /// <summary>
    /// Interstitial reklamı gösterir. Reklam kapandığında onComplete çağrılır.
    /// Time.timeScale yönetimi burada yapılır — SDK PauseGame çağrısı oyunu dondurabilir.
    /// </summary>
    public void ShowInterstitialAd(System.Action onComplete = null)
    {
        if (interstitial != null && interstitial.CanShowAd())
        {
            // Callbacks fire on a background Java thread — dispatch to main thread.
            interstitial.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("Interstitial closed.");
                DispatchToMainThread(() =>
                {
                    Time.timeScale = 1f;
                    onComplete?.Invoke();
                    LoadInterstitial();
                });
            };

            interstitial.OnAdFullScreenContentFailed += (AdError adError) =>
            {
                Debug.LogWarning($"Interstitial failed to show: {adError.GetMessage()}");
                DispatchToMainThread(() =>
                {
                    Time.timeScale = 1f;
                    onComplete?.Invoke();
                    LoadInterstitial();
                });
            };

            try
            {
                interstitial.Show();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error showing interstitial: {e.Message}");
                Time.timeScale = 1f;
                onComplete?.Invoke();
                LoadInterstitial();
            }
        }
        else
        {
            Debug.Log("Interstitial not ready, loading...");
            LoadInterstitial();
            onComplete?.Invoke();
        }
    }

    // -----------------------------------------------------------
    // REWARDED AD
    // -----------------------------------------------------------
    public void LoadRewardedAd()
    {
        if (_isLoadingRewarded)
        {
            Debug.Log("[AdMob] RewardedAd already loading, skipping.");
            return;
        }

        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        _isLoadingRewarded = true;
        RewardedAd.Load(rewardedID, new AdRequest(),
            (RewardedAd ad, LoadAdError error) =>
            {
                _isLoadingRewarded = false;
                if (error != null)
                {
                    Debug.LogError($"RewardedAd failed to load: {error.GetMessage()}");
                    DispatchToMainThread(() => Invoke(nameof(LoadRewardedAd), 10f));
                    return;
                }
                rewardedAd = ad;
                Debug.Log("RewardedAd loaded.");
            });
    }

    public void ShowRewardedAd(Action<bool> onAdComplete)
    {
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            bool rewardEarned = false;
            // Guard against both Closed AND Failed firing (which would double-invoke the callback).
            bool callbackFired = false;

            // All callbacks fire on a background Java thread.
            // Dispatch everything to Unity's main thread to avoid native SIGABRT crash.
            rewardedAd.OnAdFullScreenContentClosed += () =>
            {
                DispatchToMainThread(() =>
                {
                    if (callbackFired) return;
                    callbackFired = true;
                    onAdComplete?.Invoke(rewardEarned);
                    LoadRewardedAd();
                });
            };

            rewardedAd.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Debug.LogWarning($"RewardedAd failed to show: {error.GetMessage()}");
                DispatchToMainThread(() =>
                {
                    if (callbackFired) return;
                    callbackFired = true;
                    onAdComplete?.Invoke(false);
                    LoadRewardedAd();
                });
            };

            rewardedAd.Show((Reward reward) =>
            {
                // Fires on a background thread — only set the local flag.
                // The actual Unity work happens in OnAdFullScreenContentClosed above.
                Debug.Log("Rewarded ad granted reward.");
                rewardEarned = true;
            });
        }
        else
        {
            Debug.Log("RewardedAd not ready, loading...");
            LoadRewardedAd();
            // Invoke immediately so the caller is not blocked.
            onAdComplete?.Invoke(false);
        }
    }
}