// --- Full FILE GameManager.cs ---

using UnityEngine;
using TMPro;
using UnityEngine.Networking; // Required for web requests
using System.Collections;    // Required for Coroutines
using System.Text;          // Required for JSON encoding

public class GameManager : MonoBehaviour
{
    // --- Variables ---
    private iTouchable selectedObject;     // The currently selected object (set by TrySelectObject)
    private Transform selectedTransform; // Transform of the selected object
    private Camera mainCamera;
    public LayerMask ignoreLayerMask; // Make sure this is set correctly in the Inspector!
    private float initialDistance;       // For pinch scaling/rotation
    private Vector3 initialScale;        // For pinch scaling

    [Header("Spawning Settings")]
    public GameObject[] shapePrefabs;    // Prefabs should use ShapeController.cs
    public float spawnInterval = 3f;
    public float spawnHeight = 8f;
    public Vector2 spawnXRange = new Vector2(-2f, 2f);
    public Vector2 sizeRange = new Vector2(0.7f, 1.3f);

    [Header("Platform Settings")]
    public Transform platform; // Reference needed for spawning position

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
        if (mainCamera == null) Debug.LogError("GameManager: Main Camera not found!");

        InvokeRepeating("SpawnShape", 0f, spawnInterval);
        gameOverPanel.SetActive(false);
        UpdateUI();
    }

    // --- Update Loop ---
    void Update()
    {
        if (isGameOver) return;

        // --- Manipulation Logic (Only if object is selected) ---
        if (selectedObject != null)
        {
            if (Input.touchCount == 1)
            {
                // Handle single-touch movement of the selected object
                HandleSingleTouchMovement();
            }
            else if (Input.touchCount >= 2)
            {
                // Handle two-finger scaling and rotation of the selected object
                HandleObjectManipulation();
            }
        }
        // --- Camera/Platform Logic is Handled by CameraController ---

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
    // --- End Update Loop ---


    void SpawnShape()
    {
        if (isGameOver || shapePrefabs == null || shapePrefabs.Length == 0) return;
        if (platform == null)
        {
            Debug.LogError("GameManager: Platform reference is missing, cannot determine spawn position!");
            return;
        }
        Vector3 spawnPos = new Vector3(
            platform.position.x + Random.Range(spawnXRange.x, spawnXRange.y),
            platform.position.y + spawnHeight,
            platform.position.z // Assuming Z is consistent with platform
        );
        // Ensure prefabs have ShapeController and Rigidbody
        GameObject newShape = Instantiate(
            shapePrefabs[Random.Range(0, shapePrefabs.Length)], spawnPos, Random.rotation
        );
        newShape.transform.localScale *= Random.Range(sizeRange.x, sizeRange.y);
        // AddScore(100); // Scoring moved to collision/goal? Keep if intended on spawn.
    }

    public void AddScore(int points)
    {
        if (isGameOver) return;
        score += points;
        // UpdateUI(); // Called in main Update loop
    }

    public void DoubleFinalScore() // Called by Rewarded Ad Button
    {
        if (!isGameOver || _isScoreDoubled) { Debug.LogWarning("Score cannot be doubled now."); return; }
        if (_finalScore == 0) _finalScore = score; // Capture score if not already captured
        if (_finalScore == 0 && score == 0) { Debug.LogWarning("Cannot double zero score."); return; }

        score = _finalScore * 2;
        _isScoreDoubled = true; // Set flag

        Debug.Log("Attempting to send doubled score...");
        SendScoreData(score, _finalGameTime, true); // Send doubled score to sheets

        UpdateFinalScoreText(); // Update UI
    }

    public void UpdateFinalScoreText() // Called by DoubleFinalScore & GameOver
    {
        if (finalScoreText != null)
        {
            finalScoreText.text = $"Final Score: {score}"; // Use current score (which might be doubled)
            if (_isScoreDoubled) finalScoreText.text += " (x2)";
        }
        if (finalTimeText != null) // Also update time here
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

    public void ResetGameState() // Called by Restart button/Interstitial Ad
    {
        Debug.Log("Resetting Game State...");
        score = 0;
        gameTime = 0f;
        timeBonusCounter = 0;
        isGameOver = false;
        _isScoreDoubled = false;
        _finalScore = 0;
        _finalGameTime = 0f;
        DeselectObject(); // Ensure no object is selected

        if (platform != null) platform.position = new Vector3(0f, platform.position.y, 0f); // Keep original Y? Adjust if needed

        // Clear existing shapes BEFORE resetting UI/TimeScale
        ClearAllBlocks();

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        UpdateUI();
        Time.timeScale = 1;

        CancelInvoke("SpawnShape"); // Stop old spawner
        InvokeRepeating("SpawnShape", 0f, spawnInterval); // Start new spawner
    }

    public void GameOver() // Called by NetTrigger
    {
        if (isGameOver) return;
        Debug.Log("GAME OVER triggered!");
        isGameOver = true;
        _finalScore = score;        // Store the score *before* potential doubling
        _finalGameTime = gameTime;
        _isScoreDoubled = false;    // Reset double flag for the next game end screen
        DeselectObject();           // Deselect any held object

        UpdateFinalScoreText();     // Update text with final score/time

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        Time.timeScale = 0; // Pause game

        Debug.Log("Attempting to send original final score...");
        SendScoreData(_finalScore, _finalGameTime, false); // Send ORIGINAL score

        CancelInvoke("SpawnShape"); // Stop spawning
    }

    public void RestartWithoutAd() // Called by plain Restart button
    {
        ResetGameState();
    }

    // --- Called by GestureManager ---
    public void TrySelectObject(Vector2 tapPosition)
    {
        if (mainCamera == null) return; // Safety check

        Ray r = mainCamera.ScreenPointToRay(tapPosition);
        // Draw the ray in the Scene view for visual debugging (lasts 2 seconds)
        Debug.DrawRay(r.origin, r.direction * 100f, Color.yellow, 2.0f);
        // Log the attempt, including the mask value being used (inverted from Inspector setting)
        Debug.Log($"[GameManager.TrySelectObject] Attempting Raycast from {r.origin} dir {r.direction} | LayerMask Used (inverted): {LayerMask.LayerToName(~ignoreLayerMask)} ({(~ignoreLayerMask)}) | Ignoring Mask (Inspector): {LayerMask.LayerToName(ignoreLayerMask)} ({ignoreLayerMask})");

        // Perform the raycast using the INVERTED mask (~ignoreLayerMask)
        if (Physics.Raycast(r, out RaycastHit info, Mathf.Infinity, ~ignoreLayerMask))
        {
            // Raycast Hit Something!
            Debug.Log($"[GameManager.TrySelectObject] Raycast HIT: {info.collider.gameObject.name} on Layer: {LayerMask.LayerToName(info.collider.gameObject.layer)}");

            // Check if the hit object has the iTouchable component (or in parent)
            // Important: Use GetComponentInParent if collider might be on a child object
            iTouchable newObject = info.collider.GetComponentInParent<iTouchable>();
            if (newObject != null)
            {
                // Found a touchable object
                Debug.Log($"[GameManager.TrySelectObject] Hit object {info.collider.gameObject.name} IS iTouchable.");

                if (selectedObject == newObject)
                {
                    // If we tapped the same object that might already be selected (e.g., during hold)
                    Debug.Log("[GameManager.TrySelectObject] Tapped the same object already selected.");
                    return; // Already selected, do nothing further in this method
                }

                // Tapped a new touchable object
                DeselectObject(); // Deselect previous if any

                selectedObject = newObject;
                // Get the main transform (parent if collider is child)
                selectedTransform = info.collider.GetComponentInParent<Transform>(); // Robust way to get the transform holding the script
                selectedObject.SelectToggle(true); // Call interface method
                Debug.Log($"[GameManager.TrySelectObject] SUCCESS: Selected object: {selectedTransform.name}");
            }
            else
            {
                // Tapped something non-touchable that was hit by the raycast
                Debug.LogWarning($"[GameManager.TrySelectObject] Raycast hit {info.collider.gameObject.name}, but it has NO iTouchable interface (or in parent). Deselecting.");
                DeselectObject(); // Deselect any previously selected object
            }
        }
        else
        {
            // Raycast Didn't Hit Anything (that wasn't on the ignored layer mask)
            Debug.Log("[GameManager.TrySelectObject] Raycast MISSED (didn't hit any non-ignored layer). Deselecting previous object if any.");
            DeselectObject(); // Deselect any previously selected object
        }
    }

    // --- Called by GestureManager or internally ---
    public void DeselectObject()
    {
        if (selectedObject != null)
        {
            // Log before nulling out references
            Debug.Log($"[GameManager.DeselectObject] Deselecting: {selectedTransform?.name ?? "Previously Selected Object"}");
            selectedObject.SelectToggle(false); // Call interface method
            selectedObject = null;
            selectedTransform = null;
        }
        // else { Debug.Log("[GameManager.DeselectObject] Called, but no object was selected."); } // Optional: Log even if nothing was selected
    }
    // --- End Selection/Deselection ---

    // --- Getter for CameraController ---
    public iTouchable GetSelectedObject()
    {
        return selectedObject;
    }


    public void HideGameOverPanel() // Called by UI button?
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    public void ClearAllBlocks() // Called by ResetGameState
    {
        Debug.Log("Clearing all blocks with tag 'Shape'...");
        GameObject[] blocks = GameObject.FindGameObjectsWithTag("Shape");
        int count = 0;
        foreach (GameObject block in blocks)
        {
            Destroy(block);
            count++;
        }
        Debug.Log($"Destroyed {count} blocks.");
    }

    // --- Handles MOVEMENT only ---
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

    // --- In FILE GameManager.cs ---

    // --- Handles SCALING and ROTATION for the selected object ---
    private void HandleObjectManipulation()
    {
        if (selectedObject == null || Input.touchCount < 2) return; // Simplified check

        Touch touch1 = Input.GetTouch(0);
        Touch touch2 = Input.GetTouch(1);

        // Call the interface methods directly, passing the touches.
        // The specific implementation in TouchableObject (or potentially ShapeController if used)
        // will handle the logic based on touch phases internally.

        // --- Scaling ---
        // Delegate scaling logic entirely to the selected object's implementation
        selectedObject.ScaleObject(touch1, touch2);

        // --- Rotation ---
        // Delegate rotation logic entirely to the selected object's implementation
        selectedObject.RotateObject(touch1, touch2);

        // We no longer need initialDistance/initialScale calculations here in GameManager
        // as the object itself handles its state (like lastPinchDistance in TouchableObject).
    }
    // --- End HandleObjectManipulation ---


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

            Debug.Log($"Sending score ({finalScore}, Time: {Mathf.FloorToInt(finalTime)}, Doubled: {wasDoubled}) to Google Sheet...");
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

}