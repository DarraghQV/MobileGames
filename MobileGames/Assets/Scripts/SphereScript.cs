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

        if (Physics.Raycast(ray, out hit, raycastDistance, placementMask))
        {
            transform.position = hit.point;
        }
    }
}