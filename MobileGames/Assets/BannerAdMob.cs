using UnityEngine;
using UnityEngine.UI;
using GoogleMobileAds.Api;
using System.Linq;

[DisallowMultipleComponent] // Prevents duplicate scripts
public class BannerAdManager : MonoBehaviour
{
    [Header("UI Controls")]
    [SerializeField] Button _showButton;
    [SerializeField] Button _destroyButton;

    [Header("Debug")]
    [SerializeField] bool _verboseLogging = true;

    [Header("Ad Settings")]
    [SerializeField] AdPosition _bannerPosition = AdPosition.Bottom;

#if UNITY_ANDROID
    private string _adUnitId = "ca-app-pub-9816783354168039~5042949826";
#elif UNITY_IPHONE
    private string _adUnitId = "ca-app-pub-3940256099942544/2934735716";
#else
    private string _adUnitId = "ca-app-pub-9816783354168039/5022768528";
#endif

    private BannerView _currentBanner;
    private static BannerAdManager _instance;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            if (_verboseLogging) Debug.LogWarning("Destroying duplicate BannerAdManager");
            Destroy(this);
            return;
        }
        _instance = this;
    }

    void Start()
    {
        if (_verboseLogging) Debug.Log("Initializing BannerAdManager");

        NuclearCleanup();

        _showButton.onClick.AddListener(CreateSingleBanner);
        _destroyButton.onClick.AddListener(NuclearCleanup);

        _showButton.interactable = true;
        _destroyButton.interactable = false;

        MobileAds.Initialize(initStatus => {
            if (_verboseLogging) Debug.Log("AdMob initialized (no banners created)");
        });
    }

   public void CreateSingleBanner()
    {
        if (_verboseLogging) Debug.Log("Creating single banner");

        NuclearCleanup();

        _currentBanner = new BannerView(_adUnitId, AdSize.Banner, _bannerPosition);

        _currentBanner.OnBannerAdLoaded += () =>
        {
            if (_verboseLogging) Debug.Log("Banner loaded successfully");
            _currentBanner.Show();
            _destroyButton.interactable = true;
        };

        _currentBanner.OnBannerAdLoadFailed += (error) =>
        {
            if (_verboseLogging) Debug.LogError($"Banner failed: {error.GetMessage()}");
            _currentBanner = null;
            _showButton.interactable = true;
        };

        _currentBanner.LoadAd(new AdRequest());
        _showButton.interactable = false;
    }

    public void NuclearCleanup()
    {
        if (_verboseLogging) Debug.Log("Initiating nuclear cleanup");

        if (_currentBanner != null)
        {
            if (_verboseLogging) Debug.Log("Destroying AdMob banner instance");
            _currentBanner.Destroy();
            _currentBanner = null;
        }

        var allObjects = FindObjectsOfType<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj != null && obj.name.Contains("BANNER"))
            {
                if (_verboseLogging) Debug.Log($"Destroying banner object: {obj.name}");
                if (Application.isPlaying)
                {
                    Destroy(obj);
                }
                else
                {
                    DestroyImmediate(obj);
                }
            }
        }

        var newObjects = FindObjectsOfType<GameObject>()
                       .Where(go => go.name.Contains("New Game Object"));

        foreach (var obj in newObjects)
        {
            if (obj.GetComponents<Component>().Length <= 1) // Empty object
            {
                if (_verboseLogging) Debug.Log($"Destroying empty object: {obj.name}");
                if (Application.isPlaying)
                {
                    Destroy(obj);
                }
                else
                {
                    DestroyImmediate(obj);
                }
            }
        }

        _showButton.interactable = true;
        _destroyButton.interactable = false;

        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }

    void OnDestroy()
    {
        NuclearCleanup();
    }

    public static void ForceCleanup()
    {
        if (_instance != null)
        {
            _instance.NuclearCleanup();
        }
        else
        {
            var banners = FindObjectsOfType<GameObject>()
                        .Where(go => go.name.Contains("BANNER"));

            foreach (var banner in banners)
            {
                DestroyImmediate(banner);
            }
        }
    }
}