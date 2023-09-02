using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameModeSettings : MonoBehaviour
{
    public enum Gamemode
    {
        Race, // Carrera
        Practice // Practica
    }

    public Gamemode gamemode;
    RaceManager raceManager;

    private void Awake()
    {
        if (PlayerPrefs.GetString("Gamemode") == "Race")
        {
            SetRaceMode();
        }
        else if (PlayerPrefs.GetString("Gamemode") == "Practice")
        {
            SetPracticeMode();
        }
    }

    private void Start()
    {
        ApplyGameModeSettings();
    }

    public void SetRaceMode()
    {
         gamemode = Gamemode.Race;
    }

    public void SetPracticeMode()
    {
        gamemode = Gamemode.Practice;
    }

    public Gamemode GetGameMode()
    {
        return gamemode;
    }

    public void ApplyGameModeSettings()
    {
        raceManager = FindObjectOfType<RaceManager>();
        if (gamemode == Gamemode.Practice)
        {
            raceManager.totalLaps = SetNumberOfLaps(int.MaxValue);
            raceManager.numberOfRivals = SetNumberOfRivals(0);
        }
        else if (gamemode == Gamemode.Race)
        {
            raceManager.totalLaps = SetNumberOfLaps(raceManager.totalLaps);
            raceManager.numberOfRivals = SetNumberOfRivals(raceManager.numberOfRivals);
        }
    }

    public int SetNumberOfLaps(int n_laps)
    {
        return n_laps;
    }

    public int SetNumberOfRivals(int n_rivals)
    {
        if (n_rivals <= raceManager.maxNumberOfRivals)
        {
            return n_rivals;
        }
        else
        {
            return raceManager.maxNumberOfRivals;
        }
    }
}
