using UnityEngine;

public interface iTouchable
{
    void SelectToggle(bool isSelected);
    void MoveObject(Touch touch, Camera mainCamera);
    void ScaleObject(Touch touch1, Touch touch2);
    void RotateObject(Touch touch1, Touch touch2);
    void ChangeColor(Color color);
}