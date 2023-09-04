using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CarController))]
public class InputManager : MonoBehaviour
{
    private VehicleInputs input; // Referencia a la clase VehicleInputs generada por el set de acciones de inputs
    private CarController carController; // Referencia a la clase CarController
    private TrackCheckpoints trackCheckpoints; // Referencia a la clase TrackCheckpoints
    private UIManager UIManager; // Referencia a la clase UIManager
    private PlayerCam playerCam; // Referencia a la clase PlayerCam

    private float throttleInput; // Entrada del acelerador
    private float steeringInput; // Entrada de dirección
    private float throttleDamp; // Valor de amortiguación del acelerador
    private float steeringDamp; // Valor de amortiguación de la dirección

    public bool isRearview; // Indica si el jugador esta usando la camara trasera
    public bool canUseInput = true; // Indica si el jugador puede controlar el coche
    private bool isPaused = false; // Indica si el juego esta en pausa

    private Coroutine fillCoroutine; // Variable para guardar la referencia a la corrutina
    public float dampenSpeed = 5f; // Velocidad de suavizado para cambios de movimiento

    // Start is called before the first frame update
    void Awake()
    {
        input = new VehicleInputs();
        carController = GetComponent<CarController>();
        trackCheckpoints = GetComponent<TrackCheckpoints>();
        playerCam = FindObjectOfType<PlayerCam>();
        UIManager = FindObjectOfType<UIManager>();
    }

    // Update is called once per frame
    void Update()
    {
        throttleDamp = DampenedInput(throttleInput, throttleDamp);
        steeringDamp = DampenedInput(steeringInput, steeringDamp);

        // Aplicar inputs del controlador del coche
        if (canUseInput)
        {
            if (carController.speed < -1.5f || carController.speed > 1.5f)
            {
                if (throttleDamp < 0.01f && throttleDamp > -0.01f) throttleDamp = 0;
                carController.SetInputs(throttleDamp, steeringDamp);
            }
            else
            {
                // Corrige un error que hace que el coche se mueva de lado al estar parado con el motor encendido
                carController.SetInputs(throttleInput, steeringDamp);
            }
        }
    }

    private void OnEnable()
    {
        input.Enable();

        input.Car.Throttle.performed += ApplyThrottle;
        input.Car.Throttle.canceled += ReleaseThrottle;

        input.Car.Steering.performed += ApplySteering;
        input.Car.Steering.canceled += ReleaseSteering;

        if (carController.gameObject.CompareTag("Player"))
        {
            input.Car.Reset.started += StartReset;
            input.Car.Reset.performed += ApplyReset;
            input.Car.Reset.canceled += ReleaseReset;
        }

        input.Car.Camera.performed += ChangeCamera;
        input.Car.Camera.canceled -= ChangeCamera;

        input.Car.Rearview.performed += Rearview;
        input.Car.Rearview.canceled += ReleaseRearview;

        input.Car.Pause.performed += Pause;
    }

    private void OnDisable()
    {
        input.Disable();
    }

    // Suavizar input
    private float DampenedInput(float input, float output)
    {
        return Mathf.Lerp(output, input, Time.deltaTime * dampenSpeed);
    }

    // Aplicar acelerador
    private void ApplyThrottle(InputAction.CallbackContext context)
    {
        if (carController.gameObject.CompareTag("Player"))
        {
            throttleInput = context.ReadValue<float>();
        }
    }

    // Soltar entrada de acelerador
    private void ReleaseThrottle(InputAction.CallbackContext context)
    {
        throttleInput = 0f;
    }

    // Aplicar dirección
    private void ApplySteering(InputAction.CallbackContext context)
    {
        if (carController.gameObject.CompareTag("Player"))
        {
            steeringInput = context.ReadValue<float>();
        }
    }

    // Soltar entrada de dirección
    private void ReleaseSteering(InputAction.CallbackContext context)
    {
        steeringInput = 0f;
    }

    // Cambiar camara
    private void ChangeCamera(InputAction.CallbackContext context)
    {
        playerCam.ChangeCamera();
    }

    // Aplicar camara trasera
    private void Rearview(InputAction.CallbackContext context)
    {
        isRearview = true;
    }

    // Detener camara trasera
    private void ReleaseRearview(InputAction.CallbackContext context)
    {
        isRearview = false;
    }

    // Pausar o reanudar el juego
    private void Pause(InputAction.CallbackContext context)
    {
        PauseGame pauseGame = FindObjectOfType<PauseGame>();
        if (isPaused)
        {
            pauseGame.Resume();
            isPaused = false;
        }
        else
        {
            pauseGame.Pause();
            isPaused = true;
        }
    }

    // Aplicar restablecimiento
    private void ApplyReset(InputAction.CallbackContext context)
    {
        trackCheckpoints.ResetPosition();
        // Detener la corrutina si está activa
        if (fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
            fillCoroutine = null;
        }
        UIManager.resetBar.fillAmount = 0;
        UIManager.resetBar.gameObject.transform.parent.gameObject.SetActive(false);
    }

    // Detener barra de restablecimiento si se suelta el input de restablecimiento
    private void ReleaseReset(InputAction.CallbackContext context)
    {
        // Detener la corrutina si está activa
        if (fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
            fillCoroutine = null;
        }
        UIManager.resetBar.fillAmount = 0;
        UIManager.resetBar.gameObject.transform.parent.gameObject.SetActive(false);
    }

    // Llenar la barra de restablecimiento si se mantiene pulsado el input de restablecimiento
    private void StartReset(InputAction.CallbackContext context)
    {
        UIManager.resetBar.gameObject.transform.parent.gameObject.SetActive(true);
        // Comenzar la corrutina solo si no está activa actualmente
        if (fillCoroutine == null)
        {
            fillCoroutine = StartCoroutine(FillResetBarOverTime(1f));
        }
    }

    // Corrutina para llenar la barra circular de restablecimiento del coche
    private IEnumerator FillResetBarOverTime(float duration)
    {
        float elapsedTime = 0;
        float startFillAmount = UIManager.resetBar.fillAmount;
        float targetFillAmount = 1f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            float currentFillAmount = Mathf.Lerp(startFillAmount, targetFillAmount, t);
            UIManager.resetBar.fillAmount = currentFillAmount;
            yield return null;
        }

        UIManager.resetBar.fillAmount = targetFillAmount;
    }

}
