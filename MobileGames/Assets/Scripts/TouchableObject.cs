using UnityEngine;

public abstract class TouchableObject : MonoBehaviour, iTouchable
{
    protected Renderer r;
    protected float rotationSpeed = 0.5f;
    protected float scaleSpeed = 0.001f; 
    protected float lastPinchDistance; 

    protected virtual void Start()
    {
        r = GetComponent<Renderer>();
    }


    public virtual void SelectToggle(bool selected)
    {
        ChangeColor(selected ? Color.cyan : Color.white);
    }

    public virtual void ChangeColor(Color color)
    {
        if (r == null) r = GetComponent<Renderer>(); 
        if (r != null && r.material != null) 
        {
            r.material.color = color;
        }
        else
        {
            Debug.LogError($"{gameObject.name} - Renderer or Material not found in ChangeColor", this);
        }
    }

    public abstract void MoveObject(Touch touch, Camera mainCamera);


    public virtual void RotateObject(Touch t1, Touch t2)
    {
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
            lastPinchDistance = currentDistance;
            return; 
        }
        if (lastPinchDistance <= 0)
        {
            lastPinchDistance = currentDistance; 
            return;
        }

        float scaleFactor = (currentDistance - lastPinchDistance) * scaleSpeed; 
        transform.localScale += Vector3.one * scaleFactor;

        // Clamp scale to prevent inversion or extreme sizes
        transform.localScale = Vector3.Max(transform.localScale, Vector3.one * 0.1f);
        transform.localScale = Vector3.Min(transform.localScale, Vector3.one * 10f);

        lastPinchDistance = currentDistance;
    }

}