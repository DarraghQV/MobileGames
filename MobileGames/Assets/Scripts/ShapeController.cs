using UnityEngine;

public class ShapeController : MonoBehaviour, iTouchable
{
    private Rigidbody _rb;
    private bool _isOnPlatform;
    private Transform _platform;
    private Vector3 _lastPlatformPosition;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _isOnPlatform = false;
    }

    void FixedUpdate()
    {
        if (_isOnPlatform && _platform != null)
        {
            // Calculate platform movement delta
            Vector3 platformMovement = _platform.position - _lastPlatformPosition;

            // Move the cube with the platform
            transform.position += platformMovement;

            // Update last known platform position
            _lastPlatformPosition = _platform.position;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Platform"))
        {
            _isOnPlatform = true;
            _platform = collision.transform;
            _lastPlatformPosition = _platform.position;

            // Optional: Reduce physics effects
            _rb.drag = 5f;
            _rb.angularDrag = 5f;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Platform"))
        {
            _isOnPlatform = false;
            _platform = null;

            // Restore default physics
            _rb.drag = 0.5f;
            _rb.angularDrag = 0.5f;
        }
    }

    // Required interface implementation
    public void ChangeColor(Color color)
    {
        if (TryGetComponent<Renderer>(out var renderer))
        {
            renderer.material.color = color;
        }
    }

    // Your existing iTouchable implementation...
    public void SelectToggle(bool selected) { /* ... */ }
    public void MoveObject(Touch touch, Camera cam) { /* ... */ }
    public void ScaleObject(float scaleFactor) { /* ... */ }
    public void RotateObject(Vector2 rotationDelta) { /* ... */ }
}