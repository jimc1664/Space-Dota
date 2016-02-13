using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour {

    public Transform mainMenu, optionsMenu, graphicsMenu, controlsMenu;

    [SerializeField]
    private Slider AASlider;

    [SerializeField]
    private Text AAValue;

    [SerializeField]
    private Dropdown Resolution;

    public bool isFullScreen = false;

    public void Update()
    {
        ChangeResolution();
    }

    public void ChangeAA()
    {
        switch ((int)AASlider.value)
        {
            case 00:
                PlayerPrefs.SetInt("AntiAliasing", 0);
                QualitySettings.antiAliasing = 0;
                AASlider.value = 0;
                AAValue.text = "Disabled";
                break;

            case 01:
                PlayerPrefs.SetInt("AntiAliasing", 2);
                QualitySettings.antiAliasing = 2;
                AASlider.value = 2;
                AAValue.text = "2x Sampling";
                break;

            case 02:
                PlayerPrefs.SetInt("AntiAliasing", 4);
                QualitySettings.antiAliasing = 4;
                AASlider.value = 4;
                AAValue.text = "4x Sampling";
                break;

            case 03:
                PlayerPrefs.SetInt("AntiAliasing", 8);
                QualitySettings.antiAliasing = 8;
                AASlider.value = 8;
                AAValue.text = "8x Sampling";
                break;
        }
    }

    public void ChangeResolution()
    {
      if(Resolution.onValueChanged.Equals(true))
        {
            if (Resolution.options[1].text == "1920x1080")
            {
                Screen.SetResolution(1920, 1080, isFullScreen);
            }
            else if (Resolution.options[1].text == "1600x900")
            {
                Screen.SetResolution(1600, 900, isFullScreen);
            }
            else if (Resolution.options[1].text == "1366x768")
            {
                Screen.SetResolution(1366, 768, isFullScreen);
            }
            else if (Resolution.options[1].text == "800x600")
            {
                Screen.SetResolution(800, 600, isFullScreen);
            }
        }
    

        // Screen.SetResolution(width, height, isFullScreen);
    }

    public void LoadScene(string name)
    {
        SceneManager.LoadScene(name);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void OptionsMenu(bool clicked)
    {
        if (clicked)
        {
            optionsMenu.gameObject.SetActive(clicked);
            mainMenu.gameObject.SetActive(true);
            graphicsMenu.gameObject.SetActive(false);
            controlsMenu.gameObject.SetActive(false);
        }
        
    }

    public void ReturnToMain(bool clicked)
    {
        if (clicked)
        {
            optionsMenu.gameObject.SetActive(false);
            mainMenu.gameObject.SetActive(true);
            graphicsMenu.gameObject.SetActive(false);
            controlsMenu.gameObject.SetActive(false);
        }
            
    }

    public void GraphicsMenu (bool clicked)
    {
        if (clicked)
        {
            optionsMenu.gameObject.SetActive(false);
            mainMenu.gameObject.SetActive(false);
            graphicsMenu.gameObject.SetActive(true);
            controlsMenu.gameObject.SetActive(false);
        }

    }

    public void ControlsMenu(bool clicked)
    {
        if (clicked)
        {
            optionsMenu.gameObject.SetActive(false);
            mainMenu.gameObject.SetActive(false);
            graphicsMenu.gameObject.SetActive(false);
            controlsMenu.gameObject.SetActive(true);
        }

    }

    public void EnableFullScreen(bool isFullScreen)
    {
        isFullScreen = !isFullScreen;
    }
}

