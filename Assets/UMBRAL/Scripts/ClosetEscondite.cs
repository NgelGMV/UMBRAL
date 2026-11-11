using System.Collections;
using UnityEngine;

/// <summary>Clóset donde el jugador se esconde de las entidades. E para entrar y E para salir.</summary>
public class ClosetEscondite : Interactuable
{
    public Transform hojaIzq;
    public Transform hojaDer;
    public AudioSource sonido;

    bool ocupado;

    public override string Mensaje { get { return ocupado ? "Presiona E para salir" : "Presiona E para esconderte"; } }

    public override void Interactuar(JugadorInteraccion jugador)
    {
        Quaternion mirarAfuera = transform.rotation * Quaternion.Euler(0f, 180f, 0f);
        if (!ocupado)
        {
            ocupado = true;
            jugador.fijado = this;
            jugador.controlador.Ocultar(transform.TransformPoint(0f, 0.09f, 0.03f), mirarAfuera);
        }
        else
        {
            ocupado = false;
            jugador.fijado = null;
            jugador.controlador.SalirDeEscondite(transform.TransformPoint(0f, 0.05f, -0.95f), mirarAfuera);
        }
        if (sonido != null) sonido.Play();
        StopAllCoroutines();
        StartCoroutine(AbrirYCerrar());
    }

    IEnumerator AbrirYCerrar()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.55f;
            float a = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * 70f;
            if (hojaIzq != null) hojaIzq.localRotation = Quaternion.Euler(0f, a, 0f);
            if (hojaDer != null) hojaDer.localRotation = Quaternion.Euler(0f, -a, 0f);
            yield return null;
        }
        if (hojaIzq != null) hojaIzq.localRotation = Quaternion.identity;
        if (hojaDer != null) hojaDer.localRotation = Quaternion.identity;
    }
}
