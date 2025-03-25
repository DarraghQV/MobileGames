using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    interface iTouchable
    {
      void SelectToggle(bool isSelected);
      void MoveObject(Touch touch, Camera mainCamera);
      void ScaleObject(float scaleFactor);
      void RotateObject(Vector2 rotationDelta);
      void ChangeColor(Color color);
}
