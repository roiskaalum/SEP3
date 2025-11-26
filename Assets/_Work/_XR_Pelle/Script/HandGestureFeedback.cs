using UnityEngine;

public class HandGestureFeedback : MonoBehaviour
{
    [Header("Hand Renderer")]
    public Renderer handRenderer;

    [Header("Materials for each stage")]
    public Material stage0Material; // Closed
    public Material stage1Material; // Slightly open
    public Material stage2Material; // Half open
    public Material stage3Material; // Almost open
    public Material stage4Material; // Fully open

    [Header("Debug")]
    public GameObject cube;

    // Methods called by each gesture
    public void OnGestureStage0()
    {
        SetMaterial(stage0Material);
    }

    public void OnGestureStage1()
    {
        SetMaterial(stage1Material);
    }

    public void OnGestureStage2()
    {
        SetMaterial(stage2Material);
    }

    public void OnGestureStage3()
    {
        SetMaterial(stage3Material);
    }

    public void OnGestureStage4()
    {
        SetMaterial(stage3Material);
    }

    private void SetMaterial(Material mat)
    {
        if (handRenderer != null && mat != null)
        {
            handRenderer.material = mat;
            cube.GetComponent<Renderer>().material = mat;
        }
    }
}
