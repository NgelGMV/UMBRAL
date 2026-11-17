using UnityEngine;

/// <summary>Palanca escondida en la habitación. Al bajarla activa un mecanismo de su puerta.</summary>
public class PalancaPuerta : Interactuable
{
    public Transform palanca;
    public PuertaInteractiva puerta;
    public AudioSource sonido;
    public Light indicador;   // piloto ámbar; pasa a verde al bajarla

    bool activada;

    public override bool Disponible { get { return !activada; } }
    public override string Mensaje { get { return "Presiona E para bajar la palanca"; } }

    void Start()
    {
        if (palanca != null) palanca.localRotation = Quaternion.Euler(-35f, 0f, 0f);
    }

    public override void Interactuar(JugadorInteraccion jugador)
    {
        if (activada) return;
        activada = true;
        if (palanca != null) palanca.localRotation = Quaternion.Euler(-145f, 0f, 0f);
        if (sonido != null) sonido.Play();
        if (indicador != null) indicador.color = new Color(0.25f, 1f, 0.35f);
        if (puerta != null) puerta.ActivarMecanismo();
    }
}
