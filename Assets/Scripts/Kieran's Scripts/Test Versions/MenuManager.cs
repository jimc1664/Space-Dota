using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour {

    public Transform mainMenu, optionsMenu, graphicsMenu, controlsMenu;

    [SerializeField]
    private Dropdown qualitySettingsDropDown;

    [SerializeField]
    private Dropdown antialiasingDropDown;

    [SerializeField]
    private Toggle fullscreenToggle;

    public bool isFullScreen = false;

    public void Start()
    {
        qualitySettingsDropDown.options.Clear();

        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            qualitySettingsDropDown.options.Add(new Dropdown.OptionData(QualitySettings.names[i]));
        }
        qualitySettingsDropDown.onValueChanged.AddListener(

            delegate
            {
                SetQualityLevel();
            }
        );

        antialiasingDropDown.onValueChanged.AddListener(
            delegate
            {
                SetAntiAliasing();
            }
        );

        fullscreenToggle.onValueChanged.AddListener(

            delegate
            {
                EnableFullScreen();
            }
        );
    }

    public void Update()
    {
        
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

    private void EnableFullScreen()
    {
        isFullScreen = !isFullScreen;

        if (isFullScreen)
            Screen.SetResolution(Screen.width, Screen.height, true);
       else
            Screen.SetResolution(Screen.width, Screen.height, false);
    }

    public void SetQualityLevel()
    {
        QualitySettings.SetQualityLevel(qualitySettingsDropDown.value);
    }

    public void SetAntiAliasing()
    {
        switch (antialiasingDropDown.value)
        {
            case 0:
                QualitySettings.antiAliasing = 0;
                break;

            case 1:
                QualitySettings.antiAliasing = 2;
                break;
            case 2:
                QualitySettings.antiAliasing = 4;
                break;

            case 3:
                QualitySettings.antiAliasing = 8;
                break;
        }
    }
}

