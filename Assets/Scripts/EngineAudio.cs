using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineAudio : MonoBehaviour
{
    public AudioSource runningSound; // Fuente de audio del coche en marcha
    public float runningMaxVolume; // Volumen maximo del coche en marcha
    public float runningMaxPitch; // Pitch maximo del coche en marcha 
    
    public AudioSource idleSound; // Fuente de audio del coche en ralentí
    public float idleMaxVolume; // Volumen maximo del coche en ralentí

    public AudioSource reverseSound; // Fuente de audio del coche en marcha atrás
    public float reverseMaxVolume; // Volumen maximo del coche en marcha atrás
    public float reverseMaxPitch; // Pitch maximo del coche en marcha atrás
    
    [SerializeField] private float pitchSmoothTime; // Tiempo de suavizado del pitch

    private float engineRPMRatio; // Relación de las RPM del motor ajustadas

    private float revLimiter; // Limitador de revoluciones
    public float LimiterSound = 3f; // Valor multiplicador del limitador
    public float LimiterFrequency = 10f; // Frecuencia del limitador
    public float LimiterEngage = 0.8f; // Valor de activación del limitador
    
    public bool isEngineRunning = false; // Indica si el motor está encendido
    public AudioSource startingSound; // Fuente de audio de arranque del motor

    [HideInInspector] public CarController carController; // Referencia a la clase CarController

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
        float speedSign = 0; // Dirección de movimiento del coche
        if (carController)
        {
            speedSign = carController.movingDirection;
            engineRPMRatio = Mathf.Abs(carController.CalculateEngineRPMRatio());
        }
        if (engineRPMRatio > LimiterEngage)
        {
            revLimiter = (Mathf.Sin(Time.time * LimiterFrequency) + 2f) * LimiterSound * (engineRPMRatio - LimiterEngage);
        }
        if (isEngineRunning)
        {
            idleSound.volume = Mathf.Lerp(0.1f, idleMaxVolume, engineRPMRatio);
            if (speedSign > 0)
            {
                reverseSound.volume = 0;
                idleSound.volume = Mathf.Lerp(idleSound.volume, 0.1f, engineRPMRatio);
                runningSound.volume = Mathf.Lerp(0f, runningMaxVolume, engineRPMRatio);
                runningSound.pitch = Mathf.Lerp(runningSound.pitch, Mathf.Lerp(0.3f, runningMaxPitch, engineRPMRatio) + revLimiter, Time.deltaTime * pitchSmoothTime);
            }
            else
            {
                runningSound.volume = 0;
                idleSound.volume = Mathf.Lerp(0.1f, idleMaxVolume, engineRPMRatio);
                reverseSound.volume = Mathf.Lerp(0f, reverseMaxVolume, engineRPMRatio);
                reverseSound.pitch = Mathf.Lerp(reverseSound.pitch, Mathf.Lerp(0.2f, reverseMaxPitch, engineRPMRatio) + revLimiter, Time.deltaTime * pitchSmoothTime);
            }
        }
        else
        {
            idleSound.volume = 0;
            runningSound.volume = 0;
        }
    }

    // Corrutina para encender el motor
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

    // Corrutina para detener el motor
    public IEnumerator StopEngine()
    {
        carController.isEngineRunning = 1;
        yield return new WaitForSeconds(1);
        isEngineRunning = false;
        carController.isEngineRunning = 0;
    }
}
