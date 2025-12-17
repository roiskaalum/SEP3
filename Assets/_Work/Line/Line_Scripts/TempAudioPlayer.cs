using UnityEngine;

public class TempAudioPlayer : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void testLyd()
    {
        GetComponent<AudioManager>().Play("test");

    }

}
