using UnityEngine;

public class CylinderScript : TouchableObject
{
    protected override void Start()
    {
        base.Start();
        rotationSpeed = 2f;
    }

    public override void MoveObject(Touch touch, Camera mainCamera)
    {
        float sensitivity = 0.05f;
        Vector3 touchDelta = touch.deltaPosition * sensitivity;

        transform.RotateAround(mainCamera.transform.position, Vector3.up, touchDelta.x);
        transform.RotateAround(mainCamera.transform.position, mainCamera.transform.right, -touchDelta.y);
    }
}