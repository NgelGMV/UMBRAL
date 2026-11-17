using UnityEngine;

/// <summary>Fusible suelto que el jugador recoge con E para llevarlo a la caja de fusibles.</summary>
public class Fusible : Interactuable
{
    public override string Mensaje { get { return "Presiona E para tomar el fusible"; } }

    public override void Interactuar(JugadorInteraccion jugador)
    {
        EstadoJuego.I.TomarFusible();
        gameObject.SetActive(false);
    }
}
