using UnityEngine;


public class ChoiceButton : MonoBehaviour
{
    [Tooltip("Zero-based index for this choice")]
    public int index;


    // Hook this to the Button.OnClick
    public void Press()
    {
        Debug.Log("ChoiceButton Pressed: " + index);
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnChoiceSelected(index);
    }
}