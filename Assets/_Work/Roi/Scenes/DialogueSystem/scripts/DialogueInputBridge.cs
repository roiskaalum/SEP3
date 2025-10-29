using UnityEngine;

public class DialogueInputBridge : MonoBehaviour
{
    private DialogueManager _manager;

    private void Start()
    {
        _manager = DialogueManager.Instance;
    }

    private void Update()
    {
        if (InputRouter.Instance == null || _manager == null)
            return;

        // Skip or advance
        if (InputRouter.Instance.NextPressed || InputRouter.Instance.SelectPressed)
        {
            _manager.OnNextPressed();
        }

        // Handle choice selection
        if (InputRouter.Instance.GetChoicePressed(1))
            _manager.OnChoiceSelected(0);
        if (InputRouter.Instance.GetChoicePressed(2))
            _manager.OnChoiceSelected(1);
        if (InputRouter.Instance.GetChoicePressed(3))
            _manager.OnChoiceSelected(2);
    }
}
