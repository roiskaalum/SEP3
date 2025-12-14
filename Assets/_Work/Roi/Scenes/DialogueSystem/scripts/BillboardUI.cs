using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    [Tooltip("If empty, the script will try to find a GameObject with the tag.")]
    public Transform cam;
    public string targetTag = "LookAtTarget";

    [Tooltip("Keep the billboard upright by only rotating around Y.")]
    public bool lockRotationToY = true;

    [Tooltip("Flip 180° if the visual faces the wrong way after LookAt.")]
    public bool invertFace = true;

    void Start()
    {
        if (cam == null)
        {
            //var found = GameObject.FindGameObjectWithTag(targetTag);
            var found = GameObject.FindFirstObjectByType<Camera>();
            if (found != null) cam = found.transform;
        }
    }

    void LateUpdate()
    {
        if (cam == null) return;

        // Use camera world position as the target — this makes the billboard face the camera.
        Vector3 targetPos = cam.position;

        // Optionally keep the billboard level (no pitch/roll) by constraining Y.
        if (lockRotationToY)
        {
            targetPos.y = transform.position.y;
        }

        // Make the forward (+Z) of the billboard point at the target position.
        transform.LookAt(targetPos, Vector3.up);

        // If the visual is facing away (prefab forward/back mismatch), invert.
        if (invertFace)
            transform.Rotate(0f, 180f, 0f, Space.Self);
    }
}
