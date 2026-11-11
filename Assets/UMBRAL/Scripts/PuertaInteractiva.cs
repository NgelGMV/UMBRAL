using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Puerta numerada. Sigue el diagrama de flujo del primer avance:
/// Trigger -> "Presiona E" -> tecla E -> (¿seguro? ¿llave?) -> rota 90° sobre su pivote -> contador +1.
/// El pivote del modelo está en la bisagra, por eso basta con rotar la hoja sobre su eje Y.
/// </summary>
public class PuertaInteractiva : Interactuable
{
    [Header("Referencias")]
    public Transform hoja;              // Modelo INT_Puerta (pivote en la bisagra)
    public Collider areaDeteccion;      // Trigger de 2.0 x 2.5 x 2.0 m
    public AudioSource sonido;
    public AudioClip sonidoCerrada;     // manija que traba cuando la puerta tiene seguro

    [Header("Configuración")]
    public string numero = "001";
    public float anguloApertura = 90f;  // El signo depende de hacia qué lado abre la puerta
    public float duracion = 0.6f;
    public bool tieneSeguro = false;
    public UnityEvent alAbrir;

    public bool Abierta { get; private set; }
    float avisoHasta;

    public override bool Disponible { get { return !Abierta; } }

    public override string Mensaje
    {
        get { return Time.time < avisoHasta ? "Está cerrada. Necesitas una llave" : "Presiona E"; }
    }

    public override void Interactuar(JugadorInteraccion jugador)
    {
        if (Abierta) return;

        if (tieneSeguro)
        {
            if (!EstadoJuego.I.TieneLlave)
            {
                avisoHasta = Time.time + 2.5f;
                if (sonido != null && sonidoCerrada != null) sonido.PlayOneShot(sonidoCerrada);
                return;
            }
            EstadoJuego.I.UsarLlave();
            tieneSeguro = false;
        }

        Abierta = true;
        if (areaDeteccion != null) areaDeteccion.enabled = false;
        StartCoroutine(Rotar());
    }

    IEnumerator Rotar()
    {
        if (sonido != null) sonido.Play();

        Quaternion inicio = hoja.localRotation;
        Quaternion fin = inicio * Quaternion.Euler(0f, anguloApertura, 0f);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duracion;
            hoja.localRotation = Quaternion.Slerp(inicio, fin, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        hoja.localRotation = fin;

        EstadoJuego.I.RegistrarPuerta(this);
        if (alAbrir != null) alAbrir.Invoke();
    }
}
