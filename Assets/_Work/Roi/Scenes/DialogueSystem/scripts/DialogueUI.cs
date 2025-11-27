using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

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

    private List<Button> _choiceButtons = new();
    private Coroutine _pauseRoutine;

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
            Destroy(child.gameObject);

        _choiceButtons.Clear();

        for (int i = 0; i < count; i++)
        {
            var btn = Instantiate(choiceButtonPrefab, choicesContainer).GetComponent<Button>();
            // use i directly per request (C# captures loop variable correctly in modern C#)
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => DialogueManager.Instance.OnChoiceSelected(i));
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

    // Resize & center the prefab's BoxCollider to match its RectTransform (assumes prefab already has BoxCollider)
    private void SyncColliderForButton(Button btn)
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
        bc.center = new Vector3(centerX, centerY, 0f);

        Debug.Log($"SyncColliderForButton('{go.name}') rect={width:F1}x{height:F1} -> collider size={bc.size} center={bc.center}");
    }
}
