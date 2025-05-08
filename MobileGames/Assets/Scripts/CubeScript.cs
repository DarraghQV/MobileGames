using UnityEngine;

public class CubeScript : TouchableObject
{
    public override void MoveObject(Touch touch, Camera mainCamera)
    {
        Ray ray = mainCamera.ScreenPointToRay(touch.position);
        Plane plane = new Plane(mainCamera.transform.forward, transform.position);

        float distanceToPlane;
        if (plane.Raycast(ray, out distanceToPlane))
        {
            Vector3 targetPosition = ray.GetPoint(distanceToPlane);
            float sensitivity = 1f;
            Vector3 moveDelta = targetPosition - transform.position;
            transform.position += moveDelta * sensitivity;
        }
    }
}