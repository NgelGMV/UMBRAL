using UnityEngine;

/// <summary>Panel con palanca que enciende un grupo de lámparas (sala final).</summary>
public class InterruptorLuz : Interactuable
{
    public Transform palanca;
    public LamparaPared[] lamparas;
    public AudioSource sonido;
    public Light indicador;   // piloto rojo para ubicar el panel a oscuras; pasa a verde al activarlo

    bool activado;

    public override bool Disponible { get { return !activado; } }
    public override string Mensaje { get { return "Presiona E para encender la luz"; } }

    void Start()
    {
        if (palanca != null) palanca.localRotation = Quaternion.Euler(-35f, 0f, 0f);
    }

    public override void Interactuar(JugadorInteraccion jugador)
    {
        if (activado) return;
        activado = true;
        if (palanca != null) palanca.localRotation = Quaternion.Euler(-145f, 0f, 0f);
        if (sonido != null) sonido.Play();
        if (indicador != null) indicador.color = new Color(0.25f, 1f, 0.35f);
        foreach (var l in lamparas) if (l != null) l.Encender();
    }
}
