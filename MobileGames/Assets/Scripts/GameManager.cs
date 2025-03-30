using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    // Original touch interaction variables
    private iTouchable selectedObject;
    private Vector2 initialTouchPosition;
    private Transform selectedTransform;
    private Camera mainCamera;
    public LayerMask ignoreLayerMask;
    private float verticalRotationLimit = 80f;
    private float currentVerticalRotation = 0f;
    private float initialDistance;
    private Vector3 initialScale;

    // New spawning variables
    [Header("Spawning Settings")]
    public GameObject[] shapePrefabs;
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

    // Score variables
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

    void Update()
    {
        if (isGameOver) return;

        // Original touch handling
        if (Input.touchCount == 1)
        {
            HandleSingleTouch();
        }
        else if (Input.touchCount == 2)
        {
            if (selectedObject != null)
            {
                HandleObjectManipulation();
            }
            else
            {
                RotateCamera();
            }
        }

        // New platform movement - controlled by player touch
        if (Input.touchCount > 0 && selectedObject == null)
        {
            MovePlatformWithTouch();
        }

        // Update game time and score
        gameTime += Time.deltaTime;

        // Add time bonus every 5 seconds
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

        Vector3 spawnPos = new Vector3(
            platform.position.x + Random.Range(spawnXRange.x, spawnXRange.y),
            platform.position.y + spawnHeight,
            platform.position.z
        );

        GameObject newShape = Instantiate(
            shapePrefabs[Random.Range(0, shapePrefabs.Length)],
            spawnPos,
            Random.rotation
        );

        newShape.transform.localScale *= Random.Range(sizeRange.x, sizeRange.y);
        AddScore(100);
    }

    void MovePlatformWithTouch()
    {
        Touch touch = Input.GetTouch(0);
        float screenMiddle = Screen.width / 2;
        float direction = touch.position.x < screenMiddle ? -1f : 1f;

        float newX = platform.position.x + direction * platformSpeed * Time.deltaTime;
        newX = Mathf.Clamp(newX, -platformRange, platformRange);

        platform.position = new Vector3(
            newX,
            platform.position.y,
            platform.position.z
        );
    }

    public void AddScore(int points)
    {
        score += points;
        UpdateUI();
    }

    public void DoubleFinalScore()
    {
        // Store the original score first if we haven't already
        if (_finalScore == 0)
        {
            _finalScore = score;
        }

        // Double the score
        score = _finalScore * 2;
    }

    public void UpdateFinalScoreText()
    {
        if (finalScoreText != null)
        {
            finalScoreText.text = $"Final Score: {score}!";
        }
    }


    void UpdateUI()
    {
        scoreText.text = $"Score: {score}";
        timeText.text = $"Time: {Mathf.FloorToInt(gameTime)}s";
    }

    // Just add this one method to GameManager
    public void ResetGameState()
    {
        score = 0;
        gameTime = 0f;
        timeBonusCounter = 0;
        isGameOver = false;
        if (platform != null)
        {
            platform.position = new Vector3(0f, 2f, 0f);
        }

        UpdateUI();
    }

    // Keep original GameOver() but remove any ad triggering
    public void GameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        _finalScore = score; // Store the original score
        finalScoreText.text = $"Final Score: {score}";
        finalTimeText.text = $"Time: {Mathf.FloorToInt(gameTime)}s";
        gameOverPanel.SetActive(true);
        Time.timeScale = 0;
    }

    // Add this new method for direct restart (without ad)
    public void RestartWithoutAd()
    {
        ResetGame();
        gameOverPanel.SetActive(false);
        Time.timeScale = 1;
    }

    public void DeselectObject()
    {
        if (selectedObject != null)
        {
            selectedObject.SelectToggle(false);
            selectedObject = null;
            selectedTransform = null;
        }
    }

    public void HideGameOverPanel()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    public void ClearAllBlocks()
    {
        GameObject[] blocks = GameObject.FindGameObjectsWithTag("Shape"); // Make sure your blocks have the "Block" tag
        foreach (GameObject block in blocks)
        {
            Destroy(block);
        }
    }

    public void ResetGame()
    {
        score = 0;
        gameTime = 0f;
        timeBonusCounter = 0;
        isGameOver = false;
        UpdateUI();

        // Restart spawning using existing InvokeRepeating
        CancelInvoke("SpawnShape");
        InvokeRepeating("SpawnShape", 0f, spawnInterval);
    }

    private void HandleSingleTouch()
    {
        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Moved && selectedObject != null)
        {
            selectedObject.MoveObject(touch, mainCamera);
        }
    }

    private void HandleObjectManipulation()
    {
        Touch touch1 = Input.GetTouch(0);
        Touch touch2 = Input.GetTouch(1);

        if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
        {
            initialDistance = Vector2.Distance(touch1.position, touch2.position);
            initialScale = selectedTransform.localScale;
        }
        else if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
        {
            float currentDistance = Vector2.Distance(touch1.position, touch2.position);
            float scaleFactor = (currentDistance - initialDistance) / Screen.dpi;
            selectedObject.ScaleObject(scaleFactor);

            if (Vector2.Distance(touch1.deltaPosition, touch2.deltaPosition) > 5f)
            {
                Vector2 rotationDelta = (touch1.deltaPosition + touch2.deltaPosition) * 0.5f;
                selectedObject.RotateObject(rotationDelta);
            }
        }
    }

    public void TrySelectObject(Vector2 tapPosition)
    {
        Ray r = mainCamera.ScreenPointToRay(tapPosition);
        if (Physics.Raycast(r, out RaycastHit info, Mathf.Infinity, ~ignoreLayerMask))
        {
            iTouchable newObject = info.collider.GetComponent<iTouchable>();
            if (newObject != null)
            {
                if (selectedObject == newObject)
                {
                    selectedObject.SelectToggle(false);
                    selectedObject = null;
                    selectedTransform = null;
                }
                else
                {
                    if (selectedObject != null)
                    {
                        selectedObject.SelectToggle(false);
                    }
                    selectedObject = newObject;
                    selectedTransform = info.collider.transform;
                    newObject.SelectToggle(true);
                }
            }
        }
    }

    private void RotateCamera()
    {
        Touch touch = Input.GetTouch(0);
        Touch touch2 = Input.GetTouch(1);

        if (touch.phase == TouchPhase.Moved && touch2.phase == TouchPhase.Moved)
        {
            float rotationSpeed = 0.2f;
            Vector2 delta = touch.deltaPosition - touch2.deltaPosition;

            float horizontal = delta.x * rotationSpeed;
            float vertical = -delta.y * rotationSpeed;

            currentVerticalRotation += vertical;
            currentVerticalRotation = Mathf.Clamp(currentVerticalRotation, -verticalRotationLimit, verticalRotationLimit);

            mainCamera.transform.Rotate(Vector3.up, horizontal, Space.Self);
            mainCamera.transform.localRotation = Quaternion.Euler(currentVerticalRotation,
                mainCamera.transform.localRotation.eulerAngles.y, 0);
        }
    }
}