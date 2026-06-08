using UnityEngine;
using GoogleMobileAds.Api;
using System;
using System.Collections;

public class GoogleAdMobController : MonoBehaviour
{
    public static GoogleAdMobController Instance;

    private BannerView bannerView;
    private InterstitialAd interstitial;

    private bool isBannerLoaded = false;
    private bool isLoadingBanner = false;

    // Test IDs – replace with your own for production
    public string bannerID = "";
    public string interstitialID = "";

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
        MobileAds.Initialize(initStatus =>
        {
            // Start banner creation after a short delay to avoid layout conflicts
            StartCoroutine(CreateBannerCoroutine());
            LoadInterstitial();
        });
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
    }

    // -----------------------------------------------------------
    // INTERSTITIAL
    // -----------------------------------------------------------
    public void LoadInterstitial()
    {
        if (interstitial != null)
        {
            interstitial.Destroy();
            interstitial = null;
        }

        InterstitialAd.Load(interstitialID, new AdRequest(),
            (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null)
                {
                    Debug.LogError($"Interstitial failed: {error.GetMessage()}");
                    Invoke(nameof(LoadInterstitial), 10f);
                    return;
                }
                interstitial = ad;
                Debug.Log("Interstitial loaded.");
            });
    }

    public void ShowInterstitialAd()
    {
        if (interstitial != null && interstitial.CanShowAd())
        {
            interstitial.Show();
            // Reload after showing
            LoadInterstitial();
        }
        else
        {
            Debug.Log("Interstitial not ready, loading...");
            LoadInterstitial();
        }
    }
}