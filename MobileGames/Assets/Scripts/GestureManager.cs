// --- Updated FILE GestureManager.cs ---

using UnityEngine;

public class GestureManager : MonoBehaviour
{
    private float tapTimeThreshold = 0.15f;
    private float holdTimeThreshold = 0.3f;
    private float timer = 0;
    private bool isHolding = false;
    private GameManager gameManager;
    private bool touchStartedOverUI = false; 

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
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            {
                touchStartedOverUI = true;
                if (isHolding && gameManager != null)
                {
                    gameManager.DeselectObject();
                    isHolding = false;
                }
            }
            else
            {
                touchStartedOverUI = false;
            }
        }

        if (touchStartedOverUI && Input.touchCount > 0)
        {
            if (Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetTouch(0).phase == TouchPhase.Canceled)
            {
                touchStartedOverUI = false;
            }
            return;
        }


        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);
            timer += Time.deltaTime;

            switch (t.phase)
            {
                case TouchPhase.Began:
                    if (!touchStartedOverUI)
                    {
                        timer = 0;
                        isHolding = false;
                    }
                    break;

                case TouchPhase.Stationary:
                case TouchPhase.Moved:
                    if (!isHolding && timer > holdTimeThreshold && gameManager != null)
                    {
                        isHolding = true;
                        gameManager.TrySelectObject(t.position);
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled: 
                    bool wasTapSelectAttempt = false;
                    if (!isHolding && timer < tapTimeThreshold && gameManager != null)
                    {
                        gameManager.TrySelectObject(t.position);
                        wasTapSelectAttempt = true;
                    }
                    if ((isHolding || !wasTapSelectAttempt) && gameManager != null)
                    {
                        gameManager.DeselectObject();
                    }

                    isHolding = false;
                    timer = 0;
                    touchStartedOverUI = false; 
                    break;
            }
        }
        else if (Input.touchCount == 0 && isHolding && gameManager != null) 
        {
            gameManager.DeselectObject();
            isHolding = false;
            timer = 0;
            touchStartedOverUI = false;
        }
        else if (Input.touchCount > 1 && isHolding && gameManager != null)
        {
        }

    }
}