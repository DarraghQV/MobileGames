// GestureManager.cs
using UnityEngine;

public class GestureManager : MonoBehaviour
{
    private float tapTime = 0.5f;
    private bool hasMoved = false;
    private float timer = 0;
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
                    hasMoved = false;
                    break;
                case TouchPhase.Moved:
                    hasMoved = true;
                    break;
                case TouchPhase.Ended:
                    if (timer < tapTime && !hasMoved)
                    {
                        gameManager.TrySelectObject(t.position);
                    }
                    break;
            }
        }
    }
}