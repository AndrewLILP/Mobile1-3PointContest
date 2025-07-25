using UnityEngine;
using UnityEngine.Advertisements;

public class ADManager : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [SerializeField] private string androidGameId = "5901632";
    [SerializeField] private string iosGameId = "5901633";
    [SerializeField] private string interstitialAdUnitId = "Interstitial_Android"; // Replace with your actual Ad Unit ID for Android
    [SerializeField] private string rewardedAdUnitId = "Rewarded_Android"; // Replace with your actual Ad Unit ID for Android
    [SerializeField] private string bannerAdUnitId = "Banner_Android"; // Replace with your actual Ad Unit ID for Android
    [SerializeField] private bool testMode = true; // Set to false for production

    private string gameID;

    void Start()
    {
#if UNITY_ANDROID
        gameID = androidGameId;
#endif
        Advertisement.Initialize(gameID, testMode);

        Advertisement.Load(interstitialAdUnitId, this);
        Advertisement.Load(rewardedAdUnitId, this);
        Advertisement.Load(bannerAdUnitId, this);
    }

    public void ShowInterInterstitialAD()
    {
        if (Advertisement.isInitialized)
        {
            Advertisement.Show(interstitialAdUnitId, this);
        }
        else
        {
            Debug.LogWarning("Ad not ready");
        }
            
    }
    public void ShowBannerAD()
    {
        if (Advertisement.isInitialized)
        {
            Advertisement.Show(bannerAdUnitId, this);
        }
        else
        {
            Debug.LogWarning("Ad not ready");
        }

    }

    public void ShowRewardAD()
    {
        if (Advertisement.isInitialized)
        {
            Advertisement.Show(rewardedAdUnitId, this);
        }
        else
        {
            Debug.LogWarning("Ad not ready");
        }

    }

    public void OnUnityAdsAdLoaded(string placementId)
    {
        
    }
    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        
    }
    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
      
    }
    public void OnUnityAdsShowStart(string placementId)
    {
      
    }
    public void OnUnityAdsShowClick(string placementId)
    {
      
    }
    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        if (placementId == rewardedAdUnitId && showCompletionState == UnityAdsShowCompletionState.COMPLETED)
        {
            // Give player bonus for completing the ad.
            // in game money, power ups, skins etc. 
        }
    }
}