using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    [Header("Data")]
    public TextAsset dialogueTextAsset;
    public string streamingAssetsFilename = "dialogue_demo.json";

    [Header("Typewriter")]
    public float typeDelay = 0.03f;

    [Header("Animation")]
    [Tooltip("Reference to AnimationTrigger script (optional - will find automatically)")]
    public AnimationTrigger animationTrigger;

    private DialogueInterpreter _interpreter = new DialogueInterpreter();
    private DialogueUIForceClick _ui;
    private Coroutine _typeRoutine;
    private bool _isTyping;
    private bool _startDialogueImmediately = false; // Changed to false - wait for AnimationTrigger

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        _ui = FindFirstObjectByType<DialogueUIForceClick>();
        
        // Find AnimationTrigger if not assigned
        if (animationTrigger == null)
        {
            animationTrigger = FindFirstObjectByType<AnimationTrigger>();
            if (animationTrigger == null)
            {
                Debug.Log("Warning(DialogueManager: AnimationTrigger not found in scene.)");
            }
        }
    }

    private void Start()
    {
        DialogueData data = null;
        if (dialogueTextAsset != null)
            data = DialogueLoader.LoadFromJsonString(dialogueTextAsset.text);
        else
            data = DialogueLoader.LoadFromStreamingAssets(streamingAssetsFilename);

        if (data == null)
        {
            Debug.LogError("DialogueManager: Failed to load dialogue data.");
            return;
        }

        _interpreter.LoadFromData(data);
        
        // DON'T display immediately - AnimationTrigger will call DisplayCurrentNode() when ready
        if (_startDialogueImmediately)
        {
            DisplayCurrentNode();
        }
    }

    private void Update()
    {
        if (InputRouter.Instance == null) return;

        if (InputRouter.Instance.NextPressed)
        {
            if (_isTyping)
            {
                FinishTyping();
            }
            else if (!_isTyping)
            {
                Advance();
            }
        }

        if (!_isTyping && _interpreter.GetCurrentNodeType() == DialogueNodeType.Choice)
        {
            if (InputRouter.Instance.GetChoicePressed(1)) OnChoiceSelected(0);
            if (InputRouter.Instance.GetChoicePressed(2)) OnChoiceSelected(1);
            if (InputRouter.Instance.GetChoicePressed(3)) OnChoiceSelected(2);
        }
    }

    private void TryChoose(int index)
    {
        Debug.Log("Trying to choose index: " + index);
        var node = _interpreter.GetCurrentNode();
        if (!(node is DialogueChoiceNode)) return;

        _interpreter.Choose(index);
        DisplayCurrentNode();
    }

    public void DisplayCurrentNode()
    {
        var node = _interpreter.GetCurrentNode();
        Debug.Log("Node: " + node);
        if (node == null)
        {
            _ui.ShowDialogue("", "Dialogue finished.");
            return;
        }

        switch (node.type)
        {
            case DialogueNodeType.Dialogue:
                if (node is DialogueLineNode lineNode)
                {
                    if (_typeRoutine != null) StopCoroutine(_typeRoutine);
                    Debug.Log(lineNode.speaker);
                    Debug.Log(lineNode.text);
                    _typeRoutine = StartCoroutine(Typewriter(lineNode.speaker, lineNode.text));
                }
                else
                {
                    Debug.LogError("Expected DialogueLineNode but got base DialogueNode!");
                }
                break;

            case DialogueNodeType.Choice:
                if (node is DialogueChoiceNode choiceNode)
                {
                    _ui.ShowChoices(choiceNode.choices);
                }
                break;

            case DialogueNodeType.Pause:
                if (node is DialoguePauseNode pauseNode)
                {
                    _ui.ShowPause(pauseNode.duration);
                }
                break;
        }
    }

    private IEnumerator Typewriter(string speaker, string text)
    {
        _isTyping = true;
        _ui.ShowChoices(null);
        _ui.ShowDialogue(speaker, "");

        string currentText = "";
        foreach (char c in text)
        {
            currentText += c;
            _ui.ShowDialogue(speaker, currentText);
            yield return new WaitForSeconds(typeDelay);
        }

        _ui.ShowDialogue(speaker, text);
        _isTyping = false;
    }

    private void FinishTyping()
    {
        if (_typeRoutine != null) StopCoroutine(_typeRoutine);
        _isTyping = false;
        _ui.ShowDialogue(_interpreter.GetSpeaker(), _interpreter.GetText());
    }

    private void Advance()
    {
        Debug.Log("DialogueManager.Advance called.");
        if (_interpreter.GetCurrentNodeType() == DialogueNodeType.Choice ||
            _interpreter.GetCurrentNodeType() == DialogueNodeType.Pause)
            return;

        _interpreter.Continue();
        DisplayCurrentNode();
    }

    public void OnNextPressed()
    {
        if (_isTyping) FinishTyping();
        else Advance();
    }

    public void OnChoiceSelected(int index)
    {
        Debug.Log($"[DialogueManager] Choice {index} selected");
        
        // Trigger Lars' animation based on choice (if this is the first choice)
        if (animationTrigger != null)
        {
            // You can add logic here to check if this is the specific choice node
            // For now, assumes first choice triggers Lars' animation
            animationTrigger.SetLarsEmotionalState(index);
            
            // Optionally activate background NPCs when choice is made
            animationTrigger.ActivateBackgroundNPCs();
        }
        
        TryChoose(index);
    }

    public void OnPauseComplete()
    {
        _interpreter.Continue();
        DisplayCurrentNode();
    }
}
