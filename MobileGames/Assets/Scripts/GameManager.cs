using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class GameManager : MonoBehaviour
{
    private iTouchable selectedObject;
    private Transform selectedTransform;
    private Rigidbody selectedRigidbody;
    private Camera mainCamera;
    public LayerMask ignoreLayerMask;

    [Header("Fixed Spawning Area Settings")]
    public Vector3 spawnAreaCenter = new Vector3(0f, 9f, 3f);
    public Vector3 spawnAreaSize = new Vector3(18f, 0f, 0f);
    [Header("Spawning Object Settings")]
    public GameObject[] shapePrefabs;
    public float spawnInterval = 3f;
    public Vector2 sizeRange = new Vector2(0.7f, 1.3f);


    [Header("Platform Settings")]
    public Transform platform; 
    public string platformTag = "Platform";

    [Header("UI Settings")]
    public TMP_Text scoreText;
    public TMP_Text timeText;
    public GameObject gameOverPanel;
    public TMP_Text finalScoreText;
    public TMP_Text finalTimeText;

    [Header("Google Sheet Settings")]
    public string googleAppsScriptURL = "https://script.google.com/macros/s/AKfycbzDG1WFeKH8IIPyHvp8nnwHrxSRjq5IhRbqTpdPKjaS2HC_xxzzLfSVuimgu4BDNbDC/exec";
    public string secretKey = "DQSECRETKEY";

    private bool _isScoreDoubled = false;
    private float _finalGameTime;

    [System.Serializable]
    private class ScoreData
    {
        public string secret;
        public int score;
        public int time;
        public bool wasDoubled;
    }

    private int score = 0;
    private float gameTime = 0f;
    private bool isGameOver = false;
    private int timeBonusCounter = 0;
    private int _finalScore;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null) Debug.LogError("GameManager: Main Camera not found!");
        InvokeRepeating("SpawnShape", 5f, spawnInterval);

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        UpdateUI();
    }

    void Update()
    {
        if (isGameOver) return;

        if (selectedObject != null)
        {
            if (Input.touchCount == 1)
            {
                HandleSingleTouchMovement();
            }
            else if (Input.touchCount >= 2)
            {
                HandleObjectManipulation();
            }
        }

        gameTime += Time.deltaTime;
        int currentTimeBonusInterval = Mathf.FloorToInt(gameTime / 5f);
        if (currentTimeBonusInterval > timeBonusCounter)
        {
            timeBonusCounter = currentTimeBonusInterval;
            AddScore(50);
        }
        UpdateUI();
    }


    void SpawnShape()
    {
        if (isGameOver || shapePrefabs == null || shapePrefabs.Length == 0) return;

        float randomX = Random.Range(
            spawnAreaCenter.x - spawnAreaSize.x / 2f,
            spawnAreaCenter.x + spawnAreaSize.x / 2f
        );

        Vector3 spawnPos = new Vector3(
            randomX,
            spawnAreaCenter.y,
            spawnAreaCenter.z
        );

        GameObject newShape = Instantiate(
            shapePrefabs[Random.Range(0, shapePrefabs.Length)],
            spawnPos,
            Random.rotation
        );
        newShape.transform.localScale *= Random.Range(sizeRange.x, sizeRange.y);
    }

    public void AddScore(int points)
    {
        if (isGameOver) return;
        score += points;
    }

    public void DoubleFinalScore()
    {
        if (!isGameOver || _isScoreDoubled) { Debug.LogWarning("Score cannot be doubled now."); return; }
        if (_finalScore == 0 && score > 0) _finalScore = score;
        else if (_finalScore == 0 && score == 0) { Debug.LogWarning("Cannot double zero score."); return; }

        score = _finalScore * 2;
        _isScoreDoubled = true;
        SendScoreData(score, _finalGameTime, true);
        UpdateFinalScoreText();
    }

    public void UpdateFinalScoreText()
    {
        if (finalScoreText != null)
        {
            finalScoreText.text = $"Final Score: {score}";
            if (_isScoreDoubled) finalScoreText.text += " (x2)";
        }
        if (finalTimeText != null)
        {
            finalTimeText.text = $"Time: {Mathf.FloorToInt(_finalGameTime)}s";
        }
    }

    void UpdateUI()
    {
        if (!isGameOver)
        {
            if (scoreText != null) scoreText.text = $"Score: {score}";
            if (timeText != null) timeText.text = $"Time: {Mathf.FloorToInt(gameTime)}s";
        }
    }

    public void ResetGameState()
    {
        score = 0;
        gameTime = 0f;
        timeBonusCounter = 0;
        isGameOver = false;
        _isScoreDoubled = false;
        _finalScore = 0;
        _finalGameTime = 0f;
        DeselectObject();

        if (platform != null) platform.position = new Vector3(0f, platform.position.y, 0f);

        ClearAllBlocks();

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        UpdateUI();
        Time.timeScale = 1;

        CancelInvoke("SpawnShape");
        InvokeRepeating("SpawnShape", 0f, spawnInterval);
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        _finalScore = score;
        _finalGameTime = gameTime;
        _isScoreDoubled = false;
        DeselectObject();
        UpdateFinalScoreText();
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        Time.timeScale = 0;
        SendScoreData(_finalScore, _finalGameTime, false);
        CancelInvoke("SpawnShape");
    }

    public void RestartWithoutAd()
    {
        ResetGameState();
    }

    public void TrySelectObject(Vector2 tapPosition)
    {
        if (mainCamera == null) return;
        Ray r = mainCamera.ScreenPointToRay(tapPosition);
        RaycastHit info;

        if (Physics.Raycast(r, out info, Mathf.Infinity, ~ignoreLayerMask))
        {
            iTouchable newObject = info.collider.GetComponentInParent<iTouchable>();
            if (newObject != null)
            {
                if (selectedObject == newObject)
                {
                    return;
                }
                DeselectObject();
                selectedObject = newObject;
                selectedTransform = info.collider.GetComponentInParent<Transform>();
                selectedRigidbody = selectedTransform.GetComponent<Rigidbody>();

                if (selectedRigidbody != null)
                {
                    if (!selectedTransform.CompareTag(platformTag))
                    {
                        selectedRigidbody.isKinematic = true;
                    }
                }
                selectedObject.SelectToggle(true);
            }
            else
            {
                DeselectObject();
            }
        }
        else
        {
            DeselectObject();
        }
    }

    public void DeselectObject()
    {
        if (selectedObject != null)
        {
            if (selectedRigidbody != null)
            {
                if (!selectedTransform.CompareTag(platformTag))
                {
                    selectedRigidbody.isKinematic = false;
                }
            }
            selectedObject.SelectToggle(false);
            selectedObject = null;
            selectedTransform = null;
            selectedRigidbody = null;
        }
    }
    public iTouchable GetSelectedObject()
    {
        return selectedObject;
    }

    public void HideGameOverPanel()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    public void ClearAllBlocks()
    {
        GameObject[] blocks = GameObject.FindGameObjectsWithTag("Shape");
        foreach (GameObject block in blocks)
        {
            Destroy(block);
        }
    }

    private void HandleSingleTouchMovement()
    {
        if (selectedObject == null) return;
        Touch touch = Input.GetTouch(0);
        if (touch.phase == TouchPhase.Moved)
        {
            selectedObject.MoveObject(touch, mainCamera);
        }
    }

    private void HandleObjectManipulation()
    {
        if (selectedObject == null || Input.touchCount < 2) return;
        Touch touch1 = Input.GetTouch(0);
        Touch touch2 = Input.GetTouch(1);
        selectedObject.ScaleObject(touch1, touch2);
        selectedObject.RotateObject(touch1, touch2);
    }

    void SendScoreData(int scoreToSend, float timeToSend, bool wasDoubled)
    {
        if (!string.IsNullOrEmpty(googleAppsScriptURL) && !string.IsNullOrEmpty(secretKey))
        {
            StartCoroutine(SendScoreToGoogleSheetCoroutine(scoreToSend, timeToSend, wasDoubled));
        }
    }

    IEnumerator SendScoreToGoogleSheetCoroutine(int finalScore, float finalTime, bool wasDoubled)
    {
        ScoreData dataToSend = new ScoreData { secret = secretKey, score = finalScore, time = Mathf.FloorToInt(finalTime), wasDoubled = wasDoubled };
        string jsonData = JsonUtility.ToJson(dataToSend);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(googleAppsScriptURL, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error Sending Score to Google Sheet: {request.error}");
                if (request.downloadHandler != null) { Debug.LogError($"Response: {request.downloadHandler.text}"); }
            }
        }
    }
}