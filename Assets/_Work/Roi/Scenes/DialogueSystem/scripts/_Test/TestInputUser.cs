using UnityEngine;

public class TestInputUser : MonoBehaviour
{
    private void Update()
    {
        if (InputRouter.Instance == null)
            return;

        if (InputRouter.Instance.NextPressed)
            Debug.Log("Next Pressed (space)");

        if (InputRouter.Instance.SelectPressed)
            Debug.Log("Select Pressed (enter)");
    }
}
