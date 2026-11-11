using UnityEngine;

/// <summary>
/// Base de todo objeto con el que se interactúa con la tecla E.
/// Cada objeto lleva un Collider marcado como Trigger que define su área de detección.
/// </summary>
public abstract class Interactuable : MonoBehaviour
{
    public Transform puntoFoco;

    public Vector3 Foco { get { return puntoFoco != null ? puntoFoco.position : transform.position; } }

    /// <summary>Texto que se muestra mientras el jugador está dentro del área de detección.</summary>
    public abstract string Mensaje { get; }

    /// <summary>Si es falso, el objeto ya no responde (por ejemplo, una puerta que ya se abrió).</summary>
    public virtual bool Disponible { get { return true; } }

    public abstract void Interactuar(JugadorInteraccion jugador);
}
