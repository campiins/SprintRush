using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ResultsTable : MonoBehaviour
{
    RaceManager raceManager;
    Transform container;
    Transform template;
    [SerializeField] float templateHeight = 20f;
    [SerializeField] Color goldColor;
    [SerializeField] Color silverColor;
    [SerializeField] Color bronzeColor;
    [SerializeField] Color purpleColor;
    private float bestOverallLapTime = float.MaxValue;

    private List<Transform> resultEntries = new List<Transform>();

    void Awake()
    {
        raceManager = FindObjectOfType<RaceManager>();
        container = transform.Find("ResultsContainer");
        template = container.Find("ResultsTemplate");

        template.gameObject.SetActive(false);

        // Instantiate the result entries and store them in the list
        for (int i = 0; i < Mathf.Min(raceManager.numberOfRivals, raceManager.maxNumberOfRivals) + 1; i++)
        {
            Transform resultEntry = Instantiate(template, container);
            resultEntries.Add(resultEntry);
        }
    }

    public void UpdateLeaderboard()
    {
        bestOverallLapTime = GetBestOverallLapTime();

        for (int i = 0; i < resultEntries.Count; i++)
        {
            Transform resultEntry = resultEntries[i];

            RectTransform entryRectTransform = resultEntry.GetComponent<RectTransform>();
            entryRectTransform.anchoredPosition = new Vector2(0, -templateHeight * i);
            resultEntry.gameObject.SetActive(true);

            int pos = i + 1;

            TMP_Text posText = resultEntry.Find("PositionText").GetComponent<TMP_Text>();
            switch (pos) // Asignar color de posiciones (oro, plata y bronce para el podio)
            {
                default: posText.color = Color.white; break;
                case 1: posText.color = goldColor; break;
                case 2: posText.color = silverColor; break;
                case 3: posText.color = bronzeColor; break;
            }
            posText.text = pos.ToString();

            int driverIndex = pos - 1;  // Indice para acceder a la posicion del coche
            if (driverIndex < raceManager.vehiclePositions.Count)
            {
                CarAI carAI = raceManager.vehiclePositions[driverIndex].GetComponent<CarAI>();
                InputManager playerInput = FindObjectOfType<InputManager>();

                if (carAI != null) // Si es un coche rival
                {
                    resultEntry.Find("DriverText").GetComponent<TMP_Text>().text = carAI.AIName;
                    if (carAI.GetComponent<LapCounter>().bestLapTime < float.MaxValue && carAI.GetComponent<TrackCheckpoints>().hasFinished)
                    {
                        float totalTimeSpan = carAI.GetComponent<LapCounter>().totalRaceTime;
                        float minutes = Mathf.FloorToInt(totalTimeSpan / 60);
                        float seconds = Mathf.FloorToInt(totalTimeSpan % 60);
                        float milliseconds = Mathf.FloorToInt((totalTimeSpan * 1000) % 1000);
                        string totalTimeFormatted = string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
                        resultEntry.Find("TotalTimeText").GetComponent<TMP_Text>().text = string.Empty;
                        resultEntry.Find("TotalTimeText").GetComponent<TMP_Text>().text = totalTimeFormatted;

                        float bestTimeSpan = carAI.GetComponent<LapCounter>().bestLapTime;
                        minutes = Mathf.FloorToInt(bestTimeSpan / 60);
                        seconds = Mathf.FloorToInt(bestTimeSpan % 60);
                        milliseconds = Mathf.FloorToInt((bestTimeSpan * 1000) % 1000);
                        string bestTimeFormatted = string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
                        resultEntry.Find("BestLapText").GetComponent<TMP_Text>().text = string.Empty;
                        resultEntry.Find("BestLapText").GetComponent<TMP_Text>().text = bestTimeFormatted;
                    }
                    else
                    {
                        resultEntry.Find("BestLapText").GetComponent<TMP_Text>().text = string.Empty;
                        resultEntry.Find("BestLapText").GetComponent<TMP_Text>().text = "- - : - - . - - -";
                    }

                    LapCounter lapCounter = carAI.GetComponent<LapCounter>();
                    // Actualizar el color del texto de vuelta rápida en función del tiempo de vuelta más rápido global
                    SetFastestLapColor(resultEntry, lapCounter.bestLapTime);
                }
                else if (playerInput != null) // Si es el coche del jugador
                {
                    resultEntry.Find("DriverText").GetComponent<TMP_Text>().text = "Player";
                    if (playerInput.GetComponent<LapCounter>().bestLapTime < float.MaxValue && playerInput.GetComponent<TrackCheckpoints>().hasFinished)
                    {
                        float totalTimeSpan = playerInput.GetComponent<LapCounter>().totalRaceTime;
                        float minutes = Mathf.FloorToInt(totalTimeSpan / 60);
                        float seconds = Mathf.FloorToInt(totalTimeSpan % 60);
                        float milliseconds = Mathf.FloorToInt((totalTimeSpan * 1000) % 1000);
                        string totalTimeFormatted = string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
                        resultEntry.Find("TotalTimeText").GetComponent<TMP_Text>().text = string.Empty;
                        resultEntry.Find("TotalTimeText").GetComponent<TMP_Text>().text = totalTimeFormatted;

                        float bestTimeSpan = playerInput.GetComponent<LapCounter>().bestLapTime;
                        minutes = Mathf.FloorToInt(bestTimeSpan / 60);
                        seconds = Mathf.FloorToInt(bestTimeSpan % 60);
                        milliseconds = Mathf.FloorToInt((bestTimeSpan * 1000) % 1000);
                        string bestTimeFormatted = string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
                        resultEntry.Find("BestLapText").GetComponent<TMP_Text>().text = string.Empty;
                        resultEntry.Find("BestLapText").GetComponent<TMP_Text>().text = bestTimeFormatted;
                    }
                    else
                    {
                        resultEntry.Find("BestLapText").GetComponent<TMP_Text>().text = string.Empty;
                        resultEntry.Find("BestLapText").GetComponent<TMP_Text>().text = "- - : - - . - - -";
                    }

                    LapCounter lapCounter = playerInput.GetComponent<LapCounter>();
                    // Actualizar el color del texto de vuelta rápida en función del tiempo de vuelta más rápido global
                    SetFastestLapColor(resultEntry, lapCounter.bestLapTime);
                }
            }
            else
            {
                resultEntry.Find("DriverText").GetComponent<TMP_Text>().text = "N/A"; // Default text if no driver found
            }

        }
    }

    private float GetBestOverallLapTime()
    {
        float bestTime = float.MaxValue;

        if (raceManager != null)
        {
            for (int i = 0; i < raceManager.vehiclePositions.Count; i++)
            {
                LapCounter lapCounter = raceManager.vehiclePositions[i].GetComponent<LapCounter>();
                if (lapCounter != null && lapCounter.bestLapTime < bestTime)
                {
                    bestTime = lapCounter.bestLapTime;
                }
            }
        }

        return bestTime;
    }

    private void SetFastestLapColor(Transform resultEntry, float lapTime)
    {
        TMP_Text bestLapText = resultEntry.Find("BestLapText").GetComponent<TMP_Text>();

        if (lapTime == bestOverallLapTime)
        {
            bestLapText.color = purpleColor;
        }
        else
        {
            bestLapText.color = Color.white; // Cambiar a otro color si no es el mejor tiempo
        }
    }

}
