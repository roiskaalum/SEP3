using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ChangeScne()
    {
        SceneManager.LoadScene(1);
    }

    public void ChangeScne2()
    {
        SceneManager.LoadScene(0);
    }
}
