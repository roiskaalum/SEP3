using UnityEngine;
using UnityEngine.InputSystem;

public class KeyboardInputProvider : MonoBehaviour, IInputProvider
{
    public Key nextKey = Key.Space;
    public Key selectKey = Key.Enter;
    public Key choice1Key = Key.Digit1;
    public Key choice2Key = Key.Digit2;
    public Key choice3Key = Key.Digit3;
    //public bool GetNextPressed()
    //{
    //    return Keyboard.current[nextKey].wasPressedThisFrame;
    //}

    //public bool GetSelectPressed()
    //{
    //    return Keyboard.current[selectKey].wasPressedThisFrame;
    //}

    public bool GetNextPressed() => Keyboard.current[nextKey].wasPressedThisFrame;
    public bool GetSelectPressed() => Keyboard.current[selectKey].wasPressedThisFrame;

    public bool GetChoicePressed(int number)
    {
        switch (number)
        {
            case 1: return Keyboard.current[choice1Key].wasPressedThisFrame;
            case 2: return Keyboard.current[choice2Key].wasPressedThisFrame;
            case 3: return Keyboard.current[choice3Key].wasPressedThisFrame;
        }
        return false;
    }

    public bool Choice1Pressed => Keyboard.current[choice1Key].wasPressedThisFrame;
    public bool Choice2Pressed => Keyboard.current[choice2Key].wasPressedThisFrame;
    public bool Choice3Pressed => Keyboard.current[choice3Key].wasPressedThisFrame;

    public Vector2 GetPointerPosition() => Vector2.zero;

}
