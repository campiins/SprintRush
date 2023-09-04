using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(CarController))]
public class CarAI : MonoBehaviour
{
    private CarController carController; // Componente CarController del coche
    private Transform cam; // Referencia a la cámara
    private Canvas canvas; // Componente Canvas para mostrar nombre y posición del coche
    private TMP_Text UIName; // Texto para mostrar el nombre del coche de la IA
    private TMP_Text UIPosition; // Texto para mostrar la posición en carrera
    public string AIName; // Nombre del coche de la IA

    private Raceline raceline; // Componente Raceline que contiene información sobre la trayectoria que el coche debe seguir
    private Node currentNode; // Nodo actual o objetivo que el coche debe alcanzar
    private List<Node> nodes = new List<Node>(); // Lista de nodos en la trayectoria

    [Range(-1, 1)] private float forwardAmount = 0f; // Controla el acelerador del coche
    [Range(-1, 1)] private float turnAmount = 0f; // Controla el giro del coche
    public float steerForce; // Controla la fuerza de giro del coche. Funciona en el rango [0,2].
    private float originalMaxSpeed; // Máxima velocidad original del coche
    private bool hasAppliedBoost = false; // Controla si se ha aplicado el efecto de rebufo previamente
    private float velocity = 0; // Utilizada para el cálculo suave del giro

    public float distanceToReachNode = 20f; // Distancia a la que se considera que ha alcanzado un nodo

    private float maxTimeOffGround = 5.0f; // Tiempo máximo que el coche puede estar quieto o sin tocar el suelo antes de reaparecer
    private float timeOffGround = 0.0f; // Contador de tiempo

    [Header("Sensors")]
    public string[] tagsToIgnore = { }; // Lista de etiquetas que ignorarán los sensores
    public GameObject frontSensors; // Objeto de los sensores frontales
    public float rayLength; // Longitud del rayo del sensor
    public float distanceFromCenter; // Distancia de los rayos laterales desde el centro
    public float sensorAngle; // Ángulo de los rayos laterales

    void Awake()
    {
        // Asignar objetos y variables
        carController = GetComponent<CarController>();
        raceline = GameObject.FindGameObjectWithTag("Path").GetComponent<Raceline>();
        nodes = raceline.nodes;
        originalMaxSpeed = carController.GetMaxSpeed();
        cam = FindObjectOfType<Camera>().transform;
        canvas = GetComponentInChildren<Canvas>();
        TMP_Text[] canvasTexts = canvas.GetComponentsInChildren<TMP_Text>();
        UIName = canvasTexts[0];
        UIPosition = canvasTexts[1];

        if (nodes.Count > 0)
        {
            // Calcular las distancias del coche a ambos nodos iniciales
            float distanceToNode0 = Vector3.Distance(transform.position, nodes[0].transform.position);
            float distanceToNode1 = Vector3.Distance(transform.position, nodes[1].transform.position);
            // El coche tendrá como primer nodo objetivo el nodo inicial más cercano
            if (distanceToNode0 < distanceToNode1)
                currentNode = nodes[0];
            else
                currentNode = nodes[1];
        }
    }

    private void FixedUpdate()
    {
        AddFrontSensors();
        CalculateCurrentNode();

        Steer();
        Accelerate();

        // Aplicar valores de acelerador y dirección
        if (FindObjectOfType<RaceManager>() != null && !GetComponent<TrackCheckpoints>().hasFinished)
        {
            carController.SetInputs(forwardAmount, turnAmount);
        }

        // Comprobar si la velocidad es menor que 10 kmh o si el coche no está en el suelo
        if (carController.GetSpeed() < 10 || !carController.isGrounded())
        {
            // Incrementar el contador de tiempo
            timeOffGround += Time.deltaTime;

            // Verificar si el tiempo ha superado el limite
            if (timeOffGround > maxTimeOffGround)
            {
                // Resetear la posicion del coche
                timeOffGround = 0;
                if (!GetComponent<TrackCheckpoints>().hasFinished) GetComponent<TrackCheckpoints>().ResetPosition();
            }
        }
        else
        {
            // Si el coche está en el suelo y/o la velocidad es mayor o igual a 10 kmh, reiniciar el contador
            timeOffGround = 0.0f;
        }
    }

