using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class TouchDetect : MonoBehaviour
{
    float timer = 0f;
    bool touchMoved = false;
    private float maxTapTime = 0.5f;

    Renderer r;
    void Start()
    {
        r = GetComponent<Renderer>();
    }

    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            timer += Time.deltaTime;


            switch (t.phase)
            {
                case TouchPhase.Began:
                    timer = 0;
                    touchMoved = false;
                    break;

                case TouchPhase.Moved:
                    touchMoved = true;
                    break;

                case TouchPhase.Ended:
                    if ((timer < maxTapTime) && !touchMoved)
                    {
                        changeColor(Color.blue);
                    }
                    break;
            }
            print(t.phase);
        }
    }
    private void changeColor(Color color)
    {
        r.material.color = color;
    }
}