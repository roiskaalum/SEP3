using UnityEngine;
using System;
using Unity.VisualScripting;

public class TempListener : MonoBehaviour
{
    public static Action ButtonClicked;
    bool clicked = false;
    void OnButtonClick()
    {
        clicked = true;
        Console.WriteLine("Knap trykked");
    }
}
