using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class DialogueNodeConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => objectType == typeof(DialogueNode);

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var jo = JObject.Load(reader);
        var typeToken = jo["type"];
        string typeStr = typeToken?.ToString() ?? "Dialogue";

        // Allow either string names ("Dialogue","Choice","Pause") or numeric enum (0/1/2)
        DialogueNodeType nodeType;
        if (!Enum.TryParse(typeStr, true, out nodeType))
        {
            // try parse as int
            if (int.TryParse(typeStr, out int i) && Enum.IsDefined(typeof(DialogueNodeType), i))
                nodeType = (DialogueNodeType)i;
            else
                nodeType = DialogueNodeType.Dialogue; // fallback
        }

        DialogueNode node = nodeType switch
        {
            DialogueNodeType.Choice => new DialogueChoiceNode(),
            DialogueNodeType.Pause => new DialoguePauseNode(),
            _ => new DialogueLineNode()
        };

        // populate the chosen concrete node
        serializer.Populate(jo.CreateReader(), node);

        // ensure the type enum is correctly set (in case JSON used string or int)
        node.type = nodeType;
        return node;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var jo = JObject.FromObject(value, serializer);
        jo.WriteTo(writer);
    }
}