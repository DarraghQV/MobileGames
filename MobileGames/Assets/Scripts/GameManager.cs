using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    iTouchable selectedObject;
    private Vector2 initialTouchPosition;
    private Transform selectedTransform;
    private Camera mainCamera;
    public LayerMask ignoreLayerMask; // LayerMask to exclude the sphere's layer
    private float raycastDistance = 1000f; // A sufficient distance for raycasting

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
                        MoveCubeObject(touch);  // Updated for free 3D movement
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
                    RotateCamera(touch);
                }
            }
        }
        else if (Input.touchCount == 2)
        {
            MoveCamera();
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
            Vector3 touchPosition = touch.position;

            float distanceToCamera = Camera.main.WorldToScreenPoint(selectedTransform.position).z;

            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, distanceToCamera));

            float sensitivity = 0.01f; 
            Vector3 moveDelta = worldPosition - selectedTransform.position;
            selectedTransform.position += moveDelta * sensitivity;
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
            float sensitivity = 0.2f;

            Vector3 touchDelta = touch.deltaPosition * sensitivity;

            selectedTransform.RotateAround(mainCamera.transform.position, Vector3.up, touchDelta.x);
            selectedTransform.RotateAround(mainCamera.transform.position, mainCamera.transform.right, -touchDelta.y);
        }
    }

    private void MoveCamera()
    {
        if (selectedObject == null && Input.touchCount == 2)
        {
            Touch touch = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            if (touch.phase == TouchPhase.Moved && touch2.phase == TouchPhase.Moved)
            {
                float moveSpeed = 0.025f;
                Vector2 averageDelta = (touch.deltaPosition + touch2.deltaPosition) / 2;
                float horizontal = averageDelta.x * moveSpeed;
                float vertical = averageDelta.y * moveSpeed;

                mainCamera.transform.Translate(horizontal, vertical, 0);
            }
        }
    }

    private void RotateCamera(Touch touch)
    {
        if (selectedObject == null)
        {
            float rotationSpeed = 0.2f;
            float horizontal = touch.deltaPosition.x * rotationSpeed;
            float vertical = -touch.deltaPosition.y * rotationSpeed;

            mainCamera.transform.Rotate(Vector3.up, horizontal, Space.Self);
            mainCamera.transform.Rotate(mainCamera.transform.right, vertical, Space.Self);
        }
    }
}
