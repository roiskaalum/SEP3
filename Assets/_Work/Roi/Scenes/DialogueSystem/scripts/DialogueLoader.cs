// DialogueLoader.cs
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public static class DialogueLoader
{
    /// <summary>
    /// Load from JSON string (uses the DialogueNodeConverter).
    /// </summary>
    public static DialogueData LoadFromJsonString(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogError("DialogueLoader: json string is null or empty.");
            return null;
        }

        var settings = new JsonSerializerSettings
        {
            Converters = { new DialogueNodeConverter() },
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        try
        {
            var data = JsonConvert.DeserializeObject<DialogueData>(json, settings);
            if (data == null || data.nodes == null)
            {
                Debug.LogError("DialogueLoader: parsed DialogueData is null or has no nodes.");
                return null;
            }
            Debug.Log($"DialogueLoader: loaded {data.nodes.Count} nodes.");
            return data;
        }
        catch (JsonException je)
        {
            Debug.LogError($"DialogueLoader: JSON parse error: {je.Message}");
            return null;
        }
    }

    /// <summary>
    /// Load from a file inside StreamingAssets (path = "dialogue_demo.json")
    /// </summary>
    public static DialogueData LoadFromStreamingAssets(string filename)
    {
        string path = Path.Combine(Application.streamingAssetsPath, filename);
        if (!File.Exists(path))
        {
            Debug.LogError($"DialogueLoader: file not found: {path}");
            return null;
        }
        string json = File.ReadAllText(path);
        return LoadFromJsonString(json);
    }
}
