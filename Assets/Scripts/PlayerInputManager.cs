using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerInputManager : MonoBehaviour
{
    // --- Referencias 
    private Rigidbody PlayerRB;
    private GameObject playerLight;
    public GameObject Camera;               
    public CinemachineFreeLook freeLook;      
    public Transform playerBody;              // Cuerpo del player

    // --- Movimiento 
    [Header("Movimiento")]
    public float maxSpeed   = 6f;   // m/s sobre el plano perpendicular al Up
    public float maxAccel   = 20f;  // m/s^2 al acelerar
    public float brakeAccel = 30f;  // m/s^2 al soltar, osease el frenon jajajaja estoy locooo
    public float rotationSpeed = 5f;

    // --- Input cache 
    private float _h, _v;
	//Si alguien lee este comentario y no entiende por qué hay un _h y un h, es porque 
	//me estoy volviendo locooo
    // --- Cinemachine World Up 
    [Header("Cinemachine World Up")]
    public Transform worldUpRef;             
    private CinemachineBrain brain;

    void Start()
    {
        PlayerRB = GetComponent<Rigidbody>();
        playerLight = GameObject.Find("PlayerLight");
        if (Camera == null) Camera = GameObject.Find("Main Camera");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Configurar WorldUpOverride para Cinemachine
        brain = Camera ? Camera.GetComponent<CinemachineBrain>() : null;
        if (worldUpRef == null)
        {
            var go = new GameObject("WorldUpRef");
            worldUpRef = go.transform;
        }
        if (brain != null) brain.m_WorldUpOverride = worldUpRef;

        // Inicializa el Up según la gravedad actual
        UpdateWorldUpFromGravity();

        if (PlayerRB != null)
        {
            PlayerRB.linearDamping = Mathf.Max(PlayerRB.linearDamping, 0.2f);
            PlayerRB.angularDamping = Mathf.Max(PlayerRB.angularDamping, 0.05f);
        }
    }

    void UpdateWorldUpFromGravity()
    {
        if (worldUpRef == null) return;

        // El +Y de worldUpRef apunta en contra de la gravedad
        Vector3 desiredUp = -Physics.gravity.normalized;

        // Alinea el Y local con desiredUp
        worldUpRef.rotation = Quaternion.FromToRotation(Vector3.up, desiredUp);
    }

    Vector3 CurrentUp()
    {
        return Physics.gravity.sqrMagnitude > 1e-6f ? -Physics.gravity.normalized : Vector3.up;
    }

    void UpdateWorldUp(Vector3 newUp)
    {
        if (worldUpRef != null)
            worldUpRef.up = newUp;
    }

    void Update()
    {
        // Lee input (en Update)
        _h = Input.GetAxis("Horizontal");
        _v = Input.GetAxis("Vertical");

        // Luz siguiendo al jugador
        if (playerLight)
            playerLight.transform.position = transform.position + new Vector3(0, 0.5f, 0);

        // Rotar el cuerpo hacia la dirección de la cámara en el plano
        if (freeLook && playerBody)
        {
            Vector3 up = CurrentUp();
            Vector3 camFwdOnPlane = Vector3.ProjectOnPlane(Camera.transform.forward, up).normalized;
            if (camFwdOnPlane.sqrMagnitude > 1e-6f)
            {
                Quaternion target = Quaternion.LookRotation(camFwdOnPlane, up);
                playerBody.rotation = Quaternion.Slerp(playerBody.rotation, target, rotationSpeed * Time.deltaTime);
            }
        }
    }

    void FixedUpdate()
    {
        if (PlayerRB == null || Camera == null) return;

        Vector3 up = CurrentUp();

        // Ejes de la cámara sobre el plano a "up"
        Transform camT = Camera.transform;
        Vector3 forward = Vector3.ProjectOnPlane(camT.forward, up).normalized;
        if (forward.sqrMagnitude < 1e-6f) 
            forward = Vector3.ProjectOnPlane(playerBody ? playerBody.forward : transform.forward, up).normalized;

        Vector3 right = Vector3.Cross(up, forward).normalized;

        // Dirección de movimiento en el plano
        Vector3 moveDir = (forward * _v + right * _h);
        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        // Velocidad deseada en el plano
        Vector3 targetVel = moveDir * maxSpeed;

        // Velocidad actual 
        Vector3 vel = PlayerRB.linearVelocity;
        Vector3 velOnPlane = Vector3.ProjectOnPlane(vel, up);

        // Delta de velocidad que queremos lograr
        Vector3 deltaV = targetVel - velOnPlane;

        // Límite de aceleración
        float accelCap = (moveDir.sqrMagnitude > 1e-6f) ? maxAccel : brakeAccel;

        // Aceleración necesaria limitada
        Vector3 neededAccel = Vector3.ClampMagnitude(deltaV / Time.fixedDeltaTime, accelCap);
        PlayerRB.AddForce(neededAccel * PlayerRB.mass, ForceMode.Force);

        // orientar el cuerpo hacia la marcha
        if (playerBody && moveDir.sqrMagnitude > 1e-6f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, up);
            playerBody.rotation = Quaternion.Slerp(playerBody.rotation, targetRot, 10f * Time.fixedDeltaTime);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("NewGravTrigger"))
        {
            SetGravity(new Vector3(0f, 0f, 9.81f));
            other.gameObject.SetActive(false);
        }
    }

    void SetGravity(Vector3 newGravity)
    {
        if (PlayerRB == null) return;

        // 1) Frenar al jugador
        PlayerRB.linearVelocity = Vector3.zero;
        PlayerRB.angularVelocity = Vector3.zero;

        // 2) Cambiar gravedad
        Physics.gravity = newGravity;

        // 3) Reorientar el player usando el up correcto
        Vector3 upRel = -newGravity.normalized;
        if (upRel.sqrMagnitude > 0.5f)
        {
            Vector3 fwdProjected = Vector3.ProjectOnPlane(transform.forward, upRel).normalized;
            if (fwdProjected.sqrMagnitude < 0.01f) fwdProjected = Vector3.Cross(upRel, transform.right).normalized;
            transform.rotation = Quaternion.LookRotation(fwdProjected, upRel);
        }

        // 4) Actualizar el "World Up" de Cinemachine
        UpdateWorldUpFromGravity();
        UpdateWorldUp(-newGravity.normalized);
    }
}
