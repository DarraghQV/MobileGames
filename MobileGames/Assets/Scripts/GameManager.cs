using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    iTouchable selectedObject;
    private Vector2 initialTouchPosition;
    private Transform selectedTransform;
    private Camera mainCamera;
    public LayerMask ignoreLayerMask;
    private float raycastDistance = 1000f;
    private float verticalRotationLimit = 80f; // Limits the vertical camera rotation to ±80 degrees
    private float currentVerticalRotation = 0f;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                initialTouchPosition = touch.position;
                TrySelectObject(touch.position);
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                if (selectedTransform != null)
                {
                    if (selectedObject is CubeScript)
                    {
                        MoveCubeObject(touch);
                    }
                    else if (selectedObject is CylinderScript)
                    {
                        OrbitAroundCamera(touch);
                    }
                    else if (selectedObject is SphereScript)
                    {
                        MoveSelectedObjectOnSurface(touch);
                    }
                    else
                    {
                        MoveSelectedObject(touch);
                    }
                }
                else
                {
                    MoveCamera(touch);
                }
            }
        }
        else if (Input.touchCount == 2)
        {
            RotateCamera();
        }
    }

    public void TrySelectObject(Vector2 tapPosition)
    {
        Ray r = mainCamera.ScreenPointToRay(tapPosition);
        RaycastHit info;
        if (Physics.Raycast(r, out info))
        {
            iTouchable newObject = info.collider.gameObject.GetComponent<iTouchable>();
            if (newObject != null)
            {
                if (selectedObject == newObject)
                {
                    selectedObject.SelectToggle(false);
                    selectedObject = null;
                    selectedTransform = null;
                }
                else
                {
                    if (selectedObject != null)
                    {
                        selectedObject.SelectToggle(false);
                    }
                    selectedObject = newObject;
                    selectedTransform = info.collider.transform;
                    newObject.SelectToggle(true);
                }
            }
        }
    }

    private void MoveCubeObject(Touch touch)
    {
        if (selectedTransform != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(touch.position);
            Plane plane = new Plane(mainCamera.transform.forward, selectedTransform.position);

            float distanceToPlane;
            if (plane.Raycast(ray, out distanceToPlane))
            {
                Vector3 targetPosition = ray.GetPoint(distanceToPlane);
                float sensitivity = 1f;
                Vector3 moveDelta = targetPosition - selectedTransform.position;
                selectedTransform.position += moveDelta * sensitivity;
            }
        }
    }

    private void MoveSelectedObject(Touch touch)
    {
        if (selectedTransform != null)
        {
            Vector3 touchPosition = mainCamera.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, mainCamera.WorldToScreenPoint(selectedTransform.position).z));
            selectedTransform.position = new Vector3(touchPosition.x, touchPosition.y, selectedTransform.position.z);
        }
    }

    private void MoveSelectedObjectOnSurface(Touch touch)
    {
        if (selectedTransform != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(touch.position);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, raycastDistance, ~ignoreLayerMask))
            {
                selectedTransform.position = hit.point;
            }
            else
            {
                selectedTransform.position = selectedTransform.position;
            }
        }
    }

    private void OrbitAroundCamera(Touch touch)
    {
        if (selectedTransform != null)
        {
            float sensitivity = 0.01f;
            Vector3 touchDelta = touch.deltaPosition * sensitivity;

            selectedTransform.RotateAround(mainCamera.transform.position, Vector3.up, touchDelta.x);
            selectedTransform.RotateAround(mainCamera.transform.position, mainCamera.transform.right, -touchDelta.y);
        }
    }

    private void MoveCamera(Touch touch)
    {
        if (selectedObject == null && Input.touchCount == 1)
        {
            if (touch.phase == TouchPhase.Moved)
            {
                float moveSpeed = 0.025f;
                Vector2 delta = touch.deltaPosition;
                float horizontal = delta.x * moveSpeed;
                float vertical = delta.y * moveSpeed;
                mainCamera.transform.Translate(horizontal, vertical, 0);
            }
        }
    }

    private void RotateCamera()
    {
        if (selectedObject == null && Input.touchCount == 2)
        {
            Touch touch = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            if (touch.phase == TouchPhase.Moved && touch2.phase == TouchPhase.Moved)
            {
                float rotationSpeed = 0.2f;
                Vector2 delta = touch.deltaPosition - touch2.deltaPosition;

                // Horizontal rotation (around Y-axis)
                float horizontal = delta.x * rotationSpeed;

                // Vertical rotation (around X-axis)
                float vertical = -delta.y * rotationSpeed;

                // Update the current vertical rotation and clamp it within the limit
                currentVerticalRotation += vertical;
                currentVerticalRotation = Mathf.Clamp(currentVerticalRotation, -verticalRotationLimit, verticalRotationLimit);

                // Apply the rotation
                mainCamera.transform.Rotate(Vector3.up, horizontal, Space.Self);
                mainCamera.transform.localRotation = Quaternion.Euler(currentVerticalRotation, mainCamera.transform.localRotation.eulerAngles.y, 0);
            }
        }
    }

}
