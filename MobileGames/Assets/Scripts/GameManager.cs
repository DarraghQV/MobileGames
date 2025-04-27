using UnityEngine;
using TMPro;
using UnityEngine.Networking; // Required for web requests
using System.Collections;    // Required for Coroutines
using System.Text;          // Required for JSON encoding

// Make sure iTouchable.cs exists and defines the interface correctly
// public interface iTouchable { ... } // Definition should be in iTouchable.cs

public class GameManager : MonoBehaviour
{
    // --- Original Variables ---
    private iTouchable selectedObject;     // The currently selected object (set by TrySelectObject)
    private Transform selectedTransform; // Transform of the selected object
    private Camera mainCamera;
    public LayerMask ignoreLayerMask;
    private float verticalRotationLimit = 80f;
    private float currentVerticalRotation = 0f;
    private float initialDistance;       // For pinch scaling/rotation
    private Vector3 initialScale;        // For pinch scaling

    [Header("Spawning Settings")]
    public GameObject[] shapePrefabs;    // Prefabs should use ShapeController.cs
    public float spawnInterval = 3f;
    public float spawnHeight = 8f;
    public Vector2 spawnXRange = new Vector2(-2f, 2f);
    public Vector2 sizeRange = new Vector2(0.7f, 1.3f);

    [Header("Platform Settings")]
    public Transform platform;
    public float platformSpeed = 1.5f;
    public float platformRange = 5f;

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

    // --- Helper Class ---
    [System.Serializable]
    private class ScoreData
    {
        public string secret;
        public int score;
        public int time;
        public bool wasDoubled;
    }

    // --- Score Variables ---
    private int score = 0;
    private float gameTime = 0f;
    private bool isGameOver = false;
    private int timeBonusCounter = 0;
    private int _finalScore;


    void Start()
    {
        mainCamera = Camera.main;
        InvokeRepeating("SpawnShape", 0f, spawnInterval);
        gameOverPanel.SetActive(false);
        UpdateUI();
    }

    // --- Updated Update Loop ---
    // Relies on GestureManager for selection/deselection calls
    // Handles manipulation *if* an object is selected
    void Update()
    {
        if (isGameOver) return;

        // --- Manipulation Logic (Only if object is selected) ---
        if (selectedObject != null)
        {
            if (Input.touchCount == 1)
            {
                // Handle single-touch movement of the selected object
                HandleSingleTouchMovement(); // Renamed for clarity
            }
            else if (Input.touchCount >= 2)
            {
                // Handle two-finger scaling and rotation of the selected object
                HandleObjectManipulation();
            }
        }
        // --- Camera/Platform Logic (Only if NO object is selected) ---
        else // selectedObject == null
        {
            if (Input.touchCount >= 2)
            {
                // Rotate camera if 2+ touches and no object selected
                RotateCamera();
            }
            else if (Input.touchCount == 1)
            {
                // Move platform if 1 touch and no object selected
                MovePlatformWithTouch();
            }
        }

        // --- Game Logic ---
        gameTime += Time.deltaTime;
        int currentTimeBonusInterval = Mathf.FloorToInt(gameTime / 5f);
        if (currentTimeBonusInterval > timeBonusCounter)
        {
            timeBonusCounter = currentTimeBonusInterval;
            AddScore(50);
        }
        UpdateUI();
    }
    // --- End Updated Update Loop ---


    void SpawnShape()
    {
        if (isGameOver || shapePrefabs == null || shapePrefabs.Length == 0) return;
        Vector3 spawnPos = new Vector3(
            platform.position.x + Random.Range(spawnXRange.x, spawnXRange.y),
            platform.position.y + spawnHeight,
            platform.position.z
        );
        // Ensure prefabs have ShapeController and Rigidbody
        GameObject newShape = Instantiate(
            shapePrefabs[Random.Range(0, shapePrefabs.Length)], spawnPos, Random.rotation
        );
        newShape.transform.localScale *= Random.Range(sizeRange.x, sizeRange.y);
        AddScore(100);
    }

    void MovePlatformWithTouch() // Called from Update when no object selected
    {
        Touch touch = Input.GetTouch(0);
        float screenMiddle = Screen.width / 2;
        float direction = touch.position.x < screenMiddle ? -1f : 1f;
        float newX = Mathf.Clamp(platform.position.x + direction * platformSpeed * Time.deltaTime, -platformRange, platformRange);
        platform.position = new Vector3(newX, platform.position.y, platform.position.z);
    }

    public void AddScore(int points)
    {
        if (isGameOver) return;
        score += points;
    }

