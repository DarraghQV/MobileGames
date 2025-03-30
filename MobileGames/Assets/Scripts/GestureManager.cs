using UnityEngine;

public class GestureManager : MonoBehaviour
{
    private float tapTimeThreshold = 0.15f;
    private float holdTimeThreshold = 0.3f;
    private float timer = 0;
    private bool isHolding = false;
    private GameManager gameManager;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
    }

    void Update()
    {
        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);
            timer += Time.deltaTime;

            switch (t.phase)
            {
                case TouchPhase.Began:
                    timer = 0;
                    isHolding = false;
                    break;

                case TouchPhase.Stationary:
                case TouchPhase.Moved:
                    if (!isHolding && timer > holdTimeThreshold)
                    {
                        isHolding = true;
                        gameManager.TrySelectObject(t.position);
                    }
                    break;

                case TouchPhase.Ended:
                    if (timer < tapTimeThreshold)
                    {
                        // Quick tap - just select
                        gameManager.TrySelectObject(t.position);
                    }
                    gameManager.DeselectObject();
                    break;
            }
        }
        else if (Input.touchCount == 0)
        {
            gameManager.DeselectObject();
        }
    }
}