using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineAudio : MonoBehaviour
{
    public AudioSource runningSound;
    public float runningMaxVolume;
    public float runningMaxPitch;
    [SerializeField] private float pitchSmoothTime;
    
    public AudioSource idleSound;
    public float idleMaxVolume;

    public AudioSource reverseSound;
    public float reverseMaxVolume;
    public float reverseMaxPitch;

    private float speedRatio;

    private float revLimiter;
    public float LimiterSound = 3f;
    public float LimiterFrequency = 10f;
    public float LimiterEngage = 0.8f;
    
    public bool isEngineRunning = false;
    public AudioSource startingSound;

    [HideInInspector] public CarController carController;

    // Start is called before the first frame update
    void Start()
    {
        idleSound.volume = 0;
        runningSound.volume = 0;
        startingSound.volume = 0.2f;
        reverseSound.volume = 0;
    }

    // Update is called once per frame
    void Update()
    {
        float speedSign = 0;
        if (carController)
        {
            speedSign = carController.movingDirection;
            speedRatio = Mathf.Abs(carController.CalculateEngineRPMRatio());
        }
        if (speedRatio > LimiterEngage)
        {
            revLimiter = (Mathf.Sin(Time.time * LimiterFrequency) + 2f) * LimiterSound * (speedRatio - LimiterEngage);
        }
        if (isEngineRunning)
        {
            idleSound.volume = Mathf.Lerp(0.1f, idleMaxVolume, speedRatio);
            if (speedSign > 0)
            {
                reverseSound.volume = 0;
                idleSound.volume = Mathf.Lerp(idleSound.volume, 0.1f, speedRatio);
                runningSound.volume = Mathf.Lerp(0f, runningMaxVolume, speedRatio);
                runningSound.pitch = Mathf.Lerp(runningSound.pitch, Mathf.Lerp(0.3f, runningMaxPitch, speedRatio) + revLimiter, Time.deltaTime * pitchSmoothTime);
            }
            else
            {
                runningSound.volume = 0;
                idleSound.volume = Mathf.Lerp(0.1f, idleMaxVolume, speedRatio);
                reverseSound.volume = Mathf.Lerp(0f, reverseMaxVolume, speedRatio);
                reverseSound.pitch = Mathf.Lerp(reverseSound.pitch, Mathf.Lerp(0.2f, reverseMaxPitch, speedRatio) + revLimiter, Time.deltaTime * pitchSmoothTime);
            }
        }
        else
        {
            idleSound.volume = 0;
            runningSound.volume = 0;
        }
    }
    public IEnumerator StartEngine()
    {
        if (carController)
        {
            startingSound.Play();
            carController.isEngineRunning = 1;
            yield return new WaitForSeconds(0.6f);
            isEngineRunning = true;
            yield return new WaitForSeconds(0.4f);
            carController.isEngineRunning = 2;
        }
    }

    public IEnumerator StopEngine()
    {
        carController.isEngineRunning = 1;
        yield return new WaitForSeconds(1);
        isEngineRunning = false;
        carController.isEngineRunning = 0;
    }
}