    private void LateUpdate()
    {
        if (canvas != null)
        {
            // Hacer que el canvas siempre mire hacia la cámara
            canvas.transform.LookAt(cam.transform);
        }

        // Asignar nombre y posición a los textos del canvas
        if (UIName.text == "") UIName.text = AIName;
        UIPosition.text = carController.GetRacePosition().ToString();
    }

    // Calcular valor de dirección del coche
    private void Steer()
    {
        Vector3 relativeVector = transform.InverseTransformPoint(currentNode.transform.position);
        relativeVector /= relativeVector.magnitude;
        float newSteer = (relativeVector.x / relativeVector.magnitude) * steerForce;
        turnAmount = Mathf.SmoothDamp(turnAmount, newSteer, ref velocity, Time.deltaTime * 2);
    }

    // Calcular valor de aceleración del coche
    private void Accelerate()
    {
        Vector3 position = gameObject.transform.position;
        Vector3 directionToCurrentNode = (currentNode.transform.position - position).normalized;
        Vector3 forwardDirection = transform.forward;
        float angle = Vector3.Angle(forwardDirection, directionToCurrentNode);

        // Si el ángulo es menor a un cierto umbral (por ejemplo, 5 grados) o la velocidad es menor a 100 kmh, acelerar
        if (angle < 5f || carController.GetSpeed() < 100f)
        {
            forwardAmount = Mathf.Lerp(forwardAmount, 1f, Time.deltaTime * 4);
        }
        else
        {
            // Si el ángulo es mayor o igual al umbral y la velocidad es mayor a 100 khm, el coche deja de acelerar
            forwardAmount = Mathf.Lerp(forwardAmount, 0f, Time.deltaTime * 2);
        }

        // Calcular distancia entre el ultimo nodo alcanzado y el nodo actual
        float distanceBetweenNodes = Vector3.Distance(currentNode.previousNode.transform.position, currentNode.transform.position);

        if (distanceBetweenNodes < 33f) // Si la distancia entre nodos es menor a 33
        {
            // Ajustar el valor del acelerador para reducir la aceleración en las curvas cerradas
            forwardAmount = 0.3f;

            if (distanceBetweenNodes < 25f) // Si la distancia entre nodos es menor a 25
            {
                // Dejar de acelerar, y frenar un poco si la velocidad es mayor a 220 kmh
                forwardAmount = Mathf.Lerp(forwardAmount, 0f, Time.deltaTime * 2);
                if (carController.GetSpeed() > 220f)
                {
                    forwardAmount = -0.3f;
                }

            }
            // Frenar si la velocidad es mayor a 250 kmh
            if (carController.GetSpeed() > 250f) 
            {
                forwardAmount = -0.9f;
            }
        }
    }

    // Calcular nodo actual
    private void CalculateCurrentNode()
    {
        Vector3 position = gameObject.transform.position;
        float distanceToNode = Vector3.Distance(position, currentNode.transform.position);
        
        if (distanceToNode < distanceToReachNode) // Si la distancia hasta el nodo actual es menor a la distancia necesaria para alcanzar un nodo
        {
            // Cambiar nodo actual al siguiente nodo
            currentNode = currentNode.nextNode;
        }
    }

