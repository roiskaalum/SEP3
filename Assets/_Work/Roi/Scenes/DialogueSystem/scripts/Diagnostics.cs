using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Diagnostics : MonoBehaviour
{
    // Run validation next frame so the scene has finished initializing and logs are reliable
    void Start() => StartCoroutine(RunValidateNextFrame());

    private System.Collections.IEnumerator RunValidateNextFrame()
    {
        Debug.Log("UIInteractionValidator: Diagnostics.Start() - scheduling validation next frame.");
        yield return null; // wait one frame for other objects to initialize
        try
        {
            Validate();
        }
        catch (Exception ex)
        {
            Debug.LogError($"UIInteractionValidator: Validate() threw exception: {ex}");
        }
    }

    [ContextMenu("Validate UI Interaction")]
    public void Validate()
    {
        Debug.Log("UIInteractionValidator: Starting validation...");

        // EventSystem
        var es = EventSystem.current;
        if (es == null)
        {
            Debug.LogWarning("UIInteractionValidator: No EventSystem.current found in scene.");
        }
        else
        {
            Debug.Log($"UIInteractionValidator: EventSystem found on '{es.gameObject.name}'. Components:");
            foreach (var comp in es.gameObject.GetComponents<Component>())
                Debug.Log($"  - {comp.GetType().FullName}");
        }

        // DialogueUI
        var dialogueUI = FindFirstObjectByType<DialogueUI>();
        if (dialogueUI == null)
        {
            Debug.LogWarning("UIInteractionValidator: No DialogueUI instance found in scene.");
            return;
        }

        Debug.Log($"UIInteractionValidator: DialogueUI found on '{dialogueUI.gameObject.name}'.");

        // Canvas (parent)
        var canvas = dialogueUI.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("UIInteractionValidator: DialogueUI has no Canvas in parents.");
        }
        else
        {
            Debug.Log($"UIInteractionValidator: Canvas '{canvas.gameObject.name}' renderMode={canvas.renderMode}.");
            var gr = canvas.GetComponent<GraphicRaycaster>();
            Debug.Log($"  GraphicRaycaster present: {gr != null}");

            foreach (var comp in canvas.GetComponents<Component>())
            {
                var tn = comp.GetType().Name;
                if (tn.Contains("Tracked") || tn.Contains("TrackedDevice") || tn.Contains("Raycaster"))
                    Debug.Log($"  Canvas component: {tn}");
            }

            var cg = canvas.GetComponent<CanvasGroup>();
            Debug.Log($"  CanvasGroup present: {(cg != null)} blocking={(cg != null ? cg.blocksRaycasts.ToString() : "n/a")}");
        }

        // Prefab check
        if (dialogueUI.choiceButtonPrefab == null)
        {
            Debug.LogWarning("UIInteractionValidator: DialogueUI.choiceButtonPrefab is not assigned.");
        }
        else
        {
            var pf = dialogueUI.choiceButtonPrefab;
            Debug.Log($"UIInteractionValidator: choiceButtonPrefab '{pf.name}' has components:");
            Debug.Log($"  Button: {pf.GetComponent<Button>() != null}, BoxCollider: {pf.GetComponent<BoxCollider>() != null}, RectTransform: {pf.GetComponent<RectTransform>() != null}");
        }

        // Spawned children under choicesContainer
        if (dialogueUI.choicesContainer == null)
        {
            Debug.LogWarning("UIInteractionValidator: DialogueUI.choicesContainer is not assigned.");
        }
        else
        {
            Debug.Log($"UIInteractionValidator: Inspecting children of '{dialogueUI.choicesContainer.name}' ({dialogueUI.choicesContainer.childCount} children)");
            for (int i = 0; i < dialogueUI.choicesContainer.childCount; i++)
            {
                var child = dialogueUI.choicesContainer.GetChild(i);
                var btn = child.GetComponent<Button>();
                var bc = child.GetComponent<BoxCollider>();
                var rt = child.GetComponent<RectTransform>();
                string rectInfo = rt != null ? $"{rt.rect.width:F1}x{rt.rect.height:F1}" : "n/a";
                string interactable = btn != null ? btn.interactable.ToString() : "n/a";
                Debug.Log($"  Child[{i}] '{child.name}': active={child.gameObject.activeSelf}, Button={btn != null}, interactable={interactable}, BoxCollider={bc != null}, Rect={rectInfo}");
            }
        }

        // Detect XR Poke Interactor(s) by type name
        var foundPoke = false;
        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            var tn = mb.GetType().Name;
            if (tn.IndexOf("Poke", StringComparison.OrdinalIgnoreCase) >= 0 ||
                tn.IndexOf("XRUI", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Debug.Log($"UIInteractionValidator: Found interactor type '{tn}' on '{mb.gameObject.name}'.");
                foundPoke = true;
            }
        }
        if (!foundPoke)
            Debug.LogWarning("UIInteractionValidator: No PokeInteractor/XR UI input components found.");

        // Final quick checks
        Debug.Log($"UIInteractionValidator: DialogueManager.Instance is {(DialogueManager.Instance != null ? "present" : "NULL")}");
        Debug.Log("UIInteractionValidator: Validation complete.");
    }
}