using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AnimationTrigger : MonoBehaviour
{
    [Header("Main Character (Lars)")]
    [Tooltip("Assign the GameObject with Lars' Animator component")]
    public GameObject mainCharacterObject;

    [Header("Wake-Up Sequence Timing (Lars)")]
    [Tooltip("Delay before setting 'Awake' to true (seconds)")]
    public float delayBeforeAwake = 0f;
    
    [Tooltip("Delay before setting 'Standing' to true after Awake (seconds)")]
    public float delayBeforeStanding = 4f;
    
    [Tooltip("Delay before showing UI and starting dialogue after Standing (seconds)")]
    public float delayBeforeUIShow = 6f;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    public DialogueUIForceClick ui;

    private Animator mainCharacterAnimator;
    private List<Animator> backgroundAnimators = new List<Animator>();
    
    // Track Lars' emotional state progression
    private bool hasBeenAggressiveOnce = false;

    private void Awake()
    {
        ui = FindFirstObjectByType<DialogueUIForceClick>();
        if (ui == null && enableDebugLogs)
        {
            Debug.LogWarning("[AnimationTrigger] DialogueUIForceClick component not found in scene during Awake()");
        }
        CollectAnimators();
    }

    private void Start()
    {
        // Start Lars' wake-up sequence
        StartCoroutine(LarsWakeUpSequence());
    }

    private void CollectAnimators()
    {
        // Find all Animators in the scene
        Animator[] allAnimators = FindObjectsByType<Animator>(FindObjectsSortMode.None);

        if (enableDebugLogs)
            Debug.Log($"[AnimationTrigger] Found {allAnimators.Length} total Animators in scene");

        // Separate main character animator from background animators
        if (mainCharacterObject != null)
        {
            mainCharacterAnimator = mainCharacterObject.GetComponent<Animator>();
            if (mainCharacterAnimator == null)
            {
                Debug.LogError($"[AnimationTrigger] Main character object '{mainCharacterObject.name}' has no Animator component!");
            }
            else if (enableDebugLogs)
            {
                Debug.Log($"[AnimationTrigger] Main character animator found: '{mainCharacterObject.name}'");
            }
        }
        else
        {
            Debug.LogWarning("[AnimationTrigger] No main character object assigned!");
        }

        // Add all other animators to background list
        foreach (Animator anim in allAnimators)
        {
            if (anim != mainCharacterAnimator)
            {
                backgroundAnimators.Add(anim);
                if (enableDebugLogs)
                    Debug.Log($"[AnimationTrigger] Added background animator: '{anim.gameObject.name}'");
            }
        }

        if (enableDebugLogs)
            Debug.Log($"[AnimationTrigger] Total background animators: {backgroundAnimators.Count}");
    }

    private IEnumerator LarsWakeUpSequence()
    {
        if (mainCharacterAnimator == null)
        {
            Debug.LogError("[AnimationTrigger] Cannot start wake-up sequence - main character animator is null!");
            yield break;
        }

        // Hide UI initially
        if (DialogueManager.Instance != null)
        {
            if (ui != null)
            {
                ui.gameObject.SetActive(false);
                if (enableDebugLogs)
                    Debug.Log("[AnimationTrigger] UI hidden for wake-up sequence");
            }
        }

        // Wait before first transition
        if (enableDebugLogs)
            Debug.Log($"[AnimationTrigger] Waiting {delayBeforeAwake}s before 'Awake'");
        yield return new WaitForSeconds(delayBeforeAwake);

        // First transition: Awake
        if (enableDebugLogs)
            Debug.Log("[AnimationTrigger] Setting 'Awake' to true");
        mainCharacterAnimator.SetBool("awake", true);

        // Wait before second transition
        if (enableDebugLogs)
            Debug.Log($"[AnimationTrigger] Waiting {delayBeforeStanding}s before 'Standing'");
        yield return new WaitForSeconds(delayBeforeStanding);

        // Second transition: Standing
        if (enableDebugLogs)
            Debug.Log("[AnimationTrigger] Setting 'Standing' to true");
        mainCharacterAnimator.SetBool("standing", true);

        // Wait before showing UI and starting dialogue
        if (enableDebugLogs)
            Debug.Log($"[AnimationTrigger] Waiting {delayBeforeUIShow}s before showing UI");
        yield return new WaitForSeconds(delayBeforeUIShow);

        // Show UI and start dialogue
        if (enableDebugLogs)
            Debug.Log("[AnimationTrigger] Wake-up sequence complete! Showing UI and starting dialogue");

        if (ui != null)
        {
            ui.gameObject.SetActive(true);
        }

        // Tell DialogueManager to start displaying dialogue
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.DisplayCurrentNode();
        }
    }

    /// <summary>
    /// Activates the "IsScared" animation for all background NPCs
    /// </summary>
    public void ActivateBackgroundNPCs()
    {
        if (enableDebugLogs)
            Debug.Log("[AnimationTrigger] Activating background NPCs (IsScared = true)");

        foreach (Animator anim in backgroundAnimators)
        {
            if (anim == null) continue;

            try
            {
                anim.SetBool("IsScared", true);
                if (enableDebugLogs)
                    Debug.Log($"[AnimationTrigger] Set IsScared=true for '{anim.gameObject.name}'");
            }
            catch (System.Exception ex)
            {
                // Safely fail if animator doesn't have "IsScared" parameter
                if (enableDebugLogs)
                    Debug.LogWarning($"[AnimationTrigger] Failed to set IsScared for '{anim.gameObject.name}': {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Sets Lars' emotional state based on dialogue choice
    /// Choice 0 = Calm path (Aggressive first, then SuperAggressive on second choice)
    /// Choice 1 = Aggressive direct path (SuperAggressive immediately)
    /// </summary>
    public void SetLarsEmotionalState(int choiceIndex)
    {
        if (mainCharacterAnimator == null)
        {
            Debug.LogError("[AnimationTrigger] Cannot set Lars' state - main character animator is null!");
            return;
        }

        if (enableDebugLogs)
            Debug.Log($"[AnimationTrigger] Setting Lars' emotional state for choice {choiceIndex} | hasBeenAggressiveOnce: {hasBeenAggressiveOnce}");

        switch (choiceIndex)
        {
            case 0:
                // Calm path - escalates gradually
                if (!hasBeenAggressiveOnce)
                {
                    // First time: Set Aggressive = true
                    mainCharacterAnimator.SetBool("aggressive", true);
                    hasBeenAggressiveOnce = true;
                    if (enableDebugLogs)
                        Debug.Log("[AnimationTrigger] Lars: Aggressive = true (first escalation - calm path)");
                }
                else
                {
                    // Second time: Aggressive -> false, then SuperAggressive -> true
                    if (enableDebugLogs)
                        Debug.Log("[AnimationTrigger] Lars: Transitioning Aggressive -> SuperAggressive (calm path failed)");
                    
                    mainCharacterAnimator.SetBool("aggressive", false);
                    mainCharacterAnimator.SetBool("superAggressive", true);
                    
                    if (enableDebugLogs)
                        Debug.Log("[AnimationTrigger] Lars: SuperAggressive = true (final state)");
                }
                break;

            case 1:
                // Aggressive direct path - skip straight to SuperAggressive
                if (enableDebugLogs)
                    Debug.Log("[AnimationTrigger] Lars: Going directly to SuperAggressive (aggressive path)");
                
                mainCharacterAnimator.SetBool("SuperAggressive", true);
                hasBeenAggressiveOnce = true; // Mark as escalated
                
                if (enableDebugLogs)
                    Debug.Log("[AnimationTrigger] Lars: SuperAggressive = true (immediate escalation)");
                break;

            default:
                Debug.LogWarning($"[AnimationTrigger] Unknown choice index: {choiceIndex}");
                break;
        }
    }

    /// <summary>
    /// Resets Lars' emotional state bools (if needed for testing)
    /// </summary>
    public void ResetLarsEmotionalState()
    {
        if (mainCharacterAnimator == null) return;

        mainCharacterAnimator.SetBool("aggressive", false);
        mainCharacterAnimator.SetBool("superAggressive", false);
        hasBeenAggressiveOnce = false;

        if (enableDebugLogs)
            Debug.Log("[AnimationTrigger] Lars emotional state reset");
    }
}
