using UnityEngine;

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
    public GameObject[] shapePrefabs; // Assign cube prefabs in Inspector
    public float spawnInterval = 2f;
    public float spawnHeight = 8f;
    public Vector2 spawnXRange = new Vector2(-2f, 2f);
    public Vector2 sizeRange = new Vector2(0.7f, 1.3f);

    [Header("Platform Settings")]
    public Transform platform; // Assign platform transform
    public float platformSpeed = 1.5f;
    public float platformRange = 5f;

    void Start()
    {
        mainCamera = Camera.main;
        InvokeRepeating("SpawnShape", 0f, spawnInterval); // Start spawning
    }

    void Update()
    {
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

        // New platform movement
        MovePlatform();
    }

    // New spawning method
    void SpawnShape()
    {
        if (shapePrefabs == null || shapePrefabs.Length == 0)
        {
            Debug.LogError("No shape prefabs assigned in GameManager!");
            return;
        }

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
    }

    // New platform movement
    void MovePlatform()
    {
        float pingPong = Mathf.PingPong(Time.time * platformSpeed, platformRange * 2) - platformRange;
        platform.position = new Vector3(
            pingPong,
            platform.position.y,
            platform.position.z
        );
    }

    // ALL ORIGINAL TOUCH METHODS REMAIN UNCHANGED BELOW
    private void HandleSingleTouch()
    {
        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            initialTouchPosition = touch.position;
            TrySelectObject(touch.position);
        }
        else if (touch.phase == TouchPhase.Moved && selectedObject != null)
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
        RaycastHit info;
        if (Physics.Raycast(r, out info, Mathf.Infinity, ~ignoreLayerMask))
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