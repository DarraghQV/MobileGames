using UnityEngine;

public abstract class TouchableObject : MonoBehaviour, iTouchable
{
    protected Renderer r;
    protected float rotationSpeed = 0.5f;
    protected float scaleSpeed = 0.01f;

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
        r.material.color = color;
    }

    public abstract void MoveObject(Touch touch, Camera mainCamera);

    public virtual void ScaleObject(float scaleFactor)
    {
        float newScale = Mathf.Clamp(transform.localScale.x + scaleFactor * scaleSpeed, 0.1f, 10f);
        transform.localScale = Vector3.one * newScale;
    }

    public virtual void RotateObject(Vector2 rotationDelta)
    {
        transform.Rotate(Vector3.up, rotationDelta.x * rotationSpeed, Space.World);
        transform.Rotate(Vector3.right, rotationDelta.y * rotationSpeed, Space.World);
    }
}