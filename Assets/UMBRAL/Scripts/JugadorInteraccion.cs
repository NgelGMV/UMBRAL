using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Detecta en qué área de detección (Trigger) está el jugador, muestra el mensaje en pantalla
/// y ejecuta la interacción cuando se presiona la tecla E.
/// </summary>
public class JugadorInteraccion : MonoBehaviour
{
    public JugadorController controlador;
    public Transform camara;

    public Interactuable Actual { get; private set; }
    [HideInInspector] public Interactuable fijado;   // escondite en uso

    static readonly Collider[] buffer = new Collider[24];

    void Update()
    {
        if (EstadoJuego.I.Terminado)
        {
            HudJuego.I.MostrarAviso("");
            return;
        }

        Actual = fijado != null ? fijado : Buscar();
        HudJuego.I.MostrarAviso(Actual != null ? Actual.Mensaje : "");

        var kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame) Interactuar();
    }

    public void Interactuar()
    {
        if (Actual != null && !EstadoJuego.I.Terminado) Actual.Interactuar(this);
    }

    public Interactuable Buscar()
    {
        Vector3 centro = transform.position + Vector3.up * 1.0f;
        int n = Physics.OverlapSphereNonAlloc(centro, 0.35f, buffer, ~0, QueryTriggerInteraction.Collide);
        Interactuable mejor = null;
        float mejorPuntaje = float.NegativeInfinity;
        for (int i = 0; i < n; i++)
        {
            if (!buffer[i].isTrigger) continue;
            var it = buffer[i].GetComponentInParent<Interactuable>();
            if (it == null || !it.Disponible) continue;
            Vector3 d = it.Foco - camara.position;
            float dist = Mathf.Max(d.magnitude, 0.01f);
            float puntaje = Vector3.Dot(camara.forward, d / dist) - 0.15f * dist;
            if (puntaje > mejorPuntaje) { mejorPuntaje = puntaje; mejor = it; }
        }
        return mejor;
    }
}
