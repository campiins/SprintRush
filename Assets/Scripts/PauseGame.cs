using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseGame : MonoBehaviour
{
    [SerializeField] GameObject optionsMenu; // Objeto del menu de pausa (opciones)

    public List<string> objectsToActivate = new List<string> // Lista de nombres de los objetos a activar al reanudar el juego
    {
        "Laps",
        "Position",
        "Times",
        "Speed",
        "AudioCountdown"
    };

    void Start()
    {
        Resume();    
    }

    public void Pause()
    {
        Debug.Log("Pausar...");

        Time.timeScale = 0; // Pausar tiempo
        AudioListener.pause = true; // Pausar audio

        // Desactivar UI
        Canvas canvasWithUIManager = FindObjectOfType<UIManager>().GetComponentInParent<Canvas>();
        Canvas minimap = GameObject.FindGameObjectWithTag("Minimap").GetComponentInChildren<Canvas>();

        if (canvasWithUIManager != null)
        {
            foreach (Transform child in canvasWithUIManager.transform)
            {
                if (child.gameObject.activeSelf == true)
                    child.gameObject.SetActive(false); // Desactivar todos los hijos del Canvas
            }
        }
        minimap.gameObject.SetActive(false); // Desactivar minimapa

        // Activar menu de pausa (opciones)
        optionsMenu.SetActive(true);
    }

    public void Resume()
    {
        Debug.Log("Reanudar...");

        // Desactivar menu de pausa (opciones)
        optionsMenu.SetActive(false);

        // Activar UI
        Canvas canvasWithUIManager = FindObjectOfType<UIManager>().GetComponentInParent<Canvas>();
        GameObject minimap = GameObject.FindGameObjectWithTag("Minimap");
        Canvas minimapCanvas = minimap.GetComponentsInChildren<Canvas>(true)[0]; // Encontrar canvas entre los hijos inactivos

        if (canvasWithUIManager != null)
        {
            foreach (Transform child in canvasWithUIManager.transform)
            {
                string childName = child.gameObject.name;
                if (objectsToActivate.Contains(childName))
                {
                    child.gameObject.SetActive(true); // Activar solo los hijos en la lista
                }
            }
        }

        minimapCanvas.gameObject.SetActive(true); // Activar minimapa

        Time.timeScale = 1; // Reanudar tiempo
        AudioListener.pause = false; // Reanudar audio
    }
}