    // Añadir funcionalidad de los sensores frontales
    private void AddFrontSensors()
    {
        Vector3 sensorPosition = frontSensors.transform.position;
        Vector3 forwardDirection = transform.forward;
        float distanceToNode = Vector3.Distance(transform.position, currentNode.transform.position);

        RaycastHit hit;
        int layerMask = ~LayerMask.GetMask(tagsToIgnore); // Ignorar objetos con las tags especificadas

        // Raycast central hacia adelante
        if (Physics.Raycast(sensorPosition, forwardDirection, out hit, rayLength * 1.5f, layerMask))
        {
            Gizmos.color = Color.red;

            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Car"))
            {
                if (hit.distance < 5f) // Si esta muy cerca de otro coche
                {
                    forwardAmount = -0.75f; // frenar
                }

                if (!hasAppliedBoost && hit.distance > 10 && distanceToNode > 50 && !carController.reverse) // si la distancia del coche alcanzado es mayor a 10 y la distancia al siguiente nodo es mayor a 50
                {
                    // Alcanzo un coche y no se ha aplicado el efecto de rebufo previamente, aumentar la velocidad máxima
                    originalMaxSpeed = carController.GetMaxSpeed();
                    carController.SetMaxSpeed(originalMaxSpeed * 1.1f); // Aumentar en un 10%
                    hasAppliedBoost = true;
                }
            }

            if (carController.GetSpeed() < 20) // Si la velocidad del coche es muy reducida
            {
                if (currentNode.nodeIndex > 2) // Esto se hace para evitar que vaya marcha atras en la salida, donde los coches estan cerca
                    forwardAmount = -1f; // Aplicar aceleración negativa
                
                // Detectar hacia donde apunta la normal del objeto detectado por el rayo y controlar la dirección de giro, dependiendo el sentido de la marcha del coche
                // para simular una maniobra y evitar el obstáculo (se necesita mejorar esta parte)
                if (hit.normal.x < 0)
                {
                    if (carController.reverse)
                        turnAmount = Mathf.Lerp(turnAmount, 1f, Time.deltaTime * 2);
                    else
                        turnAmount = Mathf.Lerp(turnAmount, -1f, Time.deltaTime * 2);
                }
                if (hit.normal.x > 0)
                {
                    if (carController.reverse)
                        turnAmount = Mathf.Lerp(turnAmount, -1f, Time.deltaTime * 2);
                    else
                        turnAmount = Mathf.Lerp(turnAmount, 1f, Time.deltaTime * 2);
                }
            }
        }
        else
        {
            Gizmos.color = Color.green;

            // Dejar de aplicar rebufo
            if (carController.GetMaxSpeed() > originalMaxSpeed)
            {
                carController.SetMaxSpeed(originalMaxSpeed);
                hasAppliedBoost = false;
            }
            
        }
        Debug.DrawRay(sensorPosition, forwardDirection * rayLength * 1.5f, Gizmos.color);

        // Raycast a la izquierda
        if (Physics.Raycast(sensorPosition - transform.right * distanceFromCenter, forwardDirection, out hit, rayLength, layerMask))
        {
            Gizmos.color = Color.red;

            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Car"))
            {
                if (hit.distance <= 10 & carController.GetSpeed() > 80) // Si esta muy cerca de otro coche
                {
                    forwardAmount = -1f; // frenar
                }

                if (hit.distance < 15f) // Si esta a menos de 15 unidades de distancia de otro coche
                {
                    turnAmount = Mathf.Lerp(turnAmount, 1f, Time.deltaTime * 2); // Girar a la derecha
                }
            }

            if (carController.reverse) // Si esta yendo marcha atras
            {
                turnAmount = Mathf.Lerp(turnAmount, -1f, Time.deltaTime * 2); // Girar a la izquierda
            }
            else // Si va hacia delante
            {
                if (carController.GetSpeed() < 20) // Si la velocidad es menor a 20 kmh
                {
                    turnAmount = Mathf.Lerp(turnAmount, 1f, Time.deltaTime * 2); // Girar a la derecha
                }
            }
        }
        else
        {
            Gizmos.color = Color.green;
        }
        Debug.DrawRay(sensorPosition - transform.right * distanceFromCenter, forwardDirection * rayLength, Gizmos.color);

        // Raycast a la derecha
        if (Physics.Raycast(sensorPosition + transform.right * distanceFromCenter, forwardDirection, out hit, rayLength, layerMask))
        {
            Gizmos.color = Color.red;

            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Car"))
            {
                if (hit.distance <= 10 && carController.GetSpeed() > 80) // Si esta muy cerca de otro coche
                {
                    forwardAmount = -1f; // frenar
                }

                if (hit.distance < 15f) // Si esta a menos de 15 unidades de distancia de otro coche
                {
                    turnAmount = Mathf.Lerp(turnAmount, -1f, Time.deltaTime * 2);  // Girar a la izquierda
                }
            }

            if (carController.reverse) // Si esta yendo marcha atras
            {
                turnAmount = Mathf.Lerp(turnAmount, 1f, Time.deltaTime * 2); // Girar a la derecha
            }
            else // Si va hacia delante
            {
                if (carController.GetSpeed() < 20) // Si la velocidad es menor a 20 kmh
                {
                    turnAmount = Mathf.Lerp(turnAmount, -1f, Time.deltaTime * 2); // Girar a la izquierda
                }
            }
        }
        else
        {
            Gizmos.color = Color.green;
        }
        Debug.DrawRay(sensorPosition + transform.right * distanceFromCenter, forwardDirection * rayLength, Gizmos.color);

        // Rayos frontales en angulo ----------------------------------------------------------------------------------------------------

        Vector3 leftDirection = Quaternion.Euler(0, -sensorAngle, 0) * forwardDirection;
        Vector3 rightDirection = Quaternion.Euler(0, sensorAngle, 0) * forwardDirection;

        // Raycast a la izquierda
        if (Physics.Raycast(sensorPosition - transform.right * distanceFromCenter, leftDirection, out hit, rayLength, layerMask))
        {
            Gizmos.color = Color.red;

            if (forwardAmount > -0.9f) // si no esta frenando al maximo
            {
                forwardAmount = 0.5f; // Acelerar a la mitad
            }
            turnAmount = Mathf.Lerp(turnAmount, 0.5f, Time.deltaTime * 2); // Girar a la derecha
             
            if (carController.reverse) // Si esta yendo marcha atras
            {
                turnAmount = Mathf.Lerp(turnAmount, -1f, Time.deltaTime * 2); // Girar a la izquierda
            }
            else // Si va hacia delante
            {
                if (carController.GetSpeed() < 20) // Si la velocidad es menor a 20 kmh
                {
                    turnAmount = Mathf.Lerp(turnAmount, 1f, Time.deltaTime * 2); // Girar a la derecha
                }
            }
        }
        else
        {
            Gizmos.color = Color.green;
        }
        Debug.DrawRay(sensorPosition - transform.right * distanceFromCenter, leftDirection * rayLength, Gizmos.color);

        // Raycast a la derecha
        if (Physics.Raycast(sensorPosition + transform.right * distanceFromCenter, rightDirection, out hit, rayLength, layerMask))
        {
            Gizmos.color = Color.red;

            if (forwardAmount > -0.9f) // si no esta frenando al maximo
            {
                forwardAmount = 0.5f; // Acelerar a la mitad
            }
            turnAmount = Mathf.Lerp(turnAmount, -0.5f, Time.deltaTime * 2); // Girar a la izquierda

            if (carController.reverse) // Si esta yendo marcha atras
            {
                turnAmount = Mathf.Lerp(turnAmount, 1f, Time.deltaTime * 2); // Girar a la derecha
            }
            else // Si va hacia delante
            {
                if (carController.GetSpeed() < 20) // Si la velocidad es menor a 20 kmh
                {
                    turnAmount = Mathf.Lerp(turnAmount, -1f, Time.deltaTime * 2); // Girar a la izquierda
                }
            }
        }
        else
        {
            Gizmos.color = Color.green;
        }
        Debug.DrawRay(sensorPosition + transform.right * distanceFromCenter, rightDirection * rayLength, Gizmos.color);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        // Dibujar nodo actual
        if (currentNode != null)
            Gizmos.DrawWireSphere(currentNode.transform.position, 2);
    }
}
