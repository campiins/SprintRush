using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class RaceManager : MonoBehaviour
{
    public int totalLaps = 3; // Numero total de vueltas
    public int numberOfRivals = 3; // Numero total de rivales
    [HideInInspector] public int maxNumberOfRivals = 3; // Numero maximo de rivales (por ahora no pueden haber mas de 3 porque no hay mas posiciones de salida asignadas)

    public List<string> availableNames; // Nombres para la IA
    private List<string> usedNames = new List<string>();

    [HideInInspector] public CarController playerController; // Referencia a la clase CarController del coche del jugador
    private TrackCheckpoints trackCheckpoints; // Referencia a la clase TrackCheckpoints
    public List<GameObject> vehiclePositions; // Lista de coches ordenada por posicion de carrera
    public List<GameObject> finishedVehicles; // Almacena los coches que han finalizado


    [SerializeField] private GameObject startPositionsContainer; // GameObject que contiene las posiciones de salida
    [HideInInspector] public List<Transform> startPositions; // Lista de posiciones de salida

    private CountdownTimer countdownTimer;
    private int finishedCarsCount = 0; // contador de vehiculos que han finalizado la carrera

    public GameObject playerCarPrefab; // Prefab del coche del jugador
    public GameObject AICarPrefab; // Prefab del coche de los rivales
    public event Action<GameObject> OnPlayerSpawned;
    public event Action<GameObject> OnAISpawned;

    public bool spawn;
    public bool isShowingResults = false;

    private void Awake()
    {
        countdownTimer = FindObjectOfType<CountdownTimer>();

        foreach (Transform startPosition in startPositionsContainer.transform)
        {
            startPositions.Add(startPosition);
        }

        finishedVehicles = new List<GameObject>(); // Inicializa la lista de coches terminados
    }

    // Start is called before the first frame update
    void Start()
    {
        if (spawn)
        {
            SpawnCars();
            // Iniciar la corrutina Countdown
            StartCoroutine(countdownTimer.Countdown(OnCountdownFinished));
        }
    }

    // Update is called once per frame
    void Update()
    {
        foreach (GameObject vehicle in vehiclePositions) // Para cada vehiculo
        {
            Rigidbody vehicleRigidbody = vehicle.GetComponent<Rigidbody>(); // Obtener componente Rigidbody del vehiculo
            TrackCheckpoints vehicleCheckpoints = vehicle.GetComponent<TrackCheckpoints>(); // Obtener componente TrackCheckpoints del vehiculo

            if (vehicleCheckpoints.currentLap > totalLaps) // si ha superado el numero total de vueltas
            {
                if (!vehicleCheckpoints.hasFinished)
                {
                    vehicleCheckpoints.hasFinished = true; // Marcar que el vehiculo ha finalizado
                    finishedVehicles.Add(vehicleCheckpoints.gameObject); // Añadir vehiculo a lista de vehiculos que han finalizado la carrera
                    finishedCarsCount++; // Sumar solo una vez por vehiculo que haya finalizado

                    if (vehicle.GetComponent<EngineAudio>().isEngineRunning) StartCoroutine(vehicle.GetComponent<EngineAudio>().StopEngine());

                    if (vehicle.GetComponent<CarController>() == playerController)
                    {
                        // El jugador ha finalizado la carrera
                        //playerCam.FinishCam(); // Aplicar la camara de final de carrera
                        
                        playerController.gameObject.GetComponent<InputManager>().canUseInput = false; // Desactivar inputs de acelerador y freno al jugador
                    }

                    if (vehicleRigidbody.drag < 2 && vehicle.GetComponent<CarController>().GetSpeed() > 10f) // Si el drag es menor a 2 y la velocidad mayor a 10
                    {
                        // Aumentar progresivamente el drag hasta valor 2
                        float newDrag = vehicleRigidbody.drag + Mathf.Pow(0.001f, Time.deltaTime);
                        vehicleRigidbody.drag = Mathf.Min(newDrag, 2);
                    }

                    if (!isShowingResults && playerController.GetComponent<TrackCheckpoints>().hasFinished)
                    {
                        StartCoroutine(ShowResults());
                    }
                    else
                    {
                        UpdateTable();
                    }
                }
            }
        }

        UpdateRacePositions(); // Actualizar posiciones de carrera
    }

    void SpawnCars()
    {
        int numberOfCars = Mathf.Clamp(numberOfRivals, 0, startPositions.Count - 1) + 1; // Incluye al coche del jugador
        int playerStartPositionIndex = UnityEngine.Random.Range(0, numberOfCars); // Generar un índice aleatorio para la posición del jugador

        // Spawn del coche del jugador
        GameObject playerCar = Instantiate(playerCarPrefab, startPositions[playerStartPositionIndex].position, startPositions[playerStartPositionIndex].rotation);
        vehiclePositions.Add(playerCar);
        playerController = playerCar.GetComponent<CarController>();
        playerCar.GetComponent<EngineAudio>().carController = playerController;
        OnPlayerSpawned?.Invoke(playerCar);

        trackCheckpoints = playerController.GetComponent<TrackCheckpoints>();

        // Spawn de los coches rivales
        for (int i = 0; i < numberOfCars; i++)
        {
            if (i != playerStartPositionIndex) // Saltar la posición del jugador
            {
                // Elegir un nombre aleatorio de la lista de nombres disponibles
                int randomIndex = UnityEngine.Random.Range(0, availableNames.Count);
                string randomName = availableNames[randomIndex];

                GameObject rivalCar = Instantiate(AICarPrefab, startPositions[i].position, startPositions[i].rotation);
                rivalCar.GetComponent<EngineAudio>().carController = rivalCar.GetComponent<CarController>();
                rivalCar.GetComponent<CarAI>().AIName = randomName;
                rivalCar.gameObject.name = "AI "+randomName; 
                usedNames.Add(randomName);
                availableNames.RemoveAt(randomIndex);
                vehiclePositions.Add(rivalCar);

                OnAISpawned?.Invoke(rivalCar);
                OnAISpawned?.Invoke(rivalCar);
            }
        }
    }

    void OnCountdownFinished()
    {
        // Activar la variable canMove de cada coche
        foreach (GameObject vehicle in vehiclePositions)
        {
            CarController carController = vehicle.GetComponent<CarController>();
            if (carController != null)
            {
                carController.canMove = true;
            }
        }
    }

    private void UpdateRacePositions()
    {
        // Separa los coches terminados de los no terminados
        List<GameObject> finishedCars = new List<GameObject>();
        List<GameObject> unfinishedCars = new List<GameObject>();

        foreach (GameObject vehicle in vehiclePositions)
        {
            CarController carController = vehicle.GetComponent<CarController>();
            if (carController.GetComponent<TrackCheckpoints>().hasFinished)
            {
                finishedCars.Add(vehicle);
            }
            else
            {
                unfinishedCars.Add(vehicle);
            }
        }

        // Ordena los coches en función de la posición actual en la carrera
        unfinishedCars.Sort(CompareCars);

        // Actualiza las posiciones de los coches en función de su orden
        for (int i = 0; i < unfinishedCars.Count; i++)
        {
            CarController carController = unfinishedCars[i].GetComponent<CarController>();
            carController.UpdateRacePosition(i + 1);
        }

        // Reemplaza la lista de coches con la lista ordenada completa
        vehiclePositions.Clear();
        vehiclePositions.AddRange(finishedCars);
        vehiclePositions.AddRange(unfinishedCars);
    }

    private int CompareCars(GameObject carA, GameObject carB)
    {
        // Compara los coches en función de su posición en la carrera

        TrackCheckpoints checkpointsA = carA.GetComponent<TrackCheckpoints>();
        TrackCheckpoints checkpointsB = carB.GetComponent<TrackCheckpoints>();

        // Comprueba la vuelta actual
        int lapA = checkpointsA.currentLap;
        int lapB = checkpointsB.currentLap;

        if (lapA > lapB)
            return -1;
        else if (lapA < lapB)
            return 1;

        // Comprueba si algún coche ha pasado la línea de meta
        if (checkpointsA.currentCheckpoint == 0 && checkpointsB.currentCheckpoint != 0)
            return -1;
        else if (checkpointsA.currentCheckpoint != 0 && checkpointsB.currentCheckpoint == 0)
            return 1;

        // Comprueba el checkpoint actual
        int checkpointA = checkpointsA.currentCheckpoint;
        int checkpointB = checkpointsB.currentCheckpoint;

        if (checkpointA > checkpointB)
            return -1;
        else if (checkpointA < checkpointB)
            return 1;

        // Comprueba la distancia hasta el checkpoint
        float distanceToCheckpointA = Vector3.Distance(carA.transform.position, trackCheckpoints.checkpoints[checkpointA].position);
        float distanceToCheckpointB = Vector3.Distance(carB.transform.position, trackCheckpoints.checkpoints[checkpointB].position);

        if (distanceToCheckpointA < distanceToCheckpointB)
            return -1;
        else if (distanceToCheckpointA > distanceToCheckpointB)
            return 1;

        return 0; // Los coches están en la misma posición
    }

    private void UpdateTable()
    {
        Canvas canvasWithUIManager = FindObjectOfType<UIManager>().GetComponentInParent<Canvas>();
        if (canvasWithUIManager != null)
        {
            GameObject leaderboardObject = canvasWithUIManager.transform.Find("Leaderboard").gameObject;
            if (leaderboardObject != null)
            {
                // Encuentra el componente ResultsTable en el objeto Leaderboard
                ResultsTable results = leaderboardObject.GetComponentInChildren<ResultsTable>();
                results.UpdateLeaderboard();
            }
        }
    }

    IEnumerator ShowResults()
    {
        yield return new WaitForSeconds(0.5f);
        isShowingResults = true;
        // Activar tabla de resultados
        Canvas canvasWithUIManager = FindObjectOfType<UIManager>().GetComponentInParent<Canvas>();
        Canvas minimap = GameObject.FindGameObjectWithTag("Minimap").GetComponentInChildren<Canvas>();
        if (canvasWithUIManager != null)
        {
            foreach (Transform child in canvasWithUIManager.transform)
            {
                child.gameObject.SetActive(false); // Desactiva todos los hijos
            }

            minimap.gameObject.SetActive(false); // Desactivar minimapa

            GameObject leaderboardObject = canvasWithUIManager.transform.Find("Leaderboard").gameObject;
            if (leaderboardObject != null)
            {
                // Encuentra el componente ResultsTable en el objeto Leaderboard
                ResultsTable results = leaderboardObject.GetComponentInChildren<ResultsTable>();
                results.gameObject.SetActive(true);
                results.UpdateLeaderboard();
            }
        }
    }
}
