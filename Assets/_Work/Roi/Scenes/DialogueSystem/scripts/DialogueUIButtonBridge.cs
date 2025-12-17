using UnityEngine;


public class DialogueUIButtonBridge : MonoBehaviour
{
    // Called by the Next button's OnClick
    public void OnNextPressed()
    {
        Debug.Log("DialogueUIButtonBridge: OnNextPressed called.");
        if (DialogueManager.Instance != null)
        {
            Debug.Log("DialogueUIButtonBridge: Invoking DialogueManager.OnNextPressed()");
            DialogueManager.Instance.OnNextPressed();
        }
    }

    // Optional: expose other DialogueManager actions if needed
    public void OnPauseComplete()
    {
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnPauseComplete();
    }
}