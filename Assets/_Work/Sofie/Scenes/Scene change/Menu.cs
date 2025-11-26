using UnityEngine;

public class Menu : MonoBehaviour
{
    public GameObject menuUI;

    bool isMenuOpen = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        menuUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ToggleMenu()
    {
        if (isMenuOpen)
        {
            menuUI.SetActive(false);
            isMenuOpen = false;
        }
        else
        {
            menuUI.SetActive(true);
            isMenuOpen = true;
        }
    }
    
}
