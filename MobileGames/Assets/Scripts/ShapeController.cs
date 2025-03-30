using UnityEngine;

public class ShapeController : MonoBehaviour, iTouchable
{
    private Rigidbody _rb;
    private bool _isOnPlatform;
    private Transform _platform;
    private Vector3 _lastPlatformPosition;
    private Renderer _renderer;
    private Color _originalColor;
    private bool _isBeingDragged = false;
    private float _dragHeightOffset = 0.5f; // Height above touch point
    private float _moveSmoothing = 10f;
    private bool _isSelected = false;


    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _isOnPlatform = false;
        _renderer = GetComponent<Renderer>();
        _originalColor = _renderer.material.color;
        gameObject.tag = "Shape";

        // Configure physics for better dragging
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void FixedUpdate()
    {
        if (_isOnPlatform && _platform != null && !_isBeingDragged)
        {
            Vector3 platformMovement = _platform.position - _lastPlatformPosition;
            transform.position += platformMovement;
            _lastPlatformPosition = _platform.position;
        }
    }

    public void SelectToggle(bool isSelected)
    {
        _isSelected = isSelected;
        _renderer.material.color = isSelected ? Color.cyan : _originalColor;
        
        if (!isSelected)
        {
            // When released, maintain some momentum
            _rb.velocity *= 0.7f;
        }
    }

    public void MoveObject(Touch touch, Camera mainCamera)
    {
        if (!_isSelected) return;

        Ray ray = mainCamera.ScreenPointToRay(touch.position);
        Plane plane = new Plane(Vector3.up, transform.position.y - _dragHeightOffset);

        if (plane.Raycast(ray, out float distance))
        {
            Vector3 targetPosition = ray.GetPoint(distance);
            Vector3 direction = (targetPosition - transform.position);
            
            // Smooth movement with acceleration
            float speed = Mathf.Min(direction.magnitude * 5f, 10f);
            _rb.velocity = direction.normalized * speed;
        }
    }

    public void ScaleObject(float scaleFactor)
    {
        float scaleMultiplier = 1 + Mathf.Clamp(scaleFactor * 0.01f, -0.1f, 0.1f);
        Vector3 newScale = transform.localScale * scaleMultiplier;

        // Limit minimum and maximum size
        newScale = Vector3.Max(newScale, Vector3.one * 0.3f);
        newScale = Vector3.Min(newScale, Vector3.one * 3f);

        transform.localScale = newScale;
        _rb.mass = newScale.x; // Mass scales with size
    }

    public void RotateObject(Vector2 rotationDelta)
    {
        float rotationSpeed = 1f;
        _rb.AddTorque(Vector3.up * rotationDelta.x * rotationSpeed, ForceMode.VelocityChange);
        _rb.AddTorque(Vector3.right * rotationDelta.y * rotationSpeed, ForceMode.VelocityChange);
    }

    public void ChangeColor(Color color)
    {
        _renderer.material.color = color;
        _originalColor = color;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Platform"))
        {
            _isOnPlatform = true;
            _platform = collision.transform;
            _lastPlatformPosition = _platform.position;
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
            _rb.drag = 0.5f;
            _rb.angularDrag = 0.5f;
        }
    }
}