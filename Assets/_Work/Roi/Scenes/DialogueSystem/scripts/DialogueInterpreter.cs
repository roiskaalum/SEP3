// DialogueInterpreter.cs
using System.Collections.Generic;
using UnityEngine;

public class DialogueInterpreter
{
    private DialogueData _data;
    private Dictionary<string, DialogueNode> _lookup;
    private DialogueNode _current;

    public void LoadFromData(DialogueData data)
    {
        _data = data;
        BuildLookup();
        _current = (_data.nodes != null && _data.nodes.Count > 0) ? _data.nodes[0] : null;
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<string, DialogueNode>();
        if (_data?.nodes == null) return;
        foreach (var n in _data.nodes)
        {
            if (string.IsNullOrEmpty(n.id))
            {
                Debug.LogWarning("DialogueInterpreter: node with empty id found, skipping.");
                continue;
            }
            if (_lookup.ContainsKey(n.id))
                Debug.LogWarning($"DialogueInterpreter: duplicate node id '{n.id}'");
            _lookup[n.id] = n;
        }
    }

    public DialogueNode GetCurrentNode() => _current;

    public DialogueNodeType GetCurrentNodeType() => _current?.type ?? DialogueNodeType.Dialogue;

    public string GetSpeaker() => (_current as DialogueLineNode)?.speaker;

    public string GetText() => (_current as DialogueLineNode)?.text;

    public float GetPauseDuration() => (_current as DialoguePauseNode)?.duration ?? 0f;

    public List<Choice> GetChoices() => (_current as DialogueChoiceNode)?.choices;

    public void Continue()
    {
        if (_current == null) return;
        string nextId = GetNextId(_current);
        _current = !string.IsNullOrEmpty(nextId) && _lookup.TryGetValue(nextId, out var n) ? n : null;
    }

    public DialogueNode Choose(int index)
    {
        if (!(_current is DialogueChoiceNode choiceNode))
        {
            Debug.LogWarning("DialogueInterpreter.Choose called but current node is not a Choice node.");
            return null;
        }

        if (index < 0 || index >= choiceNode.choices.Count)
        {
            Debug.LogWarning($"DialogueInterpreter.Choose invalid index {index}");
            return null;
        }
        Debug.Log("*** Choice Node Accessed with index: " + index + " ***");
        string nextId = choiceNode.choices[index].next;
        Debug.Log("*** Choice Node nextid: " + nextId + " ***");
        _current = _lookup.TryGetValue(nextId, out var next) ? next : null;
        Debug.Log("_current: " + _current);
        return _current;
    }

    private string GetNextId(DialogueNode node)
    {
        switch (node)
        {
            case DialogueLineNode dl: return dl.next;
            case DialoguePauseNode dp: return dp.next;
            default: return null;
        }
    }
}