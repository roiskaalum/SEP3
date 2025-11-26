// Paste this into the script that appears, then delete the script file
using UnityEngine;

[CreateAssetMenu(menuName = "Rehab/Pose Library")]
public class PoseLibrary : ScriptableObject
{
    [System.Serializable]
    public class Pose
    {
        public string name;
        public float openness;     // 0 = fist, 1 = full open
        public Color color;
        public GameObject ghostHand;
    }
    public Pose[] poses;
}