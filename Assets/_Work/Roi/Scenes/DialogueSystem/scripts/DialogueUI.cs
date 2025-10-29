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
        foreach (Transform child in choicesContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < count; i++)
        {
            var btn = Instantiate(choiceButtonPrefab, choicesContainer).GetComponent<Button>();
            int index = i;
            btn.onClick.AddListener(() => DialogueManager.Instance.OnChoiceSelected(index));
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
            _choiceButtons[i].gameObject.SetActive(true);
            _choiceButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = choices[i].text;
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
}