    public void DoubleFinalScore() // Called by Rewarded Ad Button
    {
        if (!isGameOver || _isScoreDoubled) { Debug.LogWarning("Score cannot be doubled now."); return; }
        if (_finalScore == 0) _finalScore = score;
        if (_finalScore == 0 && score == 0) { Debug.LogWarning("Cannot double zero score."); return; }

        score = _finalScore * 2;
        _isScoreDoubled = true; // Set flag

        Debug.Log("Attempting to send doubled score...");
        SendScoreData(score, _finalGameTime, true); // Send doubled score to sheets

        UpdateFinalScoreText(); // Update UI
    }

    public void UpdateFinalScoreText() // Called by DoubleFinalScore
    {
        if (finalScoreText != null)
        {
            finalScoreText.text = $"Final Score: {score}!";
            if (_isScoreDoubled) finalScoreText.text += " (x2)";
        }
    }

    void UpdateUI()
    {
        if (!isGameOver)
        {
            scoreText.text = $"Score: {score}";
            timeText.text = $"Time: {Mathf.FloorToInt(gameTime)}s";
        }
    }

    public void ResetGameState() // Called by Restart button/Interstitial Ad
    {
        score = 0;
        gameTime = 0f;
        timeBonusCounter = 0;
        isGameOver = false;
        _isScoreDoubled = false;
        _finalScore = 0;
        _finalGameTime = 0f;
        DeselectObject(); // Ensure no object is selected

        if (platform != null) platform.position = new Vector3(0f, 2f, 0f);

        // Clear existing shapes BEFORE resetting UI/TimeScale
        ClearAllBlocks();

        gameOverPanel.SetActive(false);
        UpdateUI();
        Time.timeScale = 1;

        CancelInvoke("SpawnShape");
        InvokeRepeating("SpawnShape", 0f, spawnInterval);
    }

    public void GameOver() // Called by NetTrigger
    {
        if (isGameOver) return;
        isGameOver = true;
        _finalScore = score;
        _finalGameTime = gameTime;
        _isScoreDoubled = false;
        DeselectObject(); // Deselect any held object

        finalScoreText.text = $"Final Score: {score}";
        finalTimeText.text = $"Time: {Mathf.FloorToInt(gameTime)}s";
        gameOverPanel.SetActive(true);
        Time.timeScale = 0;

        Debug.Log("Attempting to send original final score...");
        SendScoreData(_finalScore, _finalGameTime, false); // Send ORIGINAL score

        CancelInvoke("SpawnShape");
    }

    public void RestartWithoutAd() // Called by plain Restart button
    {
        ResetGameState();
    }

    // --- Called by GestureManager ---
    public void TrySelectObject(Vector2 tapPosition)
    {
        Ray r = mainCamera.ScreenPointToRay(tapPosition);
        if (Physics.Raycast(r, out RaycastHit info, Mathf.Infinity, ~ignoreLayerMask))
        {
            // IMPORTANT: Use GetComponentInParent or similar if collider is on child
            iTouchable newObject = info.collider.GetComponentInParent<iTouchable>();
            if (newObject != null)
            {
                // If we tapped the same object that might already be selected (e.g., during hold)
                if (selectedObject == newObject)
                {
                    // Optional: If holding, maybe don't deselect/reselect.
                    // If it was a quick tap that selected, GestureManager handles deselect on End.
                    return;
                }

                // Tapped a new touchable object
                DeselectObject(); // Deselect previous if any

                selectedObject = newObject;
                // Get the main transform (parent if collider is child)
                selectedTransform = info.collider.GetComponentInParent<Transform>();
                selectedObject.SelectToggle(true); // Call interface method
                Debug.Log("Selected object: " + selectedTransform.name);
            }
            else
            {
                // Tapped something non-touchable
                DeselectObject();
            }
        }
        else
        {
            // Tapped empty space
            DeselectObject();
        }
    }

    // --- Called by GestureManager or internally ---
    public void DeselectObject()
    {
        if (selectedObject != null)
        {
            Debug.Log("Deselecting object: " + selectedTransform.name);
            selectedObject.SelectToggle(false); // Call interface method
            selectedObject = null;
            selectedTransform = null;
        }
    }
    // --- End Selection/Deselection ---


    public void HideGameOverPanel() // Called by UI button?
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    public void ClearAllBlocks() // Called by ResetGameState
    {
        GameObject[] blocks = GameObject.FindGameObjectsWithTag("Shape");
        foreach (GameObject block in blocks)
        {
            Destroy(block);
        }
    }

