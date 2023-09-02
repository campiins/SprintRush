using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    private Rigidbody rb; // Componente Rigidbody del coche
    public enum Drivetrain // Tipos de transmisión del coche
    {
        RWD, // Rear Wheel Drive (Tracción trasera)
        FWD, // Front Wheel Drive (Tracción delantera)
        AWD  // All Wheel Drive (Tracción total)
    }
    public Drivetrain drivetrain; // Indica el tipo de transmision del coche
    public Vector3 centerOfMassOffset; // Desplazamiento del centro de masa del coche
    public float DownForceValue = 10f; // Valor de carga aerodinamica aplicada al coche
    private float wheelbase; // Distancia entre el eje delantero y trasero del coche
    private float reartrack; // Ancho del eje trasero del coche
    [HideInInspector] public int racePosition; // Posición del coche en carrera
    
    // Velocidad ------------------------------------------------------------------------
    [SerializeField] private float maxSpeed; // Velocidad máxima del coche
    [HideInInspector] public float speed; // Velocidad actual del coche
    [HideInInspector] public float speedClamped; // Velocidad actual del coche limitada

    [Header("Wheels")] // Ruedas -------------------------------------------------------
    public WheelColliders colliders; // Colliders de las ruedas del coche
    public WheelMeshes meshes; // Mallas de las ruedas del coche
    public int maxSteerAngle; // Máximo ángulo de giro de las ruedas
    public float turnRadius; // Distancia desde el centro del eje trasero hasta el centro de rotacion de las ruedas
    [Range(0, 1)] public float counterSteerFactor; // Factor de contragiro de las ruedas
    public float slipAllowance = 0.75f; // Margen de deslizamiento de las ruedas
    private float originalForwardStiffness; // Rigidez de deslizamiento frontal original
    private float originalSidewaysStiffness; // Rigidez de deslizamiento lateral original
    private bool isBraking; // Indica si el coche está frenando

    // Inputs --------------------------------------------------------------------------
    private float gasInput; // Entrada de aceleración
    private float brakeInput; // Entrada de freno
    private float steeringInput; // Entrada de direccón

    [Header("Engine")] // Motor --------------------------------------------------------
    [SerializeField ]private AnimationCurve enginePower; // Curva de potencia del motor
    private float totalPower; // Potencia total del motor
    [SerializeField] private float brakePower; // Fuerza de frenado

    [HideInInspector] public int isEngineRunning; // Indica si el motor está en marcha
    private float wheelsRPM; // RPM promedio de las ruedas
    private float engineRPM; // RPM del motor
    private float engineRPMClamped; // RPM del motor limitadas
    [SerializeField] private float maxRPM; // RPM maximas del motor
    [SerializeField] private float minRPM; // RPM minimas del motor

    private int currentGear; // Marcha actual
    [SerializeField] private float[] gearRatios; // Relación de marchas
    [SerializeField] private float[] upshiftSpeeds; // Velocidades para subir de marcha
    [SerializeField] private float[] downshiftSpeeds; // Velocidades para bajar de marcha
    private float finalDriveRatio = 3.6f; // Relacion de transmisión final

    // Control de movimiento -----------------------------------------------------------
    [HideInInspector] public float movingDirection; // Indica la dirección de movimiento del coche
    [HideInInspector] public bool canMove = false; // Indica si el coche puede moverse
    [HideInInspector] public bool reverse; // Indica si el coche va marcha atrás
    private float slipAngle; // Ángulo de deslizamiento del vehiculo

    // Detección de hierba -------------------------------------------------------------
    private int isTouchingGrass = 0; // Indica si el coche está tocando la hierba. 0 = Ninguna rueda toca hierba; -1/1 = Toca al menos una rueda izquierda/derecha; 2 = Tocan al menos una rueda izquierda y una derecha
    public int IsTouchingGrass { get { return isTouchingGrass; } }
    private float grassStiffness = 1f; // Rigidez de deslizamiento de las ruedas en la hierba

    // Efectos visuales y sonoros ------------------------------------------------------
    [HideInInspector] public WheelParticles wheelParticles; // Efectos de las ruedas
    private TrailRenderer[] wheelTrails; // Lista de marcas de las ruedas
    private float[] wheelTrailEmittedTime = new float[2]; // Tiempo de emision de la marca de las ruedas

    [Header("Visual and Sound Effects")]
    public GameObject tireTrail; // Marca de las ruedas
    public GameObject dustParticle; // Partícula de polvo
    public AudioSource skidSound; // Sonido de derrape
    public AudioSource grassSound; // Sonido de coche sobre hierba
    

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Calcular distancia entre eje delantero y trasero
        wheelbase = (Vector3.Distance(colliders.FLWheel.transform.position, colliders.RLWheel.transform.position) + Vector3.Distance(colliders.FRWheel.transform.position, colliders.RRWheel.transform.position)) / 2;
        // Calcular ancho del eje trasero
        reartrack = Vector3.Distance(colliders.RLWheel.transform.position, colliders.RRWheel.transform.position);

        // Obtener los valores originales de rigidez de deslizamiento de las ruedas
        originalForwardStiffness = colliders.FLWheel.forwardFriction.stiffness;
        originalSidewaysStiffness = colliders.FLWheel.sidewaysFriction.stiffness;
    }

    // Start is called before the first frame update
    void Start()
    {        
        rb.centerOfMass += centerOfMassOffset; // Aplicar deslizamiento del centro de masa

        engineRPM = 0; // Inicializar RPM a 0.
        currentGear = 1; // Inicializar marcha a primera

        // Instanciar efectos de las ruedas
        InstantiateParticles();
        wheelTrails = new TrailRenderer[]
        {
            wheelParticles.RRWheelTrail,
            wheelParticles.RLWheelTrail
        };
    }

    // Update is called once per frame
    void Update()
    {
        speed = rb.velocity.magnitude * 3.6f; // Calcular velocidad actual en KMH
        speedClamped = Mathf.Lerp(speedClamped, speed, Time.deltaTime * 5f); // Limitar velocidad actual

        CheckMovingDirection();
        ApplyWheelPositions();
        ChangeGear();
        CalculateEngineRPM();
        CheckTrails();
        CheckSlipSound();
        ControlBrakeLight();
        TouchingGrass();

        // Activar sonido cuando el coche va sobre la hierba
        if (IsTouchingGrass != 0 && GetSpeed() > 10f)
        {
            if (!grassSound.isPlaying)
            {
                grassSound.Play();
            }
        }
        else
        {
            grassSound.Stop();
        }
    }

    private void FixedUpdate()
    {
        if (canMove)
        {
            ApplyMotor();
            ApplySteering();
        }
        
        ApplyBrake();
        AddDownForce();
        IncreaseDragIfStopped();
    }

    // Establece los valores de entrada del controlador de coche y gestiona el sistema de frenos
    public void SetInputs(float throttle, float steering)
    {
        // Obtener inputs
        gasInput = throttle;
        steeringInput = steering;
        // Encender motor
        if (Mathf.Abs(gasInput) > 0 && isEngineRunning == 0)
        {
            StartCoroutine(GetComponent<EngineAudio>().StartEngine());
        }
        // Calcular angulo de deslizamiento del vehiculo
        slipAngle = Vector3.Angle(transform.forward, rb.velocity - transform.forward);
        // Calcular cuando el vehiculo debe frenar
        if (movingDirection < -0.5f && gasInput > 0)
        {
            brakeInput = Mathf.Abs(gasInput);
            isBraking = true;
            currentGear = 1; // Poner la marcha 1
        }
        else if (movingDirection > 0.5f && gasInput < 0)
        {
            brakeInput = Mathf.Abs(gasInput);
            isBraking = true;
        }
        else
        {
            brakeInput = 0;
            isBraking = false;
        }
    }

    // Aplica el torque del motor a las ruedas del coche basado en la entrada del acelerador y el tipo de transmisión
    void ApplyMotor()
    {
        if (isEngineRunning > 1) // si el motor esta encendido
        {
            if (Mathf.Abs(speed) < maxSpeed) // limitar velocidad
            {
                if (drivetrain == Drivetrain.FWD) // traccion delantera
                {
                    float torquePerWheel = totalPower * gasInput / 2f;
                    colliders.FLWheel.motorTorque = torquePerWheel;
                    colliders.FRWheel.motorTorque = torquePerWheel;
                }
                else if (drivetrain == Drivetrain.RWD) // traccion trasera
                {
                    float torquePerWheel = totalPower * gasInput / 2f;
                    colliders.RLWheel.motorTorque = torquePerWheel;
                    colliders.RRWheel.motorTorque = torquePerWheel;
                }
                else // traccion total
                {
                    float torquePerWheel = totalPower * gasInput / 4f;
                    colliders.FLWheel.motorTorque = torquePerWheel;
                    colliders.FRWheel.motorTorque = torquePerWheel;
                    colliders.RLWheel.motorTorque = torquePerWheel;
                    colliders.RRWheel.motorTorque = torquePerWheel;
                }
            }
            else // si el motor esta apagado
            {
                // no aplicar fuerza de motor
                colliders.FLWheel.motorTorque = 0;
                colliders.FRWheel.motorTorque = 0;
                colliders.RLWheel.motorTorque = 0;
                colliders.RRWheel.motorTorque = 0;
            }
        }
    }

    // Aplica fuerza de frenado a las ruedas del coche en función de la entrada de freno y ajusta la cantidad de frenado en superficies de hierba
    void ApplyBrake()
    {
        if (brakeInput > 0) // si debe frenar
        {
            // dejar de aplicar fuerza de motor
            colliders.FLWheel.motorTorque = 0;
            colliders.FRWheel.motorTorque = 0;
            colliders.RLWheel.motorTorque = 0;
            colliders.RRWheel.motorTorque = 0;
            // aplicar fuerza de frenado
            colliders.FLWheel.brakeTorque = brakeInput * brakePower * 0.7f;
            colliders.FRWheel.brakeTorque = brakeInput * brakePower * 0.7f;
            colliders.RLWheel.brakeTorque = brakeInput * brakePower * 0.3f;
            colliders.RRWheel.brakeTorque = brakeInput * brakePower * 0.3f;
        }
        else // si no debe frenar
        {
            // dejar de aplicar fuerza de frenado
            colliders.FLWheel.brakeTorque = 0;
            colliders.FRWheel.brakeTorque = 0;
            colliders.RLWheel.brakeTorque = 0;
            colliders.RRWheel.brakeTorque = 0;
        }

        float brakeMultiplier;
        if (speedClamped > 120)
        {
            brakeMultiplier = 0.4f;
        }
        else
        {
            brakeMultiplier = 0;
        }
        // Calcular la nueva cantidad de brakeTorque dependiendo del multiplicador y aplicarla
        float grassBrakeTorque = brakePower * brakeMultiplier;

        if (isTouchingGrass == 2) // si todas las ruedas tocan la hierba frenamos el coche
        {
            colliders.FLWheel.brakeTorque = Mathf.Max(colliders.FLWheel.brakeTorque, grassBrakeTorque);
            colliders.FRWheel.brakeTorque = Mathf.Max(colliders.FRWheel.brakeTorque, grassBrakeTorque);
            colliders.RLWheel.brakeTorque = Mathf.Max(colliders.RLWheel.brakeTorque, grassBrakeTorque);
            colliders.RRWheel.brakeTorque = Mathf.Max(colliders.RRWheel.brakeTorque, grassBrakeTorque);
        }
        else if (isTouchingGrass == -1)
        { // si alguna rueda izquierda toca la hierba frenamos el coche
            colliders.FLWheel.brakeTorque = Mathf.Max(colliders.FLWheel.brakeTorque, grassBrakeTorque);
            colliders.RLWheel.brakeTorque = Mathf.Max(colliders.RLWheel.brakeTorque, grassBrakeTorque);
        }
        else if (isTouchingGrass == 1) // si alguna rueda derecha toca la hierba frenamos el coche
        {
            colliders.FRWheel.brakeTorque = Mathf.Max(colliders.FRWheel.brakeTorque, grassBrakeTorque);
            colliders.RRWheel.brakeTorque = Mathf.Max(colliders.RRWheel.brakeTorque, grassBrakeTorque);
        }
    }

    // Implementa la dirección de Ackermann para aplicar ángulos de giro a las ruedas delanteras del coche
    void ApplySteering()
    {
        // Ackermann Steering
        float leftAngle, rightAngle;

        if (steeringInput < 0) // cuando gira a la izquierda
        {
            leftAngle = Mathf.Rad2Deg * Mathf.Atan(wheelbase / (turnRadius - (reartrack / 2))) * steeringInput;
            rightAngle = Mathf.Rad2Deg * Mathf.Atan(wheelbase / (turnRadius + (reartrack / 2))) * steeringInput;
        }
        else if (steeringInput > 0) // cuando gira a la derecha
        {
            leftAngle = Mathf.Rad2Deg * Mathf.Atan(wheelbase / (turnRadius + (reartrack / 2))) * steeringInput;
            rightAngle = Mathf.Rad2Deg * Mathf.Atan(wheelbase / (turnRadius - (reartrack / 2))) * steeringInput;
        }
        else // sin giro aplicado
        {
            leftAngle = 0;
            rightAngle = 0;
        }

        // Calcular ángulo de contraviraje para ayudar a estabilizar el vehiculo
        if (movingDirection > 0.5)
        {
            leftAngle += Vector3.SignedAngle(transform.forward, rb.velocity + transform.forward, Vector3.up) * Mathf.Clamp01(counterSteerFactor);
            rightAngle += Vector3.SignedAngle(transform.forward, rb.velocity + transform.forward, Vector3.up) * Mathf.Clamp01(counterSteerFactor);
        }

        // Aplicar angulo de giro a cada rueda delantera
        colliders.FLWheel.steerAngle = Mathf.Clamp(leftAngle, -maxSteerAngle, maxSteerAngle);
        colliders.FRWheel.steerAngle = Mathf.Clamp(rightAngle, -maxSteerAngle, maxSteerAngle);
    }

    // Verifica si todas las ruedas del coche están en contacto con el suelo
    public bool isGrounded()
    {
        if (colliders.FLWheel.isGrounded && colliders.FRWheel.isGrounded && colliders.RLWheel.isGrounded && colliders.RRWheel.isGrounded)
            return true;
        else
            return false;
    }

    // Calcula las RPM del motor del coche en función de las RPM de las ruedas y la potencia total considerando la relación de transmisión de la marcha actual y la evaluación de la potencia del motor
    public void CalculateEngineRPM()
    {
        GetWheelRPM();
        if (isEngineRunning > 0)
        {
            totalPower = enginePower.Evaluate(engineRPM) * gearRatios[currentGear];

            float velocity = 0.0f;
            if (engineRPM >= maxRPM) // Si las RPM del motor llegan a las RPM máximas
            {
                // Limitar RPM
                engineRPM = maxRPM;
                engineRPM = Mathf.SmoothDamp(engineRPM, maxRPM - 500, ref velocity, 0.005f);
            }
            else
            {
                // Si el motor está encendido, las RPM mínimas del motor serán de 1000

                engineRPM = Mathf.SmoothDamp(engineRPM, 900 + (Mathf.Abs(wheelsRPM) * finalDriveRatio * gearRatios[currentGear]), ref velocity, 0.01f);
                if (engineRPM > maxRPM) engineRPM = maxRPM; // Limitar RPM si supera las RPM máximas
            }
        }
        else
        {
            engineRPM = 0;
        }
    }

    // Devuelve la velocidad actual del coche
    public float GetSpeed()
    {
        return speed;
    }

    // Devuelve la velocidad máxima del coche
    public float GetMaxSpeed()
    {
        return maxSpeed;
    }

    // Modifica la velocidad máxima del coche
    public void SetMaxSpeed(float newSpeed)
    {
        maxSpeed = newSpeed;
    }

    // Devuelve el promedio de RPM de las 4 ruedas del coche
    void GetWheelRPM()
    {
        float sum = (colliders.FLWheel.rpm +
                colliders.FRWheel.rpm + colliders.RLWheel.rpm +
                colliders.RRWheel.rpm);
        wheelsRPM = sum / 4;
    }

    // Comprueba si es necesario subir de marcha basandose en la velocidad actual del coche
    private bool CheckUpshift()
    {
        if (GetSpeed() >= upshiftSpeeds[currentGear]) return true;
        else return false;
    }

    // Comprueba si es necesario bajar de marcha basandose en la velocidad actual del coche
    private bool CheckDownshift()
    {
        if (GetSpeed() <= downshiftSpeeds[currentGear]) return true;
        else return false;
    }

    // Gestiona el cambio de marchas del coche
    private void ChangeGear()
    {
        if (!isGrounded()) return;

        if (reverse) // Si el coche está yendo marcha atrás
        {
            currentGear = 0; // Aplicar la marcha atrás
        }
        else // Si el coche no va marcha atrás
        {
            if (currentGear < (gearRatios.Length - 1) && engineRPM > (maxRPM-500) && CheckUpshift()) // Si la marcha es menor a la última y las RPM llegan a las RPM de subida
            {
                currentGear++; // Subir de marcha
                return;
            }
            if (currentGear > 1 && engineRPM <= minRPM && CheckDownshift())
            {
                currentGear--; // Bajar de marcha
                return;
            }
        }
    }

    // Aplica una fuerza hacia abajo al coche para aumentar la adherencia al suelo
    private void AddDownForce()
    {
        rb.AddForce(transform.up * -DownForceValue * rb.velocity.magnitude);
    }

    // Ayuda a estabilizar el coche cuando está detenido, aumentando su resistencia angular
    private void IncreaseDragIfStopped()
    {
        if (gasInput == 0 && speedClamped <= 1)
        {
            rb.angularDrag = 50;
        }
        else
        {
            rb.angularDrag = 0.05f;
        }
    }

    // Instancia partículas y efectos visuales en las ruedas del coche
    private void InstantiateParticles()
    {
        if (dustParticle)
        {
            wheelParticles.FRWheelParticle = Instantiate(dustParticle, colliders.FRWheel.transform.position - Vector3.up * colliders.FRWheel.radius, Quaternion.identity, colliders.FRWheel.transform)
                .GetComponent<ParticleSystem>();
            wheelParticles.FLWheelParticle = Instantiate(dustParticle, colliders.FLWheel.transform.position - Vector3.up * colliders.FRWheel.radius, Quaternion.identity, colliders.FLWheel.transform)
                .GetComponent<ParticleSystem>();
            wheelParticles.RRWheelParticle = Instantiate(dustParticle, colliders.RRWheel.transform.position - Vector3.up * colliders.FRWheel.radius, Quaternion.identity, colliders.RRWheel.transform)
                .GetComponent<ParticleSystem>();
            wheelParticles.RLWheelParticle = Instantiate(dustParticle, colliders.RLWheel.transform.position - Vector3.up * (colliders.FRWheel.radius - 0.01f), Quaternion.identity, colliders.RLWheel.transform)
                .GetComponent<ParticleSystem>();
        }

        if (tireTrail)
        {
            wheelParticles.RRWheelTrail = Instantiate(tireTrail, colliders.RRWheel.transform.position - Vector3.up * colliders.FRWheel.radius, Quaternion.identity, colliders.RRWheel.transform)
                .GetComponent<TrailRenderer>();
            wheelParticles.RLWheelTrail = Instantiate(tireTrail, colliders.RLWheel.transform.position - Vector3.up * (colliders.FRWheel.radius - 0.01f), Quaternion.identity, colliders.RLWheel.transform)
                .GetComponent<TrailRenderer>();
        }
    }

    // Activa o desactiva los efectos visuales de los rastros de las ruedas en función de la cantidad de derrape
    void CheckTrails()
    {
        WheelHit[] wheelHits = new WheelHit[4];
        colliders.RRWheel.GetGroundHit(out wheelHits[2]);
        colliders.RLWheel.GetGroundHit(out wheelHits[3]);

        for (int i = 0; i < wheelTrails.Length; i++)
        {
            // Si la rueda está derrapando
            if ((Mathf.Abs(wheelHits[i + 2].sidewaysSlip) + Mathf.Abs(wheelHits[i + 2].forwardSlip)) > slipAllowance)
            {
                // Activar TrailRenderer y actualizar el tiempo de emision
                if (!wheelTrails[i].emitting)
                {
                    wheelTrails[i].emitting = true;
                    wheelTrailEmittedTime[i] = Time.time;
                }
            }
            else
            {
                // Si ha estado emitiendo durante al menos 0.25 segundos, detener la emision
                if (wheelTrails[i].emitting && Time.time - wheelTrailEmittedTime[i] >= 0.25f)
                {
                    wheelTrails[i].emitting = false;
                }
            }
        }
    }

    // Activa o desactiva el sonido de derrape en función de la cantidad de derrape
    void CheckSlipSound()
    {
        WheelHit[] wheelHits = new WheelHit[4];
        colliders.FRWheel.GetGroundHit(out wheelHits[0]);
        colliders.FLWheel.GetGroundHit(out wheelHits[1]);
        colliders.RRWheel.GetGroundHit(out wheelHits[2]);
        colliders.RLWheel.GetGroundHit(out wheelHits[3]);

        bool anyWheelSlipping = false;

        for (int i = 0; i < wheelHits.Length; i++)
        {
            if ((Mathf.Abs(wheelHits[i].sidewaysSlip) + Mathf.Abs(wheelHits[i].forwardSlip)) > slipAllowance)
            {
                anyWheelSlipping = true;
                break; // Si al menos una rueda esta derrapando, no necesitamos comprobar las demas ruedas
            }
        }

        if (anyWheelSlipping)
        {
            if (!skidSound.isPlaying && IsTouchingGrass == 0 && !GetComponent<TrackCheckpoints>().hasFinished)
            {
                skidSound.Play();
            }              
        }
        else
        {
            skidSound.Stop();
        }
    }

    // Activa o desactiva la emisión de la luz de freno en función de si el coche está frenando
    void ControlBrakeLight(){
        Transform bodyTransform = transform.Find("Body");
        if (bodyTransform != null)
        {
            if (isBraking)
            {
                Material brakeMaterial = bodyTransform.gameObject.GetComponent<Renderer>().materials[1];
                brakeMaterial.EnableKeyword("_EMISSION");
            }
            else
            {
                Material brakeMaterial = bodyTransform.gameObject.GetComponent<Renderer>().materials[1];
                brakeMaterial.DisableKeyword("_EMISSION");
            }
        }
    }

    // Detecta si las ruedas del coche están tocando la hierba y modifica los valores de fricción de las ruedas, además de controlar la reproducción de partículas de hierba
    private void TouchingGrass()
    {
        bool leftWheelsTouchingGrass = false;
        bool rightWheelsTouchingGrass = false;

        // Verificar si las ruedas izquierdas están tocando la hierba
        if (colliders.FLWheel.GetGroundHit(out WheelHit leftHit))
        {
            if (leftHit.collider.CompareTag("Grass"))
            {
                leftWheelsTouchingGrass = true;
                ModifyFrictionCurve(colliders.FLWheel, true); // Modificar los valores y guardar el estado original
                if (!wheelParticles.FLWheelParticle.isPlaying)
                {
                    wheelParticles.FLWheelParticle.Play();
                }
                else if (GetSpeed() < 50)
                {
                    wheelParticles.FLWheelParticle.Stop();
                }
            }
            else
            {
                wheelParticles.FLWheelParticle.Stop();
            }     
        }
        if (colliders.RLWheel.GetGroundHit(out WheelHit leftRearHit))
        {
            if (leftRearHit.collider.CompareTag("Grass"))
            {
                leftWheelsTouchingGrass = true;
                ModifyFrictionCurve(colliders.RLWheel, true);
                if (!wheelParticles.RLWheelParticle.isPlaying)
                {
                    wheelParticles.RLWheelParticle.Play();
                }
                else if (GetSpeed() < 50)
                {
                    wheelParticles.RLWheelParticle.Stop();
                }
            }
            else
            {
                wheelParticles.RLWheelParticle.Stop();
            }
        }

        // Verificar si las ruedas derechas están tocando la hierba
        if (colliders.FRWheel.GetGroundHit(out WheelHit rightHit))
        {
            if (rightHit.collider.CompareTag("Grass"))
            {
                rightWheelsTouchingGrass = true;
                ModifyFrictionCurve(colliders.FRWheel, true);
                if (!wheelParticles.FRWheelParticle.isPlaying)
                {
                    wheelParticles.FRWheelParticle.Play();
                }
                else if (GetSpeed() < 50)
                {
                    wheelParticles.FRWheelParticle.Stop();
                }
            }
            else
            {
                wheelParticles.FRWheelParticle.Stop();
            }
        }
        if (colliders.RRWheel.GetGroundHit(out WheelHit rightRearHit))
        {
            if (rightRearHit.collider.CompareTag("Grass"))
            {
                rightWheelsTouchingGrass = true;
                ModifyFrictionCurve(colliders.RRWheel, true);
                if (!wheelParticles.RRWheelParticle.isPlaying)
                {
                    wheelParticles.RRWheelParticle.Play();
                }
                else if (GetSpeed() < 50)
                {
                    wheelParticles.RRWheelParticle.Stop();
                }
            }
            else
            {
                wheelParticles.RRWheelParticle.Stop();
            }
        }

        // Actualizar la variable isTouchingGrass según qué ruedas están tocando la hierba
        if (leftWheelsTouchingGrass && rightWheelsTouchingGrass)
        {
            isTouchingGrass = 2; // Ambas ruedas están tocando la hierba
        }
        else if (leftWheelsTouchingGrass)
        {
            isTouchingGrass = -1; // Ruedas izquierdas están tocando la hierba
        }
        else if (rightWheelsTouchingGrass)
        {
            isTouchingGrass = 1; // Ruedas derechas están tocando la hierba
        }
        else
        {
            isTouchingGrass = 0; // Ninguna rueda está tocando la hierba, restaurar los valores originales
            ModifyFrictionCurve(colliders.FLWheel, false);
            ModifyFrictionCurve(colliders.FRWheel, false);
            ModifyFrictionCurve(colliders.RLWheel, false);
            ModifyFrictionCurve(colliders.RRWheel, false);
        }
    }

    // Modifica los valores de fricción de las ruedas
    private void ModifyFrictionCurve(WheelCollider wheelCollider, bool touchingGrass)
    {
        WheelFrictionCurve forwardFriction = wheelCollider.forwardFriction;
        WheelFrictionCurve sidewaysFriction = wheelCollider.sidewaysFriction;

        if (touchingGrass)
        {
            forwardFriction.stiffness = grassStiffness; // Modificar el valor de stiffness según tus necesidades
            sidewaysFriction.stiffness = grassStiffness; // Modificar el valor de stiffness según tus necesidades
        }
        else
        {
            forwardFriction.stiffness = originalForwardStiffness; // Restaurar el valor original de stiffness
            sidewaysFriction.stiffness = originalSidewaysStiffness; // Restaurar el valor original de stiffness
        }

        wheelCollider.forwardFriction = forwardFriction;
        wheelCollider.sidewaysFriction = sidewaysFriction;
    }

    // Actualiza la posición en carrera del coche
    public void UpdateRacePosition(int position)
    {
        racePosition = position;
    }

    // Devuelve la posición en carrera del coche
    public int GetRacePosition()
    {
        return racePosition;
    }

    // Aplica la posición y rotación de todas las ruedas
    private void ApplyWheelPositions()
    {
        UpdateWheel(colliders.FLWheel, meshes.FLWheel);
        UpdateWheel(colliders.FRWheel, meshes.FRWheel);
        UpdateWheel(colliders.RLWheel, meshes.RLWheel);
        UpdateWheel(colliders.RRWheel, meshes.RRWheel);
    }

    // Ajusta la posición y la rotación de la malla de la rueda con las del WheelCollider
    private void UpdateWheel(WheelCollider coll, MeshRenderer mesh)
    {
        Vector3 position;
        Quaternion rotation;
        coll.GetWorldPose(out position, out rotation);
        mesh.transform.position = position;
        mesh.transform.rotation = rotation;
    }

    // Comprueba si el coche va marcha atrás o no
    private void CheckMovingDirection()
    {
        // Calcular direccion de movimiento del vehiculo
        movingDirection = Vector3.Dot(transform.forward, rb.velocity);
        if (movingDirection < -0.5)
        {
            reverse = true;
        }
        else
        {
            reverse = false;
        }
    }

    // Calcula y devuelve una relación de RPM del motor ajustada, limitada por la entrada de aceleración y las RPM del motor
    public float CalculateEngineRPMRatio()
    {
        var gas = Mathf.Clamp(Mathf.Abs(gasInput), 0.5f, 1f);
        engineRPMClamped = Mathf.Lerp(engineRPMClamped, engineRPM, Time.deltaTime);
        return engineRPMClamped * gas / maxRPM;
    }

    // Detiene completamente el coche
    public void StopCompletely()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}

[System.Serializable]
public class WheelColliders
{
    public WheelCollider FLWheel;
    public WheelCollider FRWheel;
    public WheelCollider RLWheel;
    public WheelCollider RRWheel;
}

[System.Serializable]
public class WheelMeshes
{
    public MeshRenderer FLWheel;
    public MeshRenderer FRWheel;
    public MeshRenderer RLWheel;
    public MeshRenderer RRWheel;
}

[System.Serializable]
public class WheelParticles
{
    public ParticleSystem FLWheelParticle;
    public ParticleSystem FRWheelParticle;
    public ParticleSystem RLWheelParticle;
    public ParticleSystem RRWheelParticle;

    public TrailRenderer FLWheelTrail;
    public TrailRenderer FRWheelTrail;
    public TrailRenderer RLWheelTrail;
    public TrailRenderer RRWheelTrail;
}
