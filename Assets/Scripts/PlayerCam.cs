using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCam : MonoBehaviour
{
    private RaceManager raceManager;
    private GameObject playerCar;
    private Transform cameraTransform;
    public GameObject playerCameras;
    public GameObject rearCameras;

    public List<Transform> camPositions;
    private int currentCamIndex = 0;

    public List<Transform> rearPositions;

    public float distance = 6.4f; // Distancia de la camara al coche
    public float height = 1.4f; // Altura de la camara respecto al coche
    public float rotationDamping = 3.0f; // Suavidad de la rotacion de la camara
    public float heightDamping = 2.0f; // Suavidad del ajuste de altura de la camara
    public float zoomRatio = 0.5f; // Relacion de zoom basado en la velocidad del coche
    public float defaultFOV = 60f; // Campo de vision por defecto de la camara
    private Vector3 rotationVector; // Vector que almacena los angulos de rotacion para la camara

    private float originalRotationDamping;
    private float originalHeightDamping;


    // Start is called before the first frame update
    void Awake()
    {
        raceManager = FindObjectOfType<RaceManager>();
        raceManager.OnPlayerSpawned += OnPlayerSpawned;
        cameraTransform = gameObject.transform;
        originalRotationDamping = rotationDamping;
        originalHeightDamping = heightDamping;
    }

    void OnPlayerSpawned(GameObject player)
    {
        playerCar = player;
        playerCameras = GameObject.Find("Cameras");
        foreach (Transform child in playerCameras.transform)
        {
            camPositions.Add(child); // Exterior, Hood, Bumper
        }

        rearCameras = GameObject.Find("RearCameras");
        foreach (Transform child in rearCameras.transform)
        {
            rearPositions.Add(child); // Exterior, Back
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (playerCar.GetComponent<InputManager>().isRearview)
        {
            Rearview();

            // Asignamos un valor alto para que, cuando quitemos la camara trasera, la camara vuelva directamente a la posicion original
            rotationDamping = 500;
            heightDamping = 500;
        }
        else if (currentCamIndex > 0)
        {
            GetComponent<Camera>().fieldOfView = defaultFOV;
            transform.position = GetCurrentCameraPosition().position;
            transform.rotation = GetCurrentCameraPosition().rotation;
        }
        else
        {
            float wantedAngle = rotationVector.y; // Obtener el angulo de rotacion deseado para la camara
            float wantedHeight = playerCar.transform.position.y + height; // Obtener altura deseada para la camara
            float myAngle = transform.eulerAngles.y; // Obtener angulo actual de la camara
            float myHeight = transform.position.y; // Obtener altura actual de la camara

            // Interpolar suavemente el angulo actual hacia el angulo deseado
            myAngle = Mathf.LerpAngle(myAngle, wantedAngle, rotationDamping * Time.deltaTime);
            // Interpolar suavemente la altura actual hacia la altura deseada
            myHeight = Mathf.Lerp(myHeight, wantedHeight, heightDamping * Time.deltaTime);

            // Crear una rotacion actual basada en el angulo calculado
            Quaternion currentRotation = Quaternion.Euler(0f, myAngle, 0f);

            // Actualizar la posicion de la camara para seguir al coche a una distancia y altura determinada
            transform.position = playerCar.transform.position;
            transform.position -= currentRotation * Vector3.forward * distance;
            transform.position = new Vector3(transform.position.x, myHeight, transform.position.z);
            transform.LookAt(playerCar.transform);

            // Calcular zoom basado en la velocidad del coche
            float acceleration = playerCar.GetComponent<Rigidbody>().velocity.magnitude;
            GetComponent<Camera>().fieldOfView = defaultFOV + acceleration * zoomRatio;

            // Devolvemos los valores originales a rotationDamping y heightDamping
            if (rotationDamping != originalRotationDamping && heightDamping != originalHeightDamping)
            {
                rotationDamping = originalRotationDamping;
                heightDamping = originalHeightDamping;
            }
        }
    }

    private void FixedUpdate()
    {
        
        if (currentCamIndex == 0) // camara exterior
        {
            // Obtener la velocidad local del coche en su propio sistema de coordenadas
            Vector3 localVelocity = playerCar.transform.InverseTransformDirection(playerCar.GetComponent<Rigidbody>().velocity);

            // Si el coche se mueve hacia atras (velocidad z negativa), girar la camara 180 grados para mirar hacia atras
            if (localVelocity.z < -1f)
            {
                rotationVector.y = playerCar.transform.eulerAngles.y + 180f;
            }
            else
            {
                rotationVector.y = playerCar.transform.eulerAngles.y;
            }
        }
    }

    public void ChangeCamera()
    {
        // Verificar si hay posiciones de camara en la lista
        if (camPositions.Count == 0)
        {
            Debug.LogWarning("No camera positions available.");
            return;
        }

        // Cambiar a la siguiente posicion de camara
        currentCamIndex = (currentCamIndex + 1) % camPositions.Count;
        Transform nextCamPosition = camPositions[currentCamIndex];

        // Mover la camara a la nueva posicion
        transform.position = nextCamPosition.position;
        transform.rotation = nextCamPosition.rotation;
    }

    public void Rearview()
    {
        // Verificar si hay posiciones de camara trasera en la lista
        if (camPositions.Count == 0)
        {
            Debug.LogWarning("No rearview positions available.");
            return;
        }

        // Obtener la posición de la cámara trasera correspondiente según las condiciones
        Transform rearCamPosition = currentCamIndex == 0 ? rearPositions[0] : rearPositions[1];

        // Mover la cámara a la nueva posición de la cámara trasera
        transform.position = rearCamPosition.position;
        transform.rotation = rearCamPosition.rotation;
    }

    public Transform GetCurrentCameraPosition()
    {
        return camPositions[currentCamIndex];
    }

    public void FinishCam()
    {
        //currentCamIndex = 0;
        //rotationVector.y = playerCar.transform.eulerAngles.y + 180f;
        this.enabled = false;
        cameraTransform.position = Vector3.Lerp(cameraTransform.transform.position, cameraTransform.position, 0.5f);
        cameraTransform.rotation = Quaternion.Lerp(cameraTransform.transform.rotation, cameraTransform.rotation, 0.5f);
    }
}
