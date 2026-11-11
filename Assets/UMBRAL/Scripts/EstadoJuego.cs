using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>Datos que se conservan al pasar de un piso al siguiente.</summary>
public static class Progreso
{
    public static int puertasAlEntrar;
}

/// <summary>Estado de la partida en el piso actual: llave, contador de puertas, paso al siguiente piso, victoria y derrota.</summary>
[DefaultExecutionOrder(-100)]
public class EstadoJuego : MonoBehaviour
{
    public static EstadoJuego I { get; private set; }

    public JugadorController jugador;
    public AudioSource susto;
    public int nivel = 1;
    public int totalNiveles = 10;

    public bool TieneLlave { get; private set; }
    public int PuertasAbiertas { get; private set; }
    public bool Terminado { get; private set; }

    public event Action<PuertaInteractiva> AlAbrirPuerta;
    public event Action AlTomarLlave;

    bool esperaR;       // la pantalla final ya está visible y se puede presionar R
    bool juegoCompleto;

    void Awake()
    {
        I = this;
        Time.timeScale = 1f;
        if (nivel <= 1) Progreso.puertasAlEntrar = 0;
        PuertasAbiertas = Progreso.puertasAlEntrar;
    }

    void Start()
    {
        HudJuego.I.FijarPuertas(PuertasAbiertas);
        HudJuego.I.FijarPiso(nivel, totalNiveles);
        HudJuego.I.AbrirTelon("PISO " + nivel);
        if (nivel == 1) HudJuego.I.MostrarPista("WASD: moverte      Shift: correr      E: interactuar", 8f);
        RepartirLlave();
    }

    /// <summary>La llave aparece en un cajón al azar cada vez que se carga el piso.</summary>
    void RepartirLlave()
    {
        var mesas = FindObjectsByType<MesaDeNoche>();
        if (mesas.Length > 0) mesas[UnityEngine.Random.Range(0, mesas.Length)].GuardarLlave();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (!esperaR || kb == null || !kb.rKey.wasPressedThisFrame) return;
        if (juegoCompleto) { Progreso.puertasAlEntrar = 0; SceneManager.LoadScene(0); }
        else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);   // reintenta el mismo piso
    }

    public void DarLlave()
    {
        TieneLlave = true;
        HudJuego.I.FijarLlave(true);
        HudJuego.I.MostrarPista("Tienes la llave", 4f);
        AlTomarLlave?.Invoke();
    }

    public void UsarLlave()
    {
        TieneLlave = false;
        HudJuego.I.FijarLlave(false);
    }

    public void RegistrarPuerta(PuertaInteractiva puerta)
    {
        PuertasAbiertas++;
        HudJuego.I.FijarPuertas(PuertasAbiertas);
        AlAbrirPuerta?.Invoke(puerta);
    }

    /// <summary>El jugador cruzó la salida: pasa al siguiente piso o termina el juego en el último.</summary>
    public void Ganar()
    {
        if (Terminado) return;
        Terminado = true;
        if (jugador != null) jugador.Bloqueado = true;

        int siguiente = SceneManager.GetActiveScene().buildIndex + 1;
        if (nivel < totalNiveles && siguiente < SceneManager.sceneCountInBuildSettings)
        {
            Progreso.puertasAlEntrar = PuertasAbiertas;
            StartCoroutine(PasarDePiso(siguiente));
        }
        else
        {
            juegoCompleto = true;
            HudJuego.I.MostrarFinal("ESCAPASTE DEL HOTEL", "Pisos superados: " + nivel + "\nPuertas abiertas: " + PuertasAbiertas + "\nPresiona R para volver al piso 1");
            esperaR = true;
        }
    }

    IEnumerator PasarDePiso(int indice)
    {
        HudJuego.I.CerrarTelon("PISO " + (nivel + 1));
        yield return new WaitForSecondsRealtime(1.3f);
        SceneManager.LoadScene(indice);
    }

    /// <summary>El espectro alcanzó al jugador: aparece frente a la cámara un instante y después sale la pantalla de derrota.</summary>
    public void Perder(EntidadVisual visual, Transform entidad)
    {
        if (Terminado) return;
        Terminado = true;
        if (jugador != null) jugador.Bloqueado = true;
        StartCoroutine(Susto(visual, entidad));
    }

    IEnumerator Susto(EntidadVisual visual, Transform entidad)
    {
        if (susto != null) susto.Play();
        if (entidad != null && jugador != null)
        {
            Transform cam = jugador.camara;
            Vector3 frente = jugador.transform.forward;
            cam.localEulerAngles = new Vector3(-6f, 0f, 0f);
            const float alturaCara = 0.86f;   // de la raíz del espectro a su máscara
            var luzJugador = cam.GetComponentInChildren<Light>();
            if (luzJugador != null) luzJugador.enabled = false;   // la cara queda iluminada solo desde abajo
            entidad.gameObject.SetActive(true);
            entidad.rotation = Quaternion.LookRotation(-frente);
            if (visual != null) visual.atacando = true;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / 1.3f;
                float d = Mathf.Lerp(2.4f, 1.42f, Mathf.SmoothStep(0f, 1f, t * 3.5f));   // se abalanza; el modelo va inclinado, así que la cara queda a medio metro de la cámara
                entidad.position = cam.position + frente * d + Vector3.down * alturaCara;
                yield return null;
            }
            entidad.gameObject.SetActive(false);
        }
        HudJuego.I.MostrarFinal("TE ATRAPÓ", "Piso " + nivel + ". Cuando las luces parpadeen, escóndete en un clóset.\nPresiona R para reintentar");
        esperaR = true;
    }
}
