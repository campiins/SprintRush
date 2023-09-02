using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Camera_BigScreens : MonoBehaviour
{
    [Header("Optional Player")]
    public Transform player;
    private RaceManager raceManager;

    [Space(10)]
    [Header("Interval in seconds")]
    [Range(10, 60)]
    public int interval = 15;

    [Space(20)]



    private Transform atualCam;
    private float timeLeft;
    private Transform pai;

    GameObject[] camera_Point;
    GameObject[] screens;
    // Use this for initialization

    private void Awake()
    {
        raceManager = FindObjectOfType<RaceManager>(); // Buscar el objeto con la clase RaceManager en el escenario
        raceManager.OnPlayerSpawned += OnPlayerSpawned;
    }

    void Start()
    {
        pai = transform.parent;

        camera_Point = GameObject.FindObjectsOfType(typeof(GameObject)).Select(g => g as GameObject).Where(g => g.name.Equals("Camera_Point")).ToArray();

        ChangeCamPoint();

        InvokeRepeating("ChangeCamPoint", 2f, interval);
    }

    void OnPlayerSpawned(GameObject player)
    {
        player = raceManager.playerController.gameObject;
        this.player = player.transform;
    }

    void ChangeCamPoint()
    {

        if (camera_Point.Length <= 0) return;

        int q;

        if (player)
        {

            if (Random.Range(0, 25) < 15)
            {

                atualCam = player.Find("cameraInPlayer");

                if (!atualCam)
                {
                    q = cameraCloserPlayer();
                    atualCam = camera_Point[q].transform;
                    transform.SetParent(pai);
                }
                else {
                    transform.SetParent(player);
                }

            }
            else if (Random.Range(0, 15) < 10)
            {

                q = cameraCloserPlayer();
                atualCam = camera_Point[q].transform;
                transform.SetParent(pai);


            }else {


                q = Random.Range(0, camera_Point.Length - 1);
                atualCam = camera_Point[q].transform;
                transform.SetParent(pai);

            }


        }
        else {

            q = Random.Range(0, camera_Point.Length - 1);
            atualCam = camera_Point[q].transform;
            transform.SetParent(pai);
        }

        if (!atualCam.parent.gameObject.activeInHierarchy)
            ChangeCamPoint();
        else
            CamPosition();

    }

    public void VerifyOcclusionModule()
    {
        if (!atualCam) return;
        if (!atualCam.parent.gameObject.activeInHierarchy)
            ChangeCamPoint();

        
    }

    void CamPosition()
    {
        if (atualCam)
        {
            transform.position = atualCam.position;
            transform.rotation = Quaternion.Euler(atualCam.eulerAngles);
        }
    }

    int cameraCloserPlayer()
    {

        int ncam = camera_Point.Length;
        int _closer = 0;

        float _distance = 10000;
        float _d;

        if (camera_Point[0] == null)
        {
            camera_Point = GameObject.FindObjectsOfType(typeof(GameObject)).Select(g => g as GameObject).Where(g => g.name.Equals("Camera_Point")).ToArray();
            ncam = camera_Point.Length;
        }    

        for (int i = 0; i < ncam; i++)
        {

            _d = Vector3.Distance(camera_Point[i].transform.position, player.position);

            if (_distance > _d)
            {
                _distance = _d;
                _closer = i;
            }

        }



        return _closer;
    }




}
