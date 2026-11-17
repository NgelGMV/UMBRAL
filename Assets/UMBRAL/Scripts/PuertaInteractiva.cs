using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Qué hace falta para abrir una puerta.</summary>
public enum Cerradura { Ninguna, Llave, Mecanismo }

/// <summary>
/// Puerta numerada. Sigue el diagrama de flujo del primer avance:
/// Trigger -> "Presiona E" -> tecla E -> (¿está trabada?) -> rota 90° sobre su pivote -> contador +1.
/// El pivote del modelo está en la bisagra, por eso basta con rotar la hoja sobre su eje Y.
/// Según la habitación, la puerta abre directo, pide una llave o espera a que se active un mecanismo
/// (palanca, teclado con código o caja de fusibles).
/// </summary>
public class PuertaInteractiva : Interactuable
{
    [Header("Referencias")]
    public Transform hoja;              // Modelo INT_Puerta (pivote en la bisagra)
    public Collider areaDeteccion;      // Trigger de 2.0 x 2.5 x 2.0 m
    public AudioSource sonido;
    public AudioClip sonidoCerrada;     // manija que traba
    public AudioClip sonidoDesbloqueo;  // cerrojo que se suelta

    [Header("Configuración")]
    public string numero = "001";
    public float anguloApertura = 90f;  // El signo depende de hacia qué lado abre la puerta
    public float duracion = 0.6f;
    public Cerradura cerradura = Cerradura.Ninguna;
    public int mecanismosNecesarios = 1;                 // palancas, teclado o caja de fusibles que la destraban
    public string mensajeBloqueada = "Está trabada";
    public bool forzarEntidad;                           // al abrirla, el espectro aparece siempre
    public UnityEvent alAbrir;

    public bool Abierta { get; private set; }
    int mecanismosActivados;
    float avisoHasta;

    public override bool Disponible { get { return !Abierta; } }

    public override string Mensaje
    {
        get
        {
            if (Time.time >= avisoHasta) return "Presiona E";
            return cerradura == Cerradura.Llave ? "Está cerrada. Necesitas una llave" : mensajeBloqueada;
        }
    }

    public override void Interactuar(JugadorInteraccion jugador)
    {
        if (Abierta) return;

        if (cerradura == Cerradura.Llave && EstadoJuego.I.TieneLlave)
        {
            EstadoJuego.I.UsarLlave();
            cerradura = Cerradura.Ninguna;
        }
        if (cerradura != Cerradura.Ninguna)
        {
            avisoHasta = Time.time + 3f;
            if (sonido != null && sonidoCerrada != null) sonido.PlayOneShot(sonidoCerrada);
            return;
        }

        Abierta = true;
        if (areaDeteccion != null) areaDeteccion.enabled = false;
        StartCoroutine(Rotar());
    }

    /// <summary>Lo llaman la palanca, el teclado y la caja de fusibles. Con todos activados, la puerta queda libre.</summary>
    public void ActivarMecanismo()
    {
        mecanismosActivados++;
        if (cerradura != Cerradura.Mecanismo) return;
        if (mecanismosActivados >= mecanismosNecesarios)
        {
            cerradura = Cerradura.Ninguna;
            avisoHasta = 0f;
            if (sonido != null && sonidoDesbloqueo != null) sonido.PlayOneShot(sonidoDesbloqueo);
            HudJuego.I.MostrarPista("La puerta " + numero + " se destrabó", 4f);
        }
        else HudJuego.I.MostrarPista("Algo se movió, pero la puerta " + numero + " sigue trabada", 4f);
    }

    /// <summary>Deja la puerta abierta sin animación ni conteo: se usa al reaparecer en un punto de control.</summary>
    public void AbrirAlInstante()
    {
        if (Abierta) return;
        Abierta = true;
        cerradura = Cerradura.Ninguna;
        if (areaDeteccion != null) areaDeteccion.enabled = false;
        hoja.localRotation = hoja.localRotation * Quaternion.Euler(0f, anguloApertura, 0f);
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
