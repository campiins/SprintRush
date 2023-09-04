using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TrackCheckpoints : MonoBehaviour
{
    private GameObject checkpointsContainer; // Objeto contenedor de los puntos de control
    [HideInInspector] public List<Transform> checkpoints; // Lista de puntos de control
    [HideInInspector] public int currentCheckpoint; // Punto de control actual
    private int lastCheckpoint; // Último punto de control alcanzado
    [HideInInspector] public int currentLap; // Número de vuelta actual
    [HideInInspector] public bool hasFinished = false; // Indicador de carrera finalizada

    private LapCounter lapCounter; // Referencia a la clase LapCounter

    private Vector3 respawnPosition; // Posición de restablecimiento del coche

    public event EventHandler<CarCheckpointEventArgs> OnCarCorrectCheckpoint;
    public event EventHandler<CarCheckpointEventArgs> OnCarWrongCheckpoint;

    private void Awake()
    {
        checkpointsContainer = GameObject.Find("Checkpoints");
        lapCounter = GetComponent<LapCounter>();

        foreach (Transform checkpoint in checkpointsContainer.transform)
        {
            checkpoints.Add(checkpoint);
        }
    }

    void Start()
    {
        currentCheckpoint = 0;
        lastCheckpoint = checkpoints.Count - 1;
        currentLap = 0;
    }

    // Restablecer posición del coche a la posición último punto de control alcanzado
    public void ResetPosition()
    {
        if (currentLap != 0)
        {
            Transform respawnCheckpoint = checkpoints[lastCheckpoint];
            transform.position = respawnCheckpoint.position;
            transform.rotation = respawnCheckpoint.rotation;
            respawnPosition = respawnCheckpoint.position; // Actualizar la posición de respawn

            // Lanzar un Raycast hacia abajo para encontrar la superficie del suelo
            RaycastHit hit;
            if (Physics.Raycast(respawnPosition, -Vector3.up, out hit))
            {
                // Ajustar la posición vertical para que el vehiculo aparezca en el suelo
                float groundOffset = 1f;
                transform.position = hit.point + hit.normal * groundOffset;
            }

            GetComponent<CarController>().StopCompletely(); // Detener la velocidad del coche
        }
    }

    // Detectar cuando el coche pasa por un punto de control
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Checkpoint"))
        {
            if (currentCheckpoint < checkpoints.Count - 1) // Verificar si el coche aun no ha pasado por todos los checkpoints
            {
                if (other.GetComponent<CheckpointData>().checkpointIndex == currentCheckpoint) // Si el checkpoint por el que pasa el coche no ha sido 'visitado'
                {
                    lastCheckpoint = currentCheckpoint; // Establecer ultimo checkpoint

                    currentCheckpoint++; // Incrementar checkpoint actual

                    if (currentCheckpoint == 1) // Si ha pasado por linea de meta
                    {
                        if (currentLap > 0) // Si no es la primera vez que pasa por meta (la vuelta es minimo la primera)
                        {
                            lapCounter.EndLap(); // Finaliza una vuelta
                        }
   
                        lapCounter.StartLap(); // Empieza una nueva vuelta
                        currentLap++; // Incrementar vuelta
                    }
                    // Lanzar el evento OnCarCorrectCheckpoint
                    OnCarCorrectCheckpoint?.Invoke(this, new CarCheckpointEventArgs(transform));
                }
                else
                {
                    // Lanzar el evento OnCarWrongCheckpoint
                    OnCarWrongCheckpoint?.Invoke(this, new CarCheckpointEventArgs(transform));
                }
            }
            else // Si el coche ya ha pasado por todos los checkpoints
            {
                currentCheckpoint = 0; // El checkpoint actual es la linea de meta
                lastCheckpoint++; // Incrementar ultimo checkpoint
            }
        }
    }

    // Obtener siguiente checkpoint
    public Transform GetNextCheckpoint(Transform carTransform)
    {
        if (currentCheckpoint < checkpoints.Count)
        {
            return checkpoints[currentCheckpoint];
        }

        // Si el coche ha pasado por todos los checkpoints, retorna el primer checkpoint (línea de meta)
        return checkpoints[0];
    }

    // Reiniciar checkpoint para el entrenamiento
    public void ResetCheckpoint(Transform carTransform)
    {
        currentCheckpoint = 0;
        lastCheckpoint = checkpoints.Count - 1;
        currentLap = 0;
    }

    public class CarCheckpointEventArgs : EventArgs
    {
        public Transform carTransform;

        public CarCheckpointEventArgs(Transform transform)
        {
            carTransform = transform;
        }
    }
}
