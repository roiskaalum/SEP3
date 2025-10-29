using UnityEngine;

public interface IInputProvider
{
    bool GetNextPressed();

    bool GetSelectPressed();

    Vector2 GetPointerPosition();
    bool GetChoicePressed(int number);
}
