using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    public Transform cam;

    void LateUpdate()
    {
        transform.LookAt(cam.position);
    }
}
