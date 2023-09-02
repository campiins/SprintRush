using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class CarAgent : Agent
{
    private TrackCheckpoints trackCheckpoints;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    private CarController carController;

    public float currentReward;

    private void Awake()
    {
        carController = GetComponent<CarController>();
        trackCheckpoints = GetComponent<TrackCheckpoints>();
    }

    // Start is called before the first frame update
    void Start()
    {
        spawnPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z); ;
        spawnRotation = new Quaternion(transform.rotation.x, transform.rotation.y, transform.rotation.z, transform.rotation.w);
        trackCheckpoints.OnCarCorrectCheckpoint += TrackCheckpoints_OnCarCorrectCheckpoint;
        trackCheckpoints.OnCarWrongCheckpoint += TrackCheckpoints_OnCarWrongCheckpoint;
    }

    private void TrackCheckpoints_OnCarCorrectCheckpoint(object sender, TrackCheckpoints.CarCheckpointEventArgs e)
    {
        if (e.carTransform == transform)
        {
            AddReward(0.5f);
            currentReward += 0.5f;

            if (trackCheckpoints.currentLap > 1)
            {
                Debug.Log(currentReward);
                EndEpisode();
            }
        }
    }

    private void TrackCheckpoints_OnCarWrongCheckpoint(object sender, TrackCheckpoints.CarCheckpointEventArgs e)
    {
        if (e.carTransform == transform)
        {
            AddReward(-1f);
            currentReward -= 1.0f;
            Debug.Log("Checkpoint incorrecto");
        }
    }

    public override void OnEpisodeBegin()
    {
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        trackCheckpoints.ResetCheckpoint(transform);
        carController.StopCompletely();
        currentReward = 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Producto punto entre la direccion del checkpoint objetivo y la direccion actual del coche
        Vector3 checkpointForward = trackCheckpoints.GetNextCheckpoint(transform).forward;
        float directionDot = Vector3.Dot(transform.forward, checkpointForward);
        sensor.AddObservation(directionDot); // Size = 1
        // Posicion del siguiente checkpoint
        Vector3 currentCheckpoint = trackCheckpoints.GetNextCheckpoint(transform).position;
        sensor.AddObservation(currentCheckpoint); // Size = 3
        // Velocidad del coche
        sensor.AddObservation(carController.GetSpeed()); // Size = 1
        // Posicion y direccion delantera del coche
        sensor.AddObservation(transform.position); // Size = 3
        sensor.AddObservation(transform.forward); // Size = 3
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        //float forwardAmount = 0f;
        //float turnAmount = 0f;

        //switch (actions.DiscreteActions[0])
        //{
        //    case 0:
        //        forwardAmount = 0f;
        //        AddReward(-0.3f);
        //        break;
        //    case 1:
        //        forwardAmount = 1f;
        //        AddReward(0.0001f);
        //        break;
        //    case 2:
        //        forwardAmount = -1f;
        //        AddReward(-0.01f);
        //        break;

        //}
        //switch (actions.DiscreteActions[1])
        //{
        //    case 0:
        //        turnAmount = 0f;
        //        break;
        //    case 1:
        //        turnAmount = 1f;
        //        break;
        //    case 2:
        //        turnAmount = -1f;
        //        break;
        //}

        float forwardAmount = actions.ContinuousActions[0];
        float turnAmount = actions.ContinuousActions[1];

        forwardAmount = Mathf.Clamp(forwardAmount, -1f, 1f);
        turnAmount = Mathf.Clamp(turnAmount, -1f, 1f);

        if (forwardAmount > 0)
        {
            //AddReward(carController.GetSpeed() * 0.0001f);
        }
        //else if (forwardAmount < 0)
        //{
        //    AddReward(-0.001f);
        //    currentReward -= 0.001f;
        //}
        else
        {
            AddReward(-0.001f);
            currentReward -= 0.001f;
        }

        carController.SetInputs(forwardAmount, turnAmount);

        // Penalizar al agente si esta tocando el cesped
        if (carController.IsTouchingGrass != 0)
        {
            float penalty = -0.01f;
            if (carController.IsTouchingGrass == 2)
            {
                penalty = -0.1f; // Penalizacion adicional
            }
            AddReward(penalty);
            currentReward += penalty;
            Debug.Log("Pisando hierba");
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        //int forwardAction = 0;
        //if (Input.GetKey(KeyCode.W)) forwardAction = 1;
        //if (Input.GetKey(KeyCode.S)) forwardAction = 2;

        //int turnAction = 0;
        //if (Input.GetKey(KeyCode.D)) turnAction = 1;
        //if (Input.GetKey(KeyCode.A)) turnAction = 2;

        //ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        //discreteActions[0] = forwardAction;
        //discreteActions[1] = turnAction;

        float forwardAction = Input.GetAxis("Vertical");
        float turnAction = Input.GetAxis("Horizontal");

        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = forwardAction;
        continuousActions[1] = turnAction;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            // Ha chocado contra un muro
            SetReward(-1f);
            currentReward = -1;
            Debug.Log("hit " + collision.gameObject.name + ". Choque contra muro piedra.");
            //Debug.Log(currentReward);
            EndEpisode();
        }
        if (collision.gameObject.CompareTag("AI"))
        {
            // Ha chocado contra un muro
            SetReward(-3f);
            Debug.Log("hit " + collision.gameObject.name + ". Choque contra coche.");
            EndEpisode();
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            // Ha chocado contra un muro
            SetReward(-1f);
            Debug.Log("hit " + collision.gameObject.name + ". Choque contra muro rojo.");
            EndEpisode();
        }
    }
}
