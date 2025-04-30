using UnityEngine;
public class CameraController : MonoBehaviour
{


    public float panSpeed = 20f;
    public float rotationSpeed = 0.2f;
    public float zoomSpeed = 1f;
    public float gyroSensitivity = 2.0f;

    private GameManager gameManager; 
    private Vector3 lastPanPosition;
    private Vector2 lastMidpoint;
    private float lastPinchDistance;
    private bool isPanning = false;

    private bool useGyro = false;

    void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 45;
    }

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("CameraController requires a GameManager in the scene!");
        }
    }

    void Update()
    {
        if (useGyro)
        {
            ApplyGyroRotation();
        }

        // Check selection status via GameManager
        bool objectIsSelected = gameManager != null && gameManager.GetSelectedObject() != null;

        if (Input.touchCount == 1 && !objectIsSelected) 
        {
            Touch t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Began)
            {
                lastPanPosition = t.position;
                isPanning = true;

            }
            else if (t.phase == TouchPhase.Moved && isPanning)
            {
                PanCamera(t.position);
            }
            else if (t.phase == TouchPhase.Ended)
            {
                isPanning = false;
            }
        }
        else if (Input.touchCount >= 2 && !objectIsSelected) 
        {
            isPanning = false; 

            Touch t1 = Input.GetTouch(0);
            Touch t2 = Input.GetTouch(1);

            Vector2 currentMidpoint = (t1.position + t2.position) / 2;

            if (t1.phase == TouchPhase.Began || t2.phase == TouchPhase.Began)
            {
                lastMidpoint = currentMidpoint;
                lastPinchDistance = Vector2.Distance(t1.position, t2.position);
            }
            else if (t1.phase == TouchPhase.Moved || t2.phase == TouchPhase.Moved)
            {
                Vector2 delta = currentMidpoint - lastMidpoint;
                if (delta.magnitude > 0.1f) 
                {
                    float horizontalAngle = delta.x * rotationSpeed;
                    OrbitCameraHorizontal(horizontalAngle);

                    float verticalAngle = -delta.y * rotationSpeed;
                    OrbitCameraVertical(verticalAngle);
                }
                lastMidpoint = currentMidpoint;


                float currentPinchDistance = Vector2.Distance(t1.position, t2.position);
                float pinchDelta = currentPinchDistance - lastPinchDistance;

                if (Mathf.Abs(pinchDelta) > 1f) 
                {
                    ZoomCamera(pinchDelta);
                }

                lastPinchDistance = currentPinchDistance;
            }
        }
        else if (Input.touchCount < 2) 
        {
            if (Input.touchCount == 0) isPanning = false;
        }
    }

    void ApplyGyroRotation()
    {
        if (!SystemInfo.supportsGyroscope) return;

        if (!Input.gyro.enabled) Input.gyro.enabled = true;

        Vector3 gyroDelta = Input.gyro.rotationRateUnbiased;

        transform.Rotate(Vector3.up, -gyroDelta.y * gyroSensitivity, Space.World);
        transform.Rotate(Vector3.right, -gyroDelta.x * gyroSensitivity, Space.Self);
    }

    // Called from UI Button
    public void ToggleGyro()
    {
        useGyro = !useGyro;
        if (useGyro && SystemInfo.supportsGyroscope)
        {
            EnableGyro();
        }
        else if (!useGyro && Input.gyro.enabled)
        {
            Input.gyro.enabled = false;
        }
    }

    private void EnableGyro()
    {
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            useGyro = true; 
        }
        else
        {
            Debug.LogWarning("Gyroscope not supported on this device.");
            useGyro = false; 
        }
    }

    void PanCamera(Vector3 newPanPosition)
    {
        Vector3 offset = Camera.main.ScreenToViewportPoint(lastPanPosition - newPanPosition);

        Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        Vector3 move = (right * -offset.x + forward * -offset.y) * panSpeed; // Invert offset directions

        transform.Translate(move, Space.World);

        lastPanPosition = newPanPosition;
    }

    void OrbitCameraHorizontal(float angle)
    {
        transform.RotateAround(transform.position, Vector3.up, angle);
    }

    void OrbitCameraVertical(float angle)
    {
        float currentXAngle = transform.eulerAngles.x;
        float futureAngle = currentXAngle + angle;

        if (futureAngle > 85f && futureAngle < 275f) 
        {
            if (angle > 0 && currentXAngle < 180f) angle = 85f - currentXAngle; 
            else if (angle < 0 && currentXAngle > 180f) angle = 275f - currentXAngle; 
            else angle = 0; 
        }


        if (Mathf.Abs(angle) > 0.01f) // Apply only if angle is significant
        {
            transform.RotateAround(transform.position, transform.right, angle);
        }
    }

    void ZoomCamera(float pinchDelta)
    {
        Vector3 zoomDirection = transform.forward * (pinchDelta * zoomSpeed * 0.01f); 
        transform.position += zoomDirection;

    }
}