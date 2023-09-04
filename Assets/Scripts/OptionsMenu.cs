using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OptionsMenu : MonoBehaviour
{
    [SerializeField] GameObject mainMenu;
    [SerializeField] GameObject optionsMenu;
    [SerializeField] GameObject controlsMenu;
    [SerializeField] GameObject settingsMenu;

    public void Settings()
    {
        Debug.Log("Configuración...");
        optionsMenu.SetActive(false);
        settingsMenu.SetActive(true);
    }

    public void Controls()
    {
        Debug.Log("Controles...");
        optionsMenu.SetActive(false);
        controlsMenu.SetActive(true);
    }

    public void BackToMenu()
    {
        Debug.Log("Atrás...");
        optionsMenu.SetActive(false);
        if (mainMenu != null)
            mainMenu.SetActive(true);
    }

    public void ExitToMenu()
    {
        Debug.Log("Salir al menu...");
        SceneManager.LoadScene("MainMenu");
        AudioListener.pause = false;
    }

    public void ExitControls()
    {
        controlsMenu.SetActive(false);
        optionsMenu.SetActive(true);
    }
}
