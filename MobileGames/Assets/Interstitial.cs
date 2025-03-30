using UnityEngine;
using UnityEngine.Advertisements;

public class Interstitial : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [SerializeField] string _androidAdUnitId = "Interstitial_Android";
    [SerializeField] string _iOSAdUnitId = "Interstitial_iOS";
    string _adUnitId;

    private GameManager _gameManager;

    void Awake()
    {
#if UNITY_IOS
        _adUnitId = _iOSAdUnitId;
#elif UNITY_ANDROID
        _adUnitId = _androidAdUnitId;
#endif

        _gameManager = FindObjectOfType<GameManager>();
    }

    // Call this from UI button
    public void ShowAdThenRestart()
    {
        if (Advertisement.isInitialized)
        {
            Advertisement.Load(_adUnitId, this);
        }
        else
        {
            DirectRestart();
        }
    }

    private void DirectRestart()
    {
        // 1. Clear all blocks
        GameObject[] blocks = GameObject.FindGameObjectsWithTag("Shape");
        foreach (GameObject block in blocks)
        {
            Destroy(block);
        }

        // 2. Reset game state through GameManager
        if (_gameManager != null)
        {
            _gameManager.ResetGameState();
            _gameManager.gameOverPanel.SetActive(false);
            Time.timeScale = 1;

            // This will automatically restart spawning through GameManager's existing InvokeRepeating
        }
    }

    // Ad callback handlers
    public void OnUnityAdsAdLoaded(string adUnitId) => Advertisement.Show(adUnitId, this);
    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState _) => DirectRestart();
    public void OnUnityAdsFailedToLoad(string _, UnityAdsLoadError __, string ___) => DirectRestart();
    public void OnUnityAdsShowFailure(string _, UnityAdsShowError __, string ___) => DirectRestart();
    public void OnUnityAdsShowStart(string _) { }
    public void OnUnityAdsShowClick(string _) { }
}