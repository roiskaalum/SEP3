using UnityEngine;

public class AnimButton : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    Animator animator;

    public void OnButtonPress()
    {
        animator.SetBool("IsScared", true);
    }
}
