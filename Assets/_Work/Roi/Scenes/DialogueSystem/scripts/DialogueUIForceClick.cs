using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class DialogueUIForceClick : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;
    public Button nextButton;
    public Transform choicesContainer;
    
    [Header("Choice Buttons (Pre-created in Inspector)")]
    [Tooltip("Assign 3-5 choice button GameObjects from the hierarchy")]
    public List<Button> choiceButtons = new List<Button>();
    
    public GameObject pausePanel;
    public TextMeshProUGUI pauseText;
    public Image pauseProgressBar;

    [Header("Debug")]
    public bool enableDebugLogs = true;
    [Tooltip("Draw collider bounds in Scene view (green wireframe)")]
    public bool showColliderGizmos = true;
    [Tooltip("Show collider info text in Game view")]
    public bool showColliderDebugText = true;

    private Coroutine _pauseRoutine;
    private List<DebugColliderVisual> _debugVisuals = new();

    private void Start()
    {
        if (enableDebugLogs)
            Debug.Log($"[ForceClick] Start - nextButton assigned: {nextButton != null}");

        // Set up next button listener
        //nextButton.onClick.AddListener(() => DialogueManager.Instance.OnNextPressed());
        
        // Initialize choice buttons
        InitializeChoiceButtons();
        
        HideChoices();
        pausePanel.SetActive(false);
    }

    private void InitializeChoiceButtons()
    {
        if (choiceButtons == null || choiceButtons.Count == 0)
        {
            Debug.LogError("[ForceClick] No choice buttons assigned! Please assign buttons in the Inspector.");
            return;
        }

        for (int i = 0; i < choiceButtons.Count; i++)
        {
            Button btn = choiceButtons[i];
            if (btn == null)
            {
                Debug.LogWarning($"[ForceClick] Choice button {i} is null!");
                continue;
            }

            // Set up ChoiceButton component index
            var choiceButtonComponent = btn.GetComponent<ChoiceButton>();
            if (choiceButtonComponent != null)
                choiceButtonComponent.index = i;

            // Clear any existing listeners
            btn.onClick.RemoveAllListeners();
            
            // Add listener with captured index
            //int idx = i;
            //btn.onClick.AddListener(() => 
            //{
            //    if (enableDebugLogs)
            //        Debug.Log($"[ForceClick] Choice button {idx} clicked");
            //    DialogueManager.Instance?.OnChoiceSelected(idx);
            //});

            // Ensure button has required components
            if (btn.GetComponent<ButtonCollisionTrigger>() == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"[ForceClick] Button '{btn.gameObject.name}' missing ButtonCollisionTrigger component!");
            }

            if (btn.GetComponent<BoxCollider>() == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"[ForceClick] Button '{btn.gameObject.name}' missing BoxCollider component!");
            }

            // Initially hide
            btn.gameObject.SetActive(false);
        }

        if (enableDebugLogs)
            Debug.Log($"[ForceClick] Initialized {choiceButtons.Count} choice buttons");
    }

    public void ShowDialogue(string speaker, string text)
    {
        pausePanel.SetActive(false);
        HideChoices();
        speakerText.text = speaker;
        dialogueText.text = text;
        nextButton.gameObject.SetActive(true);

        if (enableDebugLogs)
            Debug.Log($"[ForceClick] ShowDialogue - nextButton active: {nextButton.gameObject.activeSelf}");
    }

    public void ShowChoices(List<Choice> choices)
    {
        HideChoices();
        
        // Only hide next button if we're actually showing choices
        if (choices != null && choices.Count > 0)
        {
            nextButton.gameObject.SetActive(false);
        }

        if (choices == null || choices.Count == 0)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[ForceClick] ShowChoices called with null or empty choices");
            return;
        }

        int displayCount = Mathf.Min(choices.Count, choiceButtons.Count);

        if (enableDebugLogs)
            Debug.Log($"[ForceClick] ShowChoices - displaying {displayCount} of {choices.Count} choices");

        _debugVisuals.Clear();

        for (int i = 0; i < displayCount; i++)
        {
            Button btn = choiceButtons[i];
            if (btn == null) continue;

            btn.gameObject.SetActive(true);

            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            btn.GetComponent<ChoiceButton>().index = i;
            if (label != null)
                label.text = choices[i].text;
            else
                Debug.LogWarning($"[ForceClick] Button {i} has no TextMeshProUGUI child!");

            LogColliderInfo(btn, i);
        }

        if (choicesContainer != null)
        {
            var containerRect = choicesContainer.GetComponent<RectTransform>();
            if (containerRect != null)
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
        }
    }

    public void HideChoices()
    {
        foreach (Button btn in choiceButtons)
        {
            if (btn != null && btn.gameObject != null)
                btn.gameObject.SetActive(false);
        }
        
        _debugVisuals.Clear();
    }

    public void ShowPause(float duration)
    {
        pausePanel.SetActive(true);
        if (_pauseRoutine != null)
            StopCoroutine(_pauseRoutine);
        _pauseRoutine = StartCoroutine(PauseProgress(duration));
    }

    private System.Collections.IEnumerator PauseProgress(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            pauseProgressBar.fillAmount = Mathf.Clamp01(elapsed / duration);
            pauseText.text = $"Pausing... {Mathf.Ceil(duration - elapsed)}s";
            yield return null;
        }
        pausePanel.SetActive(false);
        DialogueManager.Instance.OnPauseComplete();
    }

    private void LogColliderInfo(Button btn, int index)
    {
        if (!enableDebugLogs) return;

        var bc = btn.GetComponent<BoxCollider>();
        if (bc == null)
        {
            Debug.LogError($"[ForceClick] Button {index} '{btn.gameObject.name}' has NO BoxCollider!");
            return;
        }

        var rt = btn.GetComponent<RectTransform>();
        Vector3 worldPos = btn.transform.position;
        Vector3 worldSize = new Vector3(
            bc.size.x * btn.transform.lossyScale.x,
            bc.size.y * btn.transform.lossyScale.y,
            bc.size.z * btn.transform.lossyScale.z
        );

        Debug.Log($"[ForceClick] Button[{index}] '{btn.gameObject.name}':\n" +
                 $"  Collider Size: {bc.size}\n" +
                 $"  Collider Center: {bc.center}\n" +
                 $"  World Pos: {worldPos}\n" +
                 $"  World Size: {worldSize}\n" +
                 $"  IsTrigger: {bc.isTrigger}\n" +
                 $"  Active: {btn.gameObject.activeSelf}\n" +
                 $"  Has ButtonCollisionTrigger: {btn.GetComponent<ButtonCollisionTrigger>() != null}");

        // Store debug visual info
        _debugVisuals.Add(new DebugColliderVisual
        {
            button = btn,
            collider = bc,
            index = index
        });
    }

    // Draw collider bounds in Scene view
    private void OnDrawGizmos()
    {
        if (!showColliderGizmos) return;

        // Draw gizmos for all choice buttons (even inactive ones)
        if (choiceButtons != null)
        {
            Gizmos.color = Color.yellow; // Different color for inactive
            foreach (var btn in choiceButtons)
            {
                if (btn == null) continue;
                var bc = btn.GetComponent<BoxCollider>();
                if (bc == null) continue;

                Gizmos.color = btn.gameObject.activeSelf ? Color.green : Color.yellow;

                Matrix4x4 rotationMatrix = Matrix4x4.TRS(
                    btn.transform.position,
                    btn.transform.rotation,
                    btn.transform.lossyScale
                );
                Gizmos.matrix = rotationMatrix;
                Gizmos.DrawWireCube(bc.center, bc.size);
            }
            Gizmos.matrix = Matrix4x4.identity;
        }
    }

    // Draw debug text in Game view
    private void OnGUI()
    {
        if (!showColliderDebugText || _debugVisuals == null) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 14;
        style.normal.textColor = Color.cyan;
        style.alignment = TextAnchor.MiddleCenter;

        foreach (var vis in _debugVisuals)
        {
            if (vis.button == null || vis.collider == null || !vis.button.gameObject.activeInHierarchy)
                continue;

            Vector3 worldPos = vis.button.transform.position;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            if (screenPos.z > 0)
            {
                screenPos.y = Screen.height - screenPos.y;
                string info = $"Choice {vis.index}\n" +
                             $"Size: {vis.collider.size.x:F1}x{vis.collider.size.y:F1}x{vis.collider.size.z:F1}\n" +
                             $"Trigger: {vis.collider.isTrigger}";
                GUI.Label(new Rect(screenPos.x - 50, screenPos.y - 30, 100, 60), info, style);
            }
        }
    }

    private struct DebugColliderVisual
    {
        public Button button;
        public BoxCollider collider;
        public int index;
    }
}
