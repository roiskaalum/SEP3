using UnityEngine;
using UnityEngine.UI;

public class ButtonCollisionTrigger : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Tag for finger collider (e.g., 'FingerTip')")]
    public string fingerTag = "FingerTip";
    
    [Tooltip("Minimum seconds between clicks")]
    public float clickCooldown = 0.3f;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    private Button _button;
    private float _lastClickTime;

    void Start()
    {
        _button = GetComponent<Button>();
        if (_button == null)
        {
            Debug.LogError($"ButtonCollisionTrigger: No Button component found on '{gameObject.name}'");
        }

        // Ensure this GameObject has a trigger collider
        var col = GetComponent<BoxCollider>();
        if (col != null)
        {
            col.isTrigger = true;
            if (enableDebugLogs)
                Debug.Log($"[ButtonTrigger] Initialized on '{gameObject.name}'");
        }
        else
        {
            Debug.LogWarning($"ButtonCollisionTrigger: No BoxCollider found on '{gameObject.name}'");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_button == null) return;

        if (other.CompareTag(fingerTag))
        {
            if (enableDebugLogs)
                Debug.Log($"[ButtonTrigger] Finger entered '{gameObject.name}'");

            // Check cooldown
            if (Time.unscaledTime - _lastClickTime >= clickCooldown)
            {
                _lastClickTime = Time.unscaledTime;

                if (enableDebugLogs)
                    Debug.Log($"[ButtonTrigger] Invoking onClick on '{gameObject.name}'");

                _button.onClick?.Invoke();
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log($"[ButtonTrigger] Click blocked by cooldown");
            }
        }
    }
}
