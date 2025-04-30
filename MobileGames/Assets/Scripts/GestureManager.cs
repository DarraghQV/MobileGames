// --- Updated FILE GestureManager.cs ---

using UnityEngine;

public class GestureManager : MonoBehaviour
{
    private float tapTimeThreshold = 0.15f;
    private float holdTimeThreshold = 0.3f;
    private float timer = 0;
    private bool isHolding = false;
    private GameManager gameManager;
    private bool touchStartedOverUI = false; // Prevent interacting through UI

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("GestureManager could not find GameManager!");
        }
    }

    void Update()
    {
        // Prevent interaction if touching UI elements
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            {
                touchStartedOverUI = true;
                // If holding, deselect immediately if touch restarts over UI
                if (isHolding && gameManager != null)
                {
                    gameManager.DeselectObject();
                    isHolding = false;
                }
                // Debug.Log("Touch started over UI.");
            }
            else
            {
                touchStartedOverUI = false;
            }
        }

        if (touchStartedOverUI && Input.touchCount > 0)
        {
            // If touch ends while still flagged as UI touch, reset flag
            if (Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetTouch(0).phase == TouchPhase.Canceled)
            {
                touchStartedOverUI = false;
            }
            return; // Don't process game gestures if touch started on UI
        }


        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);
            timer += Time.deltaTime;

            switch (t.phase)
            {
                case TouchPhase.Began:
                    // Reset only if not started over UI
                    if (!touchStartedOverUI)
                    {
                        timer = 0;
                        isHolding = false;
                        // We might want to try selecting immediately on Began if it's over an object,
                        // but let's stick to tap/hold for now.
                        // gameManager.TrySelectObject(t.position); // Potential alternative: select immediately
                    }
                    break;

                case TouchPhase.Stationary:
                case TouchPhase.Moved:
                    if (!isHolding && timer > holdTimeThreshold && gameManager != null)
                    {
                        // Debug.Log("Hold Detected - Trying Select");
                        isHolding = true;
                        gameManager.TrySelectObject(t.position); // Try selecting on hold
                    }
                    // If already holding, GameManager's Update handles movement
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled: // Treat cancel like end
                    // Debug.Log($"Touch Ended/Canceled: Timer={timer}, Holding={isHolding}");
                    bool wasTapSelectAttempt = false;
                    if (!isHolding && timer < tapTimeThreshold && gameManager != null)
                    {
                        // Debug.Log("Tap Detected - Trying Select");
                        gameManager.TrySelectObject(t.position); // Try selecting on tap
                        // We assume TrySelectObject might succeed. If it does, we don't want to deselect.
                        // A better approach might be for TrySelectObject to return true/false.
                        wasTapSelectAttempt = true;
                    }

                    // Deselect logic:
                    // - If ending a hold (isHolding was true) -> Deselect
                    // - If it was NOT a tap select attempt -> Deselect (means it was a tap on empty space or a drag end without hold)
                    if ((isHolding || !wasTapSelectAttempt) && gameManager != null)
                    {
                        // Debug.Log("Deselecting object on Touch End/Cancel.");
                        gameManager.DeselectObject();
                    }
                    // else { Debug.Log("Tap selection attempted, NOT deselecting immediately."); }


                    // Reset states
                    isHolding = false;
                    timer = 0;
                    touchStartedOverUI = false; // Reset UI flag on touch end
                    break;
            }
        }
        // If multiple touches or no touches, ensure deselection (unless already handled by Ended/Canceled)
        // Note: Multi-touch object manipulation is handled in GameManager,
        // Camera control is handled by CameraController when no object is selected.
        // Deselection on touchCount 0 or >1 might interfere with 2-finger object manipulation start.
        // Let's rely on the Ended/Canceled phase for deselection for single touch.
        // And rely on CameraController checking GetSelectedObject for multi-touch camera control.
        // else if (Input.touchCount == 0 || Input.touchCount > 1) // Previous logic - might be too aggressive
        else if (Input.touchCount == 0 && isHolding && gameManager != null) // Deselect only if touch disappears while holding
        {
            // Debug.Log("Touch count is 0, deselecting held object.");
            gameManager.DeselectObject();
            isHolding = false;
            timer = 0;
            touchStartedOverUI = false;
        }
        else if (Input.touchCount > 1 && isHolding && gameManager != null)
        {
            // If a second touch appears while holding an object with one,
            // we *keep* it selected for manipulation, so don't deselect here.
            // We might need to reset the 'isHolding' flag conceptually for single-touch logic though.
            // isHolding = false; // Let GameManager handle multi-touch from now on
            // timer = 0;
        }

    }
}