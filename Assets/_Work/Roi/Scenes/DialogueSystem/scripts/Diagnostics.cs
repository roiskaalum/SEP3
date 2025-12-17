using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Management;

public class Diagnostics : MonoBehaviour
{
    void Start() => StartCoroutine(RunValidateNextFrame());

    private IEnumerator RunValidateNextFrame()
    {
        Debug.Log("UIInteractionValidator: Scheduling validation next frame.");
        yield return null;
        try
        {
            Validate();
        }
        catch (Exception ex)
        {
            Debug.LogError($"UIInteractionValidator: Exception in Validate(): {ex}");
        }
    }

    [ContextMenu("Validate UI Interaction")]
    public void Validate()
    {
        Debug.Log("UIInteractionValidator: Starting validation...");

        // Basic XR status
        Debug.Log($"UIInteractionValidator: XR Device Active: {XRSettings.isDeviceActive}");
        Debug.Log($"UIInteractionValidator: Loaded Device Name: {XRSettings.loadedDeviceName}");

        // EventSystem and Input Module
        var eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogWarning("UIInteractionValidator: No EventSystem found in scene!");
        }
        else
        {
            Debug.Log($"UIInteractionValidator: EventSystem on '{eventSystem.gameObject.name}'");
            var xrInputModule = eventSystem.GetComponent<XRUIInputModule>();
            Debug.Log($"   XRUIInputModule present: {xrInputModule != null}");
            if (xrInputModule != null)
                Debug.Log($"     Enabled: {xrInputModule.enabled}");

            var baseInputModule = eventSystem.GetComponent<BaseInputModule>();
            Debug.Log($"   Any BaseInputModule active: {baseInputModule != null && baseInputModule.enabled}");
        }

        // DialogueUI
        var dialogueUI = FindFirstObjectByType<DialogueUI>();
        if (dialogueUI == null)
        {
            Debug.LogWarning("UIInteractionValidator: No DialogueUI found!");
            return;
        }
        Debug.Log($"UIInteractionValidator: DialogueUI found on '{dialogueUI.gameObject.name}'");

        // Canvas
        var canvas = dialogueUI.GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = dialogueUI.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("UIInteractionValidator: No Canvas found on DialogueUI or parents!");
        }
        else
        {
            Debug.Log($"UIInteractionValidator: Canvas '{canvas.gameObject.name}' RenderMode={canvas.renderMode}");
            Debug.Log($"   GraphicRaycaster: {canvas.GetComponent<GraphicRaycaster>() != null}");
            var tdgr = canvas.GetComponent<TrackedDeviceGraphicRaycaster>();
            Debug.Log($"   TrackedDeviceGraphicRaycaster: {tdgr != null}");
            if (tdgr != null)
                Debug.Log($"     Enabled: {tdgr.enabled}");

            var canvasGroup = canvas.GetComponent<CanvasGroup>();
            Debug.Log($"   CanvasGroup: {canvasGroup != null}");
            if (canvasGroup != null)
                Debug.Log($"     blocksRaycasts: {canvasGroup.blocksRaycasts}, interactable: {canvasGroup.interactable}");
        }

        // Choice button prefab
        if (dialogueUI.choiceButtonPrefab == null)
        {
            Debug.LogWarning("UIInteractionValidator: choiceButtonPrefab is not assigned in DialogueUI!");
        }
        else
        {
            var prefab = dialogueUI.choiceButtonPrefab;
            Debug.Log($"UIInteractionValidator: choiceButtonPrefab '{prefab.name}':");
            Debug.Log($"   Has Button: {prefab.GetComponent<Button>() != null}");
            Debug.Log($"   Has BoxCollider: {prefab.GetComponent<BoxCollider>() != null}");
            Debug.Log($"   Has RectTransform: {prefab.GetComponent<RectTransform>() != null}");
        }

        // Active choice buttons in container
        if (dialogueUI.choicesContainer == null)
        {
            Debug.LogWarning("UIInteractionValidator: choicesContainer not assigned!");
        }
        else
        {
            Debug.Log($"UIInteractionValidator: choicesContainer has {dialogueUI.choicesContainer.childCount} children:");
            for (int i = 0; i < dialogueUI.choicesContainer.childCount; i++)
            {
                var child = dialogueUI.choicesContainer.GetChild(i);
                var btn = child.GetComponent<Button>();
                var col = child.GetComponent<BoxCollider>();
                var rt = child.GetComponent<RectTransform>();
                string size = rt != null ? $"{rt.rect.width:F1}x{rt.rect.height:F1}" : "n/a";
                Debug.Log($"   [{i}] '{child.name}': Active={child.gameObject.activeSelf}, Button={btn != null}, Interactable={(btn != null ? btn.interactable : false)}, Collider={col != null}, Size={size}");
            }
        }

        // XRPokeInteractor(s)
        var pokeInteractors = FindObjectsByType<XRPokeInteractor>(FindObjectsSortMode.None);
        Debug.Log($"UIInteractionValidator: Found {pokeInteractors.Length} XRPokeInteractor(s)");
        if (pokeInteractors.Length == 0)
        {
            Debug.LogWarning("UIInteractionValidator: No XRPokeInteractor in scene — poke will not work!");
        }
        else
        {
            foreach (var poke in pokeInteractors)
            {
                Debug.Log($"   On '{poke.gameObject.name}': Enabled={poke.enabled}, PokeDepth={poke.pokeDepth}, AttachTransform={poke.attachTransform != null}");
            }
        }

        // XR Hands subsystem
        var handsSubsystem = XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<XRHandSubsystem>();
        Debug.Log($"UIInteractionValidator: XRHandSubsystem running: {handsSubsystem != null && handsSubsystem.running}");

        // Input Action Assets (common XR actions)
        var actionAssets = FindObjectsByType<InputActionAsset>(FindObjectsSortMode.None);
        Debug.Log($"UIInteractionValidator: Found {actionAssets.Length} InputActionAsset(s)");
        foreach (var asset in actionAssets)
        {
            var selectLeft = asset.FindAction("XRI LeftHand Interaction/Select");
            var selectRight = asset.FindAction("XRI RightHand Interaction/Select");
            Debug.Log($"   '{asset.name}': Left Select = {(selectLeft != null ? "found" : "missing")}, Right Select = {(selectRight != null ? "found" : "missing")}");
        }

        Debug.Log($"UIInteractionValidator: DialogueManager.Instance present: {DialogueManager.Instance != null}");
        Debug.Log("UIInteractionValidator: Validation complete.");
    }

    // Manual poke raycast test (call via context menu when poking a button)
    [ContextMenu("Log Current Poke Raycasts")]
    public void LogCurrentPokeRaycasts()
    {
        var pokes = FindObjectsByType<XRPokeInteractor>(FindObjectsSortMode.None);
        foreach (var poke in pokes)
        {
            if (!poke.enabled) continue;
            Ray ray = new Ray(poke.transform.position, poke.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, poke.pokeDepth + 0.05f))
            {
                Debug.Log($"POKE HIT: '{hit.collider.gameObject.name}' on layer {LayerMask.LayerToName(hit.collider.gameObject.layer)} (distance {hit.distance:F3})");
            }
            else
            {
                Debug.Log($"POKE MISS from '{poke.gameObject.name}' — no hit within {poke.pokeDepth}");
            }
        }
    }
}