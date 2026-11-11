using System.Collections;
using UnityEngine;

/// <summary>Mesa de noche con cajón. Al iniciar cada piso, EstadoJuego guarda la llave en una de ellas al azar.</summary>
public class MesaDeNoche : Interactuable
{
    public Transform cajon;
    public GameObject llave;
    public bool contieneLlave;
    public float recorrido = 0.28f;
    public AudioSource sonido;
    public AudioClip sonidoLlave;

    bool abierto;
    bool moviendo;
    float avisoHasta;

    /// <summary>Marca este cajón como el que guarda la llave del piso.</summary>
    public void GuardarLlave()
    {
        contieneLlave = true;
        if (llave != null) llave.SetActive(true);
    }

    public override bool Disponible
    {
        get { return !abierto || moviendo || (contieneLlave && llave != null && llave.activeSelf) || Time.time < avisoHasta; }
    }

    public override string Mensaje
    {
        get
        {
            if (moviendo) return "";
            if (!abierto) return "Presiona E para abrir el cajón";
            if (contieneLlave && llave != null && llave.activeSelf) return "Presiona E para tomar la llave";
            return Time.time < avisoHasta ? "El cajón está vacío" : "";
        }
    }

    public override void Interactuar(JugadorInteraccion jugador)
    {
        if (moviendo) return;
        if (!abierto)
        {
            StartCoroutine(Abrir());
        }
        else if (contieneLlave && llave != null && llave.activeSelf)
        {
            llave.SetActive(false);
            if (sonido != null && sonidoLlave != null) sonido.PlayOneShot(sonidoLlave);
            EstadoJuego.I.DarLlave();
        }
    }

    IEnumerator Abrir()
    {
        moviendo = true;
        if (sonido != null) sonido.Play();
        Vector3 a = cajon.localPosition;
        Vector3 b = a + new Vector3(0f, 0f, -recorrido);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.35f;
            cajon.localPosition = Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        cajon.localPosition = b;
        abierto = true;
        moviendo = false;
        if (!contieneLlave) avisoHasta = Time.time + 2f;
    }
}
