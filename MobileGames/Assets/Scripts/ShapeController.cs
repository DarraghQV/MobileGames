// --- START OF FILE ShapeController.cs ---
using UnityEngine;

public class ShapeController : MonoBehaviour, iTouchable
{
    private Rigidbody _rb;
    private bool _isOnPlatform;
    private Transform _platform;
    private Vector3 _lastPlatformPosition;
    private Renderer _renderer;
    private Color _originalColor;
    private float _dragHeightOffset = 0.5f;
    private float _moveSmoothing = 10f;
    private bool _isSelected = false;

    private Vector2 _previousTouch1Pos;
    private Vector2 _previousTouch2Pos;
    private float _lastPinchDistance;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
        {
            Debug.LogError("ShapeController requires a Rigidbody component!", this);
            enabled = false;
            return;
        }
        _isOnPlatform = false; // Will be set by OnCollisionEnter
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            _originalColor = _renderer.material.color;
        }
        gameObject.tag = "Shape";

        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.isKinematic = false;
    }

    void FixedUpdate()
    {
        if (_rb.isKinematic)
        {
            return; // If selected and kinematic, do nothing here
        }

        // If not kinematic (i.e., not selected)
        if (_isOnPlatform && _platform != null)
        {
            // Move with platform
            Vector3 platformMovement = _platform.position - _lastPlatformPosition;
            _rb.MovePosition(_rb.position + platformMovement);
            _lastPlatformPosition = _platform.position;
        }
        // else: No special falling logic. Gravity will act naturally based on Rigidbody settings.
    }

    public void SelectToggle(bool isSelected)
    {
        _isSelected = isSelected;
        if (_renderer != null)
        {
            _renderer.material.color = isSelected ? Color.cyan : _originalColor;
        }
    }

    public void MoveObject(Touch touch, Camera mainCamera)
    {
        if (!_isSelected || !_rb.isKinematic) return;

        Ray ray = mainCamera.ScreenPointToRay(touch.position);
        Plane dragPlane = new Plane(Vector3.up, transform.position.y - _dragHeightOffset);

        if (dragPlane.Raycast(ray, out float distance))
        {
            Vector3 pointOnPlane = ray.GetPoint(distance);
            Vector3 targetPosition = pointOnPlane + Vector3.up * _dragHeightOffset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * _moveSmoothing);
        }
    }

    public void ScaleObject(Touch touch1, Touch touch2)
    {
        if (!_isSelected || !_rb.isKinematic) return;

        float currentPinchDistance = Vector2.Distance(touch1.position, touch2.position);

        if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
        {
            _lastPinchDistance = currentPinchDistance;
            return;
        }

        if (_lastPinchDistance <= 0)
        {
            _lastPinchDistance = currentPinchDistance;
            return;
        }

        float pinchDelta = currentPinchDistance - _lastPinchDistance;
        float scaleFactorFromPinch = pinchDelta * 0.1f;

        float scaleMultiplier = 1 + Mathf.Clamp(scaleFactorFromPinch * 0.01f, -0.1f, 0.1f);
        Vector3 newScale = transform.localScale * scaleMultiplier;

        newScale = Vector3.Max(newScale, Vector3.one * 0.3f);
        newScale = Vector3.Min(newScale, Vector3.one * 3f);

        transform.localScale = newScale;
        _lastPinchDistance = currentPinchDistance;
    }

    public void RotateObject(Touch touch1, Touch touch2)
    {
        if (!_isSelected || !_rb.isKinematic) return;

        if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
        {
            _previousTouch1Pos = touch1.position;
            _previousTouch2Pos = touch2.position;
            return;
        }

        if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved)
        {
            Vector2 prevVector = _previousTouch2Pos - _previousTouch1Pos;
            Vector2 currentVector = touch2.position - touch1.position;

            float angleDelta = Vector2.SignedAngle(prevVector, currentVector);
            float rotationSpeed = 0.5f;

            transform.Rotate(Vector3.up, angleDelta * rotationSpeed, Space.World);

            _previousTouch1Pos = touch1.position;
            _previousTouch2Pos = touch2.position;
        }
    }

    public void ChangeColor(Color color)
    {
        if (_renderer != null)
        {
            _renderer.material.color = color;
            _originalColor = color;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Platform"))
        {
            _isOnPlatform = true;
            _platform = collision.transform;
            _lastPlatformPosition = _platform.position;
            // REMOVED: _rb.drag = 5f; 
            // REMOVED: _rb.angularDrag = 5f;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Platform"))
        {
            _isOnPlatform = false;
            _platform = null;
            // REMOVED: _rb.drag = 0.05f; 
            // REMOVED: _rb.angularDrag = 0.1f;
        }
    }
}
// --- END OF FILE ShapeController.cs ---