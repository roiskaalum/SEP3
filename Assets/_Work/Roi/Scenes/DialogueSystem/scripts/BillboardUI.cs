using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    public Transform cam;

    void Start() => cam = Camera.main.transform;

    void LateUpdate()
    {
        transform.LookAt(transform.position + cam.forward);
    }
}
