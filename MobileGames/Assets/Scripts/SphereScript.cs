using UnityEngine;

public class SphereScript : TouchableObject
{
    private float raycastDistance = 1000f;
    private LayerMask placementMask;

    protected override void Start()
    {
        base.Start();
        placementMask = ~LayerMask.GetMask("SphereLayer");
    }

    public override void MoveObject(Touch touch, Camera mainCamera)
    {
        Ray ray = mainCamera.ScreenPointToRay(touch.position);
        RaycastHit hit;

        // This raycast IGNORES the "SphereLayer" because of placementMask
        if (Physics.Raycast(ray, out hit, raycastDistance, placementMask))
        {
            // So, the ray goes through the sphere and hits whatever is behind it.
            // The sphere then teleports to that background hit point.
            transform.position = hit.point;
        }
    }
}