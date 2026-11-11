using UnityEngine;

/// <summary>Zona de salida del nivel. Al cruzarla el jugador completa el nivel.</summary>
[RequireComponent(typeof(BoxCollider))]
public class MetaNivel : MonoBehaviour
{
    public Transform jugador;
    BoxCollider zona;

    void Awake() { zona = GetComponent<BoxCollider>(); zona.isTrigger = true; }

    void Update()
    {
        if (jugador != null && zona.bounds.Contains(jugador.position + Vector3.up))
            EstadoJuego.I.Ganar();
    }
}
