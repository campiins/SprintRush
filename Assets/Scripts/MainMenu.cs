using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject mainMenu;
    public GameObject optionsMenu;

    private void Awake()
    {
        mainMenu.SetActive(true);
    }

    public void Race()
    {
        Debug.Log("Carrera...");
        PlayerPrefs.SetString("Gamemode", "Race");
        SceneManager.LoadScene("EmeraldCircuit");
    }

    public void Practice()
    {
        Debug.Log("Práctica...");
        PlayerPrefs.SetString("Gamemode", "Practice");
        SceneManager.LoadScene("EmeraldCircuit");
    }

    public void Options()
    {
        Debug.Log("Opciones...");
        mainMenu.SetActive(false);
        optionsMenu.SetActive(true);
    }

    public void Exit()
    {
        Debug.Log("Salir...");
        Application.Quit();
    }
}
