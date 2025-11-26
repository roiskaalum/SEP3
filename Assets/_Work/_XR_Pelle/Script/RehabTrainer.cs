using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class RehabTrainer : MonoBehaviour
{
    public PoseLibrary library;
    public Slider bar;
    public Text status;
    public AudioSource sfx;
    public GameObject[] ghosts; // will be filled automatically

    int level = 0;
    float hold = 0f;
    const float NEED = 1.2f;

    void Start()
    {
        ghosts = new GameObject[library.poses.Length];
        for (int i = 0; i < library.poses.Length; i++)
        {
            ghosts[i] = Instantiate(library.poses[i].ghostHand, transform);
            ghosts[i].SetActive(i == 0);
        }
        ShowLevel(0);
    }

    void Update()
    {
        float open = 1f - Grip();
        var p = library.poses[level];

        // visual
        bar.value = Mathf.Clamp01(open / p.openness);
        bar.fillRect.GetComponent<Image>().color = Color.Lerp(Color.red, Color.green, bar.value);

        // ghost
        foreach (var g in ghosts) g.SetActive(false);
        ghosts[level].SetActive(true);
        ghosts[level].GetComponent<Renderer>().material.color =
            Color.Lerp(Color.white * 0.4f, p.color, bar.value);

        // success
        if (open >= p.openness - 0.05f)
        {
            hold += Time.deltaTime;
            status.text = $"Hold {p.name}... {hold:0.0}s";
            if (hold >= NEED) Next();
        }
        else
        {
            hold = 0f;
            status.text = $"Open to {p.name}";
        }
    }

    void Next()
    {
        sfx.Play();
        level++;
        if (level >= library.poses.Length)
        {
            status.text = "ALL DONE!";
            enabled = false;
            return;
        }
        hold = 0f;
        ShowLevel(level);
    }

    void ShowLevel(int i)
    {
        status.text = $"Level {i + 1}: {library.poses[i].name}";
        bar.value = 0f;
    }

    float Grip()
    {
        var dev = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        dev.TryGetFeatureValue(CommonUsages.grip, out float g);
        return g;
    }
}