    // --- Renamed for clarity - Handles MOVEMENT only ---
    private void HandleSingleTouchMovement()
    {
        if (selectedObject == null) return; // Should not happen if called correctly from Update

        Touch touch = Input.GetTouch(0);

        // Only process movement if touch is moving
        if (touch.phase == TouchPhase.Moved)
        {
            selectedObject.MoveObject(touch, mainCamera); // Delegate to the selected object
        }
    }

    // --- Handles SCALING and ROTATION for the selected object ---
    // Updated to pass values suitable for ShapeController
    private void HandleObjectManipulation()
    {
        if (selectedObject == null || selectedTransform == null || Input.touchCount < 2) return;

        Touch touch1 = Input.GetTouch(0);
        Touch touch2 = Input.GetTouch(1);

        // --- Scaling ---
        if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
        {
            // Store initial distance and scale when second touch begins
            initialDistance = Vector2.Distance(touch1.position, touch2.position);
            initialScale = selectedTransform.localScale;
        }
        else if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
        {
            float currentDistance = Vector2.Distance(touch1.position, touch2.position);
            // Calculate a scale *ratio* instead of the delta/dpi factor
            if (initialDistance > 1f) // Avoid division by zero or tiny values
            {
                float scaleRatio = currentDistance / initialDistance;
                // Let ShapeController handle applying this ratio relative to its *current* scale
                // Note: ShapeController currently expects a delta factor. We might need to adjust
                // EITHER this calculation OR ShapeController.ScaleObject.
                // Let's try passing the *change* in distance relative to screen size as the factor.
                float scaleFactorDelta = (currentDistance - initialDistance) / (Screen.dpi * 2f); // Smaller factor
                selectedObject.ScaleObject(scaleFactorDelta); // Pass delta factor for now
            }


            // --- Rotation ---
            // Use average delta position for rotation input
            // (ShapeController uses AddTorque, so delta position makes sense)
            Vector2 rotationDelta = (touch1.deltaPosition + touch2.deltaPosition) * 0.5f;

            // Add a threshold to avoid jittery rotation during scaling?
            if (rotationDelta.magnitude > 0.5f) // Adjust threshold as needed
            {
                selectedObject.RotateObject(rotationDelta); // Pass average delta
            }
        }
    }
    // --- End HandleObjectManipulation ---

    // --- Rotates CAMERA when no object is selected ---
    private void RotateCamera()
    {
        if (selectedObject != null || Input.touchCount < 2) return;

        Touch touch1 = Input.GetTouch(0); // Use index 0
        Touch touch2 = Input.GetTouch(1); // Use index 1

        // Check if fingers moved significantly
        if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
        {
            // Use average delta for smoother rotation (different from original)
            Vector2 avgDelta = (touch1.deltaPosition + touch2.deltaPosition) * 0.5f;
            float rotationSpeed = 0.15f; // Adjust speed as needed

            // Horizontal (Y-axis) rotation in world space for consistency
            mainCamera.transform.Rotate(Vector3.up, avgDelta.x * rotationSpeed, Space.World);

            // Vertical (X-axis) rotation locally, clamped
            float verticalInput = -avgDelta.y * rotationSpeed; // Invert Y
            currentVerticalRotation = Mathf.Clamp(currentVerticalRotation + verticalInput, -verticalRotationLimit, verticalRotationLimit);

            // Apply rotation, preserving world Y rotation
            mainCamera.transform.localEulerAngles = new Vector3(currentVerticalRotation, mainCamera.transform.localEulerAngles.y, 0);
        }
    }
    // --- End RotateCamera ---


    // --- Google Sheet Integration Methods ---
    void SendScoreData(int scoreToSend, float timeToSend, bool wasDoubled)
    {
        if (!string.IsNullOrEmpty(googleAppsScriptURL) && !string.IsNullOrEmpty(secretKey))
        {
            StartCoroutine(SendScoreToGoogleSheetCoroutine(scoreToSend, timeToSend, wasDoubled));
        }
        else
        {
            Debug.LogWarning($"Google Apps Script URL/Key empty. Score ({scoreToSend}) not sent.");
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

            Debug.Log($"Sending score ({finalScore}, Doubled: {wasDoubled}) to Google Sheet...");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error Sending Score: {request.error}");
                if (request.downloadHandler != null) { Debug.LogError($"Response: {request.downloadHandler.text}"); }
            }
            else
            {
                Debug.Log($"Score Sent Successfully! Response: {request.downloadHandler.text}");
            }
        }
    }
    // --- End Google Sheet Integration Methods ---

    // Interface definition should be in iTouchable.cs, not here.
}