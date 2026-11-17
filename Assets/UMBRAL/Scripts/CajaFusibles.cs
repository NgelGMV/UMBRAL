using UnityEngine;

/// <summary>Caja eléctrica de una puerta sin corriente. Con sus tres fusibles colocados, la puerta se destraba.</summary>
public class CajaFusibles : Interactuable
{
    public PuertaInteractiva puerta;
    public GameObject[] fusibles;   // los tres fusibles dentro de la caja, ocultos hasta que se colocan
    public AudioSource sonido;
    public Light indicador;         // rojo; pasa a verde con los tres fusibles

    int colocados;

    public override bool Disponible { get { return colocados < fusibles.Length; } }

    public override string Mensaje
    {
        get
        {
            if (EstadoJuego.I.Fusibles > 0) return "Presiona E para colocar el fusible";
            return "Faltan fusibles: " + colocados + " de " + fusibles.Length + " colocados";
        }
    }

    public override void Interactuar(JugadorInteraccion jugador)
    {
        if (EstadoJuego.I.Fusibles <= 0 || colocados >= fusibles.Length) return;
        while (EstadoJuego.I.Fusibles > 0 && colocados < fusibles.Length)
        {
            EstadoJuego.I.UsarFusible();
            fusibles[colocados].SetActive(true);
            colocados++;
        }
        if (sonido != null) sonido.Play();
        if (colocados >= fusibles.Length)
        {
            if (indicador != null) indicador.color = new Color(0.25f, 1f, 0.35f);
            if (puerta != null) puerta.ActivarMecanismo();
        }
    }
}
