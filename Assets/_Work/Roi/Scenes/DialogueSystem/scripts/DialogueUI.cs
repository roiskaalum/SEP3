using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.EventSystems;
using System;

public class DialogueUI : MonoBehaviour
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

    [Header("Debug")]
    [Tooltip("Show a translucent cube in-game representing each button's BoxCollider")]
    public bool debugShowColliders = false;
    [Tooltip("Draw poke rays from active XRPokeInteractors and log hits/misses")]
    public bool debugShowPokeRays = false;
    [Tooltip("Also log misses from poke rays")]
    public bool debugLogPokeMisses = false;
    [Tooltip("When true, only log a MISS when the interactor previously had a HIT (avoids continuous miss spam)")]
    public bool debugLogPokeMissesOnChange = true; // <-- new behavior default: avoid spam
    [Tooltip("Auto-invoke button click when a poke ray hits a choice (test only)")]
    public bool debugAutoClickOnPokeHit = false;
    [Tooltip("Minimum seconds between auto-clicks on the same button (safety)")]
    public float debugAutoClickCooldown = 0.5f;
    [Tooltip("Minimum seconds between logging repeated HITs for the same interactor+target")]
    public float debugPokeHitLogCooldown = 0.2f;
    [Tooltip("Color used for collider visuals")]
    public Color debugColliderColor = new Color(0f, 1f, 0f, 0.25f);
    [Tooltip("Color used for poke ray debug draw")]
    public Color debugPokeRayColor = Color.cyan;

    private List<Button> _choiceButtons = new();
    private List<GameObject> _debugColliderGOs = new();
    private Coroutine _pauseRoutine;

    // safety: record last auto-click time per button instance id
    private readonly Dictionary<int, float> _lastAutoClickTime = new();

    // track last hit state per interactor to avoid miss-spam
    private readonly Dictionary<int, bool> _pokeLastHadHit = new();

    // track last logged HIT time per interactor+collider to reduce HIT spam
    private readonly Dictionary<long, float> _pokeLastHitLogTime = new();

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
            Debug.Log("Destroying existing choice button: " + child.gameObject.name);
            Destroy(child.gameObject);
        }

        _choiceButtons.Clear();

        // remove any previous debug visuals list entries
        foreach (var go in _debugColliderGOs)
            if (go != null) Destroy(go);
        _debugColliderGOs.Clear();

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
            _debugColliderGOs.Add(null); // placeholder for debug visual
            Debug.Log("Created choice button: " + btn.gameObject.name + $" (index={i})");
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
        Debug.Log(choices == null ? "ShowChoices: null choices" : $"ShowChoices: {choices.Count} choices");
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
            SyncColliderForButton(_choiceButtons[i], i);
        }
    }

    public void HideChoices()
    {
        foreach (var btn in _choiceButtons)
            if(btn != null && btn.gameObject != null)
                btn.gameObject.SetActive(false);

        if (debugShowColliders)
        {
            for (int i = 0; i < _debugColliderGOs.Count; i++)
            {
                if (_debugColliderGOs[i] != null)
                    _debugColliderGOs[i].SetActive(false);
            }
        }
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

    // Resize & center the prefab's BoxCollider to match its RectTransform (assumes prefab already has BoxCollider)
    // This overload also manages the debug visual (if enabled).
    private void SyncColliderForButton(Button btn, int index)
    {
        if (btn == null) return;
        var go = btn.gameObject;
        var rt = go.GetComponent<RectTransform>();
        var bc = go.GetComponent<BoxCollider>();

        if (rt == null)
        {
            Debug.LogWarning($"SyncColliderForButton: missing RectTransform on '{go.name}'");
            return;
        }

        if (bc == null)
        {
            Debug.LogWarning($"SyncColliderForButton: missing BoxCollider on '{go.name}'");
            return;
        }

        var rect = rt.rect;
        float width = rect.width;
        float height = rect.height;

        // preserve existing depth if present, otherwise use a small default
        float depth = Mathf.Max(0.005f, Mathf.Abs(bc.size.z) > 0f ? Mathf.Abs(bc.size.z) : 0.01f);

        bc.size = new Vector3(width, height, depth);

        var pivot = rt.pivot;
        float centerX = width * (0.5f - pivot.x);
        float centerY = height * (pivot.y - 0.5f);
        // keep collider centered on the UI plane; small forward bias may help poke detection if needed
        bc.center = new Vector3(centerX, centerY, -0.002f);

        Debug.Log($"SyncColliderForButton('{go.name}') rect={width:F1}x{height:F1} -> collider size={bc.size} center={bc.center}");

        // Create or update debug visual
        if (debugShowColliders)
            CreateOrUpdateDebugVisual(btn, bc, index);
        else
            DestroyDebugVisual(index);
    }

    private void CreateOrUpdateDebugVisual(Button btn, BoxCollider bc, int index)
    {
        if (_debugColliderGOs == null)
            _debugColliderGOs = new List<GameObject>();

        // Ensure list is large enough
        while (_debugColliderGOs.Count <= index)
            _debugColliderGOs.Add(null);

        GameObject vis = _debugColliderGOs[index];

        if (vis == null)
        {
            // create a cube mesh renderer without collider
            vis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            // remove the autogenerated collider
            var autoCol = vis.GetComponent<Collider>();
            if (autoCol != null) DestroyImmediate(autoCol);
            vis.name = $"{btn.gameObject.name}_DEBUG_ColliderVis";
            vis.transform.SetParent(btn.transform, false);

            // create simple unlit material for clarity
            Shader shader = Shader.Find("Unlit/Color");
            Material mat = null;
            if (shader != null)
            {
                mat = new Material(shader) { color = debugColliderColor };
            }
            else
            {
                // fallback to standard transparent
                mat = new Material(Shader.Find("Standard"));
                Color c = debugColliderColor;
                c.a = debugColliderColor.a;
                mat.color = c;
                mat.SetFloat("_Mode", 3); // transparent
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
            }

            var mr = vis.GetComponent<MeshRenderer>();
            if (mr != null)
                mr.sharedMaterial = mat;

            _debugColliderGOs[index] = vis;
        }

        // Set local position & scale to match BoxCollider (note BoxCollider uses local space)
        vis.transform.localPosition = bc.center;
        vis.transform.localRotation = Quaternion.identity;
        vis.transform.localScale = bc.size;

        vis.SetActive(btn.gameObject.activeSelf);
    }

    private void DestroyDebugVisual(int index)
    {
        if (_debugColliderGOs == null) return;
        if (index < 0 || index >= _debugColliderGOs.Count) return;
        if (_debugColliderGOs[index] != null)
        {
            Destroy(_debugColliderGOs[index]);
            _debugColliderGOs[index] = null;
        }
    }

    private void OnDisable()
    {
        // clean up debug visuals
        if (_debugColliderGOs != null)
        {
            for (int i = 0; i < _debugColliderGOs.Count; i++)
            {
                if (_debugColliderGOs[i] != null)
                    Destroy(_debugColliderGOs[i]);
            }
            _debugColliderGOs.Clear();
        }
    }

    private void Update()
    {
        if (!debugShowPokeRays) return;

        // Find active poke interactors and draw rays
        var pokeInteractors = FindObjectsByType<XRPokeInteractor>(FindObjectsSortMode.None);
        foreach (var poke in pokeInteractors)
        {
            if (!poke.enabled) continue;

            int pid = poke.GetInstanceID();
            var attach = poke.attachTransform != null ? poke.attachTransform : poke.transform;
            Ray ray = new Ray(attach.position, attach.forward);
            float maxDistance = poke.pokeDepth + 0.05f;

            Debug.DrawRay(ray.origin, ray.direction * maxDistance, debugPokeRayColor);

            bool hadHitThisFrame = Physics.Raycast(ray, out RaycastHit hit, maxDistance);

            if (hadHitThisFrame)
            {
                // throttle HIT logging per interactor+target to reduce spam
                int colliderId = hit.collider != null ? hit.collider.GetInstanceID() : 0;
                long key = ((long)pid << 32) | (uint)colliderId;
                _pokeLastHitLogTime.TryGetValue(key, out float lastLogTime);

                bool prevHadHit = false;
                _pokeLastHadHit.TryGetValue(pid, out prevHadHit);

                bool shouldLogHit = false;
                if (!prevHadHit)
                {
                    // changed from no-hit -> hit, log immediately
                    shouldLogHit = true;
                }
                else if (Time.unscaledTime - lastLogTime >= debugPokeHitLogCooldown)
                {
                    // we previously hit something; allow periodic log for the same interactor+target
                    shouldLogHit = true;
                }

                if (shouldLogHit)
                {
                    Debug.Log($"Poke debug HIT: '{poke.gameObject.name}' -> '{hit.collider.gameObject.name}' (distance {hit.distance:F3})");
                    _pokeLastHitLogTime[key] = Time.unscaledTime;
                }

                // check if the hit object is a choice button or child of one
                var hitBtn = hit.collider.GetComponent<Button>();
                if (hitBtn == null)
                    hitBtn = hit.collider.transform.GetComponentInParent<Button>();

                if (hitBtn != null)
                {
                    int idx = _choiceButtons.IndexOf(hitBtn);
                    // throttle the "hit choice button index" line using the same key logic
                    if (idx >= 0)
                    {
                        long btnKey = ((long)pid << 32) | (uint)hitBtn.gameObject.GetInstanceID();
                        _pokeLastHitLogTime.TryGetValue(btnKey, out float lastBtnLog);
                        bool shouldLogBtnHit = false;
                        if (!prevHadHit) shouldLogBtnHit = true;
                        else if (Time.unscaledTime - lastBtnLog >= debugPokeHitLogCooldown) shouldLogBtnHit = true;

                        if (shouldLogBtnHit)
                        {
                            Debug.Log($"Poke debug: hit choice button index={idx}");
                            _pokeLastHitLogTime[btnKey] = Time.unscaledTime;
                        }
                    }
                    else
                    {
                        // not in list - still optionally show once per cooldown
                        long btnKey = ((long)pid << 32) | (uint)hitBtn.gameObject.GetInstanceID();
                        _pokeLastHitLogTime.TryGetValue(btnKey, out float lastBtnLog);
                        if (!prevHadHit || Time.unscaledTime - lastBtnLog >= debugPokeHitLogCooldown)
                        {
                            Debug.Log($"Poke debug: hit Button '{hitBtn.gameObject.name}' but it is not in _choiceButtons list");
                            _pokeLastHitLogTime[btnKey] = Time.unscaledTime;
                        }
                    }

                    // If debugAutoClickOnPokeHit is enabled, simulate a click (test only)
                    if (debugAutoClickOnPokeHit)
                    {
                        int id = hitBtn.gameObject.GetInstanceID();
                        float last;
                        _lastAutoClickTime.TryGetValue(id, out last);
                        if (Time.unscaledTime - last >= debugAutoClickCooldown)
                        {
                            _lastAutoClickTime[id] = Time.unscaledTime;
                            // invoke OnClick listeners directly
                            try
                            {
                                Debug.Log($"Poke debug: Auto-invoking OnClick on '{hitBtn.gameObject.name}'");
                                // First try pointer event execution (proper EventSystem flow)
                                var ev = EventSystem.current;
                                if (ev != null)
                                {
                                    var ped = new PointerEventData(ev);
                                    ExecuteEvents.Execute(hitBtn.gameObject, ped, ExecuteEvents.pointerDownHandler);
                                    ExecuteEvents.Execute(hitBtn.gameObject, ped, ExecuteEvents.pointerClickHandler);
                                    ExecuteEvents.Execute(hitBtn.gameObject, ped, ExecuteEvents.pointerUpHandler);
                                }

                                // Also call the Button.onClick as a fallback
                                hitBtn.onClick?.Invoke();
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"Poke debug: Exception while auto-clicking: {ex}");
                            }
                        }
                    }
                }

                // mark that this interactor had a hit this frame
                _pokeLastHadHit[pid] = true;
            }
            else
            {
                // decide whether to log a MISS:
                // - if debugLogPokeMisses is false => never log
                // - if debugLogPokeMissesOnChange is true => log only when last frame was a hit (change to MISS)
                // - otherwise log every frame (legacy behavior)
                bool prevHadHit = false;
                _pokeLastHadHit.TryGetValue(pid, out prevHadHit);

                if (debugLogPokeMisses)
                {
                    if (debugLogPokeMissesOnChange)
                    {
                        // log only when changing from hit -> miss
                        if (prevHadHit)
                            Debug.Log($"Poke debug MISS (changed) from '{poke.gameObject.name}' within depth {poke.pokeDepth}");
                    }
                    else
                    {
                        // legacy: spammy log every frame
                        Debug.Log($"Poke debug MISS from '{poke.gameObject.name}' within depth {poke.pokeDepth}");
                    }
                }

                // update state to reflect current (no hit)
                _pokeLastHadHit[pid] = false;
            }
        }
    }
}
