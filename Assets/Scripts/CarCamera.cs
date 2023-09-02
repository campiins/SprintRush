using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarCamera : MonoBehaviour
{
    RaceManager raceManager;
    public GameObject car;
    public float distance = 6.4f;
    public float height = 1.4f;
    public float rotationDamping = 3.0f;
    public float heightDamping = 2.0f;
    public float zoomRatio = 0.5f;
    public float defaultFOV = 60f;
    private Vector3 rotationVector;

    private void Awake()
    {
        raceManager = FindObjectOfType<RaceManager>();
        raceManager.OnPlayerSpawned += OnPlayerSpawned;
    }

    void OnPlayerSpawned(GameObject player)
    {
        car = raceManager.playerController.gameObject;
    }

    private void LateUpdate()
    {
        float wantedAngle = rotationVector.y;
        float wantedHeight = car.transform.position.y + height;
        float myAngle = transform.eulerAngles.y;
        float myHeight = transform.position.y;

        myAngle = Mathf.LerpAngle(myAngle, wantedAngle, rotationDamping * Time.deltaTime);
        myHeight = Mathf.Lerp(myHeight, wantedHeight, heightDamping * Time.deltaTime);

        Quaternion currentRotation = Quaternion.Euler(0f, myAngle, 0f);
        transform.position = car.transform.position;
        transform.position -= currentRotation * Vector3.forward * distance;
        transform.position = new Vector3(transform.position.x, myHeight, transform.position.z);
        transform.LookAt(car.transform);
    }

    private void FixedUpdate()
    {
        Vector3 localVelocity = car.transform.InverseTransformDirection(car.GetComponent<Rigidbody>().velocity);

        if (localVelocity.z < -0.5f)
        {
            rotationVector.y = car.transform.eulerAngles.y + 180f;
        }
        else
        {
            rotationVector.y = car.transform.eulerAngles.y;
        }

        float acceleration = car.GetComponent<Rigidbody>().velocity.magnitude;
        GetComponent<Camera>().fieldOfView = defaultFOV + acceleration * zoomRatio;
    }
}

