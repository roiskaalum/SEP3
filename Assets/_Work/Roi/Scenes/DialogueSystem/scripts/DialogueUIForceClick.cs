using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.EventSystems;
using System;

public class DialogueUIForceClick : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;
    public Button nextButton;
    public Transform choicesContainer;
    public GameObject choiceButtonPrefab;
    public GameObject pausePanel;
    public TextMeshProUGUI pauseText;
    public Image pauseProgressBar;

    [Header("Force Click Settings")]
    [Tooltip("Minimum seconds between auto-clicks on the same button (safety)")]
    public float autoClickCooldown = 0.5f;

    private List<Button> _choiceButtons = new();
    private Coroutine _pauseRoutine;

    // safety: record last auto-click time per button instance id
    private readonly Dictionary<int, float> _lastAutoClickTime = new();

    private void Start()
    {
        nextButton.onClick.AddListener(() => DialogueManager.Instance.OnNextPressed());
        GenerateChoiceButtons(3);
        HideChoices();
        pausePanel.SetActive(false);
    }

    private void GenerateChoiceButtons(int count)
    {
        // remove existing children and clear pool
        foreach (Transform child in choicesContainer)
        {
            Destroy(child.gameObject);
        }

        _choiceButtons.Clear();

        for (int i = 0; i < count; i++)
        {
            var btnGO = Instantiate(choiceButtonPrefab, choicesContainer);
            var btn = btnGO.GetComponent<Button>();
            if (btn == null)
            {
                Debug.LogWarning($"Generated choice prefab does not contain a Button component: {btnGO.name}");
                continue;
            }

            // Set ChoiceButton.index if component exists
            var choiceButtonComponent = btn.GetComponent<ChoiceButton>();
            if (choiceButtonComponent != null)
                choiceButtonComponent.index = i;

            // Remove prefab listeners and add our listener with captured index
            btn.onClick.RemoveAllListeners();
            int idx = i;
            btn.onClick.AddListener(() =>
            {
                Debug.Log($"Choice button clicked (listener) index={idx}");
                DialogueManager.Instance?.OnChoiceSelected(idx);
            });

            // Also add ChoiceButton.Press if the component exists (safe-guard)
            if (choiceButtonComponent != null)
            {
                btn.onClick.AddListener(choiceButtonComponent.Press);
            }

            btn.gameObject.SetActive(false);
            _choiceButtons.Add(btn);
        }
    }

    public void ShowDialogue(string speaker, string text)
    {
        pausePanel.SetActive(false);
        HideChoices();
        speakerText.text = speaker;
        dialogueText.text = text;
        nextButton.gameObject.SetActive(true);
    }

    public void ShowChoices(List<Choice> choices)
    {
        HideChoices();
        nextButton.gameObject.SetActive(false);

        if (choices == null) return;

        int displayCount = Mathf.Min(choices.Count, _choiceButtons.Count);

        for (int i = 0; i < displayCount; i++)
        {
            var btn = _choiceButtons[i];
            if(btn == null)
            {
                Debug.LogWarning($"ShowChoices: missing button for index {i}");
                continue;
            }
            btn.gameObject.SetActive(true);
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = choices[i].text;
        }

        // Force layout so RectTransforms are final, then sync colliders
        var containerRect = choicesContainer.GetComponent<RectTransform>();
        if (containerRect != null)
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);

        for (int i = 0; i < displayCount; i++)
        {
            SyncColliderForButton(_choiceButtons[i]);
        }
    }

    public void HideChoices()
    {
        foreach (var btn in _choiceButtons)
            if(btn != null && btn.gameObject != null)
                btn.gameObject.SetActive(false);
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

    // Resize & center the prefab's BoxCollider to match its RectTransform
    private void SyncColliderForButton(Button btn)
    {
        if (btn == null) return;
        var go = btn.gameObject;
        var rt = go.GetComponent<RectTransform>();
        var bc = go.GetComponent<BoxCollider>();

        if (rt == null || bc == null) return;

        var rect = rt.rect;
        float width = rect.width;
        float height = rect.height;

        // preserve existing depth if present, otherwise use a small default
        float depth = Mathf.Max(0.005f, Mathf.Abs(bc.size.z) > 0f ? Mathf.Abs(bc.size.z) : 0.01f);

        bc.size = new Vector3(width, height, depth);

        var pivot = rt.pivot;
        float centerX = width * (0.5f - pivot.x);
        float centerY = height * (pivot.y - 0.5f);
        bc.center = new Vector3(centerX, centerY, -0.002f);
    }

    private void Update()
    {
        // Find active poke interactors and check for hits on choice buttons
        var pokeInteractors = FindObjectsByType<XRPokeInteractor>(FindObjectsSortMode.None);
        foreach (var poke in pokeInteractors)
        {
            if (!poke.enabled) continue;

            var attach = poke.attachTransform != null ? poke.attachTransform : poke.transform;
            Ray ray = new Ray(attach.position, attach.forward);
            float maxDistance = poke.pokeDepth + 0.05f;

            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                // check if the hit object is a choice button or child of one
                var hitBtn = hit.collider.GetComponent<Button>();
                if (hitBtn == null)
                    hitBtn = hit.collider.transform.GetComponentInParent<Button>();

                if (hitBtn != null && _choiceButtons.Contains(hitBtn))
                {
                    // Apply cooldown to prevent spam clicking
                    int id = hitBtn.gameObject.GetInstanceID();
                    float last;
                    _lastAutoClickTime.TryGetValue(id, out last);
                    
                    if (Time.unscaledTime - last >= autoClickCooldown)
                    {
                        _lastAutoClickTime[id] = Time.unscaledTime;
                        
                        try
                        {
                            Debug.Log($"Force-clicking choice button: '{hitBtn.gameObject.name}'");
                            
                            // Execute through EventSystem for proper event flow
                            var ev = EventSystem.current;
                            if (ev != null)
                            {
                                var ped = new PointerEventData(ev);
                                ExecuteEvents.Execute(hitBtn.gameObject, ped, ExecuteEvents.pointerDownHandler);
                                ExecuteEvents.Execute(hitBtn.gameObject, ped, ExecuteEvents.pointerClickHandler);
                                ExecuteEvents.Execute(hitBtn.gameObject, ped, ExecuteEvents.pointerUpHandler);
                            }

                            // Also invoke the Button's onClick listeners
                            hitBtn.onClick?.Invoke();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"Exception while force-clicking button: {ex}");
                        }
                    }
                }
            }
        }
    }
}
