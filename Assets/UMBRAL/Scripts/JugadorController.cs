using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Movimiento en primera persona: WASD para caminar, Shift para correr, ratón para mirar.</summary>
[RequireComponent(typeof(CharacterController))]
public class JugadorController : MonoBehaviour
{
    public Transform camara;
    public float velocidad = 3.2f;
    public float velocidadCorrer = 5.2f;
    public float sensibilidad = 0.12f;
    public float gravedad = -18f;

    [Header("Pasos")]
    public AudioSource pasos;
    public AudioClip[] clipsPasos;
    public float zancada = 1.6f;        // metros entre un paso y el siguiente al caminar

    public bool Oculto { get; private set; }
    [HideInInspector] public bool Bloqueado;

    CharacterController cc;
    float inclinacion;
    float velocidadY;
    float recorrido;

    void Awake() { cc = GetComponent<CharacterController>(); }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        var kb = Keyboard.current;
        var raton = Mouse.current;
        if (kb == null || Bloqueado) return;

        if (raton != null)
        {
            if (Cursor.lockState != CursorLockMode.Locked && raton.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 d = raton.delta.ReadValue() * sensibilidad;
                transform.Rotate(0f, d.x, 0f);
                inclinacion = Mathf.Clamp(inclinacion - d.y, -80f, 80f);
                camara.localEulerAngles = new Vector3(inclinacion, 0f, 0f);
            }
        }

        if (Oculto) return;

        float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
        float z = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
        Vector3 dir = transform.right * x + transform.forward * z;
        if (dir.sqrMagnitude > 1f) dir.Normalize();
        float v = kb.leftShiftKey.isPressed ? velocidadCorrer : velocidad;

        if (cc.isGrounded && velocidadY < 0f) velocidadY = -2f;
        velocidadY += gravedad * Time.deltaTime;
        cc.Move((dir * v + Vector3.up * velocidadY) * Time.deltaTime);

        // Pasos: suena uno cada vez que el jugador avanza una zancada sobre el suelo
        bool corriendo = v > velocidad;
        Vector3 plano = cc.velocity; plano.y = 0f;
        if (cc.isGrounded && plano.sqrMagnitude > 0.25f)
        {
            recorrido += plano.magnitude * Time.deltaTime;
            if (recorrido >= zancada * (corriendo ? 1.25f : 1f))
            {
                recorrido = 0f;
                if (pasos != null && clipsPasos != null && clipsPasos.Length > 0)
                {
                    pasos.pitch = Random.Range(0.9f, 1.1f);
                    pasos.PlayOneShot(clipsPasos[Random.Range(0, clipsPasos.Length)], corriendo ? 1f : 0.65f);
                }
            }
        }
        else if (plano.sqrMagnitude < 0.01f) recorrido = zancada * 0.6f;   // el primer paso al arrancar suena pronto
    }

    /// <summary>Mete al jugador en un escondite: se queda quieto y solo puede mirar.</summary>
    public void Ocultar(Vector3 posicion, Quaternion rotacion)
    {
        Oculto = true;
        cc.enabled = false;
        transform.SetPositionAndRotation(posicion, rotacion);
        inclinacion = 0f;
        camara.localEulerAngles = Vector3.zero;
    }

    public void SalirDeEscondite(Vector3 posicion, Quaternion rotacion)
    {
        transform.SetPositionAndRotation(posicion, rotacion);
        cc.enabled = true;
        Oculto = false;
    }

    public void Teletransportar(Vector3 posicion, float giroY)
    {
        cc.enabled = false;
        transform.SetPositionAndRotation(posicion, Quaternion.Euler(0f, giroY, 0f));
        cc.enabled = true;
    }
}
