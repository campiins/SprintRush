using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    RaceManager raceManager;
    CarController carController;
    LapCounter lapCounter;

    [SerializeField] TMP_Text speed;
    [SerializeField] TMP_Text totalTime;
    [SerializeField] TMP_Text lapTime;
    [SerializeField] TMP_Text bestTime;
    [SerializeField] TMP_Text playerPosition;
    [SerializeField] TMP_Text playerCurrentLap;
    public Image resetBar; 

    private void Awake()
    {
        raceManager = FindObjectOfType<RaceManager>();

        raceManager.OnPlayerSpawned += OnPlayerSpawned;
        raceManager.OnAISpawned += OnAISpawned;
    }

    void OnPlayerSpawned(GameObject player)
    {
        carController = raceManager.playerController;
        lapCounter = carController.GetComponent<LapCounter>();

    }

    void OnAISpawned(GameObject opponent)
    {
        //
    }

    // Update is called once per frame
    void Update()
    {
        speed.text = Mathf.RoundToInt(carController.speedClamped).ToString();

        // LAPS

        float totalTimeSpan = lapCounter.totalRaceTime;
        int minutes = Mathf.FloorToInt(totalTimeSpan / 60);
        int seconds = Mathf.FloorToInt(totalTimeSpan % 60);
        int milliseconds = Mathf.FloorToInt((totalTimeSpan * 1000) % 1000);
        string totalTimeFormatted = string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
        totalTime.text = totalTimeFormatted.Monospace();

        float lapTimeSpan = lapCounter.currentLapTime;
        minutes = Mathf.FloorToInt(lapTimeSpan / 60);
        seconds = Mathf.FloorToInt(lapTimeSpan % 60);
        milliseconds = Mathf.FloorToInt((lapTimeSpan * 1000) % 1000);
        string lapTimeFormatted = string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
        lapTime.text = lapTimeFormatted.Monospace();

        if (lapCounter.bestLapTime != float.MaxValue)
        {
            float bestTimeSpan = lapCounter.bestLapTime;
            minutes = Mathf.FloorToInt(bestTimeSpan / 60);
            seconds = Mathf.FloorToInt(bestTimeSpan % 60);
            milliseconds = Mathf.FloorToInt((bestTimeSpan * 1000) % 1000);
            string bestTimeFormatted = string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
            bestTime.text = bestTimeFormatted.Monospace();
        }
        else
        {
            bestTime.text = "- - : - - . - - -";
        }

        if (raceManager.totalLaps != int.MaxValue)
        {
            playerCurrentLap.text = carController.GetComponent<TrackCheckpoints>().currentLap + " / " + raceManager.totalLaps;
        }
        else
        {
            playerCurrentLap.text = carController.GetComponent<TrackCheckpoints>().currentLap + " / ∞";
        }
        
        // POSITION

        playerPosition.text = carController.racePosition + " / " + raceManager.vehiclePositions.Count;
    }

    public void ExitToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
