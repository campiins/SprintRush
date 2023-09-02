using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LapCounter : MonoBehaviour
{
    public float totalRaceTime;
    public float totalTimerTimestamp;
    public float lapTimerTimestamp; // Tiempo en el que se inicia una vuelta.
    public float currentLapTime; // Tiempo transcurrido en la vuelta actual.
    public float lastLapTime; // Tiempo de la última vuelta
    public float bestLapTime = float.MaxValue; // Mejor tiempo de vuelta
    [HideInInspector] public float averageLapTime; // Tiempo de vuelta promedio

    public List<float> lapTimes = new List<float>(); // Lista de tiempos de vueltas completadas

    private bool raceOngoing;
    RaceManager raceManager;

    void Start()
    {
        raceManager = FindObjectOfType<RaceManager>();
        bestLapTime = float.MaxValue;
        if (gameObject.CompareTag("Player"))
        {
            // Recuperar el mejor tiempo de vuelta guardado
            if (PlayerPrefs.HasKey("BestLapTime") && FindObjectOfType<GameModeSettings>().GetGameMode() == GameModeSettings.Gamemode.Practice)
            {
                bestLapTime = PlayerPrefs.GetFloat("BestLapTime");
            }
        }
    }

    void Update()
    {
        if (raceOngoing)
        {
            totalRaceTime = totalTimerTimestamp > 0 ? Time.time - totalTimerTimestamp : 0;
            currentLapTime = lapTimerTimestamp > 0 ? Time.time - lapTimerTimestamp : 0;
        }
    }

    public void StartLap()
    {
        lapTimerTimestamp = Time.time;
        if (lapTimes.Count == 0) // si empieza la primera vuelta
        {
            raceOngoing = true;
            totalTimerTimestamp = lapTimerTimestamp;
        }
    } 

    public void EndLap()
    {
        lastLapTime = Time.time - lapTimerTimestamp; // Tiempo de la ultima vuelta
        bestLapTime = Mathf.Min(bestLapTime, lastLapTime); // Mejor tiempo de vuelta de la sesion
        lapTimes.Add(lastLapTime); // Añadir tiempo de vuelta a la lista de tiempos
        averageLapTime = CalculateAverageLapTime(); // Calcular tiempo de vuelta promedio
        if (lapTimes.Count == raceManager.totalLaps)
        {
            raceOngoing = false;
        }

        if (gameObject.CompareTag("Player"))
        {
            // Almacenar el mejor tiempo de vuelta en PlayerPrefs
            PlayerPrefs.SetFloat("BestLapTime", bestLapTime);
            PlayerPrefs.Save();
        }
    }

    public float CalculateAverageLapTime()
    {
        if (lapTimes.Count == 0)
        {
            return 0;
        }

        float totalLapTime = 0;
        foreach (float lapTime in lapTimes)
        {
            totalLapTime += lapTime;
        }

        return totalLapTime / lapTimes.Count;
    }

    

}
