using UnityEngine;

// Add ", iTouchable" after MonoBehaviour
public abstract class TouchableObject : MonoBehaviour, iTouchable
{
    protected Renderer r;
    protected float rotationSpeed = 0.5f;
    protected float scaleSpeed = 0.001f; // Updated scaleSpeed
    protected float lastPinchDistance; // Added for new ScaleObject method

    protected virtual void Start()
    {
        r = GetComponent<Renderer>();
    }

    // These methods now correctly fulfill the iTouchable interface requirements

    public virtual void SelectToggle(bool selected)
    {
        ChangeColor(selected ? Color.cyan : Color.white);
    }

    public virtual void ChangeColor(Color color)
    {
        if (r == null) r = GetComponent<Renderer>(); // Add safety check
        if (r != null && r.material != null) // Check material too
        {
            r.material.color = color;
        }
        else
        {
            Debug.LogError($"{gameObject.name} - Renderer or Material not found in ChangeColor", this);
        }
    }

    public abstract void MoveObject(Touch touch, Camera mainCamera);


    // --- NOTE ---
    // GameManager currently calls ScaleObject(float) and RotateObject(Vector2)
    // which are NOT defined in this base class or iTouchable.
    // If you intend to use THIS hierarchy, GameManager.HandleObjectManipulation
    // needs to be changed to call the RotateObject/ScaleObject methods below,
    // OR you need to add the float/Vector2 versions back into iTouchable and here.

    // Added ScaleObject(float) to satisfy current GameManager calls if needed
    // This implementation might differ from ShapeController's intent


    // --- These methods use Touch input directly ---
    // --- Not currently called by GameManager ---
    public virtual void RotateObject(Touch t1, Touch t2)
    {
        // Ensure Camera.main is valid
        if (Camera.main == null) return;

        Vector3 worldPos1 = Camera.main.ScreenToWorldPoint(new Vector3(t1.position.x, t1.position.y, Camera.main.nearClipPlane + 1f)); // Add small offset
        Vector3 worldPos2 = Camera.main.ScreenToWorldPoint(new Vector3(t2.position.x, t2.position.y, Camera.main.nearClipPlane + 1f)); // Add small offset


        Vector3 direction = worldPos2 - worldPos1;

        // Project direction onto camera's XY plane for rotation around Z (forward)
        Vector3 localDirection = Camera.main.transform.InverseTransformDirection(direction);
        float angle = Mathf.Atan2(localDirection.y, localDirection.x) * Mathf.Rad2Deg;

        // Rotate around the camera's forward axis
        transform.rotation = Quaternion.AngleAxis(angle, Camera.main.transform.forward);
    }
    public virtual void ScaleObject(Touch t1, Touch t2)
    {
        float currentDistance = Vector2.Distance(t1.position, t2.position);

        if (t1.phase == TouchPhase.Began || t2.phase == TouchPhase.Began)
        {
            lastPinchDistance = currentDistance; //reset pinch distance on new pinch
            return; // Don't scale on the first frame of the pinch
        }

        // Check if lastPinchDistance is valid
        if (lastPinchDistance <= 0)
        {
            lastPinchDistance = currentDistance; // Initialize if needed
            return;
        }


        float scaleFactor = (currentDistance - lastPinchDistance) * scaleSpeed; // Uses the updated small scaleSpeed
        transform.localScale += Vector3.one * scaleFactor;

        // Clamp scale to prevent inversion or extreme sizes
        transform.localScale = Vector3.Max(transform.localScale, Vector3.one * 0.1f);
        transform.localScale = Vector3.Min(transform.localScale, Vector3.one * 10f);


        lastPinchDistance = currentDistance;
    }

}