using System.Collections.Generic;

public enum DialogueNodeType { Dialogue, Choice, Pause }

public abstract class DialogueNode
{
    public string id;
    public DialogueNodeType type;
}

public class DialogueLineNode : DialogueNode
{
    public string speaker;
    public string text;
    public string next;   // id of next node (optional)
}

public class DialogueChoiceNode : DialogueNode
{
    public List<Choice> choices;
}

public class DialoguePauseNode : DialogueNode
{
    public float duration;
    public string next;
}

public class Choice
{
    public string text;
    public string next;
}

public class DialogueData
{
    public List<DialogueNode> nodes;
}