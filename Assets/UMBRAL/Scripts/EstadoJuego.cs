using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>Datos de la partida que se conservan al recargar la escena (por ejemplo, al reintentar tras ser atrapado).</summary>
public static class Progreso
{
    public static int puntoDeControl;   // puertas que ya estaban abiertas: el jugador reaparece en la habitación siguiente
    public static float tiempo;
    public static int vecesAtrapado;

    public static void Reiniciar() { puntoDeControl = 0; tiempo = 0f; vecesAtrapado = 0; }
}

/// <summary>Estado de la partida: llave, fusibles, contador de puertas, punto de control, pausa, victoria y derrota.</summary>
[DefaultExecutionOrder(-100)]
public class EstadoJuego : MonoBehaviour
{
    public const string ESCENA_MENU = "Menu";

    public static EstadoJuego I { get; private set; }

    public JugadorController jugador;
    public PuertaInteractiva[] puertas;     // en orden: 001 a 010
    public Vector3[] reaparicion;           // punto de entrada de cada habitación, de la 1 a la 10
    public float[] giroReaparicion;
    public AudioSource susto;
    public AudioSource efectos;
    public AudioClip clipFusible;

    public bool TieneLlave { get; private set; }
    public int Fusibles { get; private set; }
    public int PuertasAbiertas { get; private set; }
    public bool Terminado { get; private set; }
    public bool Pausado { get; private set; }

    public event Action<PuertaInteractiva> AlAbrirPuerta;
    public event Action AlTomarLlave;

    enum Final { Ninguno, Derrota, Victoria }
    Final final = Final.Ninguno;   // pantalla final visible: ya se puede elegir con el teclado
    bool bloqueadoAntesDePausa;
    int cuartoMostrado;

    void Awake()
    {
        I = this;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    void Start()
    {
        // Punto de control: las puertas ya superadas aparecen abiertas y el jugador entra por la última
        int k = Mathf.Clamp(Progreso.puntoDeControl, 0, puertas.Length - 1);
        for (int i = 0; i < k; i++) puertas[i].AbrirAlInstante();
        PuertasAbiertas = k;
        if (k > 0) jugador.Teletransportar(reaparicion[k], giroReaparicion[k]);

        HudJuego.I.FijarPuertas(PuertasAbiertas, puertas.Length);
        HudJuego.I.FijarInventario(false, 0);
        HudJuego.I.AbrirTelon(k == 0 ? "UMBRAL" : "HABITACIÓN " + (k + 1));
        if (k == 0) HudJuego.I.MostrarPista("WASD: moverte      Shift: correr      E: interactuar      Esc: pausa", 9f);
        RepartirLlaves();
    }

    /// <summary>Cada puerta con llave tiene su grupo de cajones: la llave aparece en uno de ellos al azar.</summary>
    void RepartirLlaves()
    {
        var zonas = new Dictionary<int, List<MesaDeNoche>>();
        foreach (var m in FindObjectsByType<MesaDeNoche>())
        {
            if (m.zona <= 0 || m.zona > puertas.Length || puertas[m.zona - 1].Abierta) continue;
            if (!zonas.ContainsKey(m.zona)) zonas[m.zona] = new List<MesaDeNoche>();
            zonas[m.zona].Add(m);
        }
        foreach (var lista in zonas.Values) lista[UnityEngine.Random.Range(0, lista.Count)].GuardarLlave();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (final == Final.Derrota)
        {
            if (kb.rKey.wasPressedThisFrame) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);   // reaparece en el punto de control
            else if (kb.mKey.wasPressedThisFrame) IrAlMenu();
            return;
        }
        if (final == Final.Victoria)
        {
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) { Progreso.Reiniciar(); SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
            else if (kb.mKey.wasPressedThisFrame) IrAlMenu();
            return;
        }
        if (Terminado) return;

        if (kb.escapeKey.wasPressedThisFrame)
        {
            if (TecladoCodigo.Activo != null) TecladoCodigo.Activo.Soltar();
            else Pausar(!Pausado);
        }
        else if (Pausado && kb.mKey.wasPressedThisFrame) IrAlMenu();

        if (Pausado) return;
        Progreso.tiempo += Time.deltaTime;
        int cuarto = DirectorEntidad.CuartoEn(jugador.transform.position);
        if (cuarto != cuartoMostrado) { cuartoMostrado = cuarto; HudJuego.I.FijarCuarto(cuarto, puertas.Length); }
    }

    void Pausar(bool pausar)
    {
        Pausado = pausar;
        Time.timeScale = pausar ? 0f : 1f;
        AudioListener.pause = pausar;
        if (pausar) { bloqueadoAntesDePausa = jugador.Bloqueado; jugador.Bloqueado = true; }
        else jugador.Bloqueado = bloqueadoAntesDePausa;
        HudJuego.I.MostrarPausa(pausar);
    }

    void IrAlMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Progreso.Reiniciar();
        SceneManager.LoadScene(ESCENA_MENU);
    }

    public void DarLlave()
    {
        TieneLlave = true;
        HudJuego.I.FijarInventario(TieneLlave, Fusibles);
        HudJuego.I.MostrarPista("Tienes la llave", 4f);
        AlTomarLlave?.Invoke();
    }

    public void UsarLlave()
    {
        TieneLlave = false;
        HudJuego.I.FijarInventario(TieneLlave, Fusibles);
    }

    public void TomarFusible()
    {
        Fusibles++;
        HudJuego.I.FijarInventario(TieneLlave, Fusibles);
        if (efectos != null && clipFusible != null) efectos.PlayOneShot(clipFusible);
    }

    public void UsarFusible()
    {
        Fusibles = Mathf.Max(0, Fusibles - 1);
        HudJuego.I.FijarInventario(TieneLlave, Fusibles);
    }

    public void RegistrarPuerta(PuertaInteractiva puerta)
    {
        PuertasAbiertas++;
        Progreso.puntoDeControl = Mathf.Min(PuertasAbiertas, puertas.Length - 1);
        HudJuego.I.FijarPuertas(PuertasAbiertas, puertas.Length);
        AlAbrirPuerta?.Invoke(puerta);
    }

    /// <summary>El jugador cruzó la puerta 010: pantalla de victoria.</summary>
    public void Ganar()
    {
        if (Terminado) return;
        Terminado = true;
        if (jugador != null) jugador.Bloqueado = true;
        StartCoroutine(Victoria());
    }

    IEnumerator Victoria()
    {
        HudJuego.I.CerrarTelon("");
        yield return new WaitForSecondsRealtime(1.2f);
        HudJuego.I.MostrarVictoria(Progreso.tiempo, PuertasAbiertas, puertas.Length, Progreso.vecesAtrapado);
        final = Final.Victoria;
    }

    /// <summary>El espectro alcanzó al jugador: aparece frente a la cámara un instante y después sale la pantalla de derrota.</summary>
    public void Perder(EntidadVisual visual, Transform entidad)
    {
        if (Terminado) return;
        Terminado = true;
        Progreso.vecesAtrapado++;
        if (TecladoCodigo.Activo != null) TecladoCodigo.Activo.Soltar();
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
            const float alturaCara = 0.86f;   // de la raíz del espectro a su cara
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
        int cuarto = Mathf.Min(Progreso.puntoDeControl + 1, puertas.Length);
        HudJuego.I.MostrarDerrota("TE ATRAPÓ", "Cuando las luces parpadeen, escóndete en un clóset.\n\nR: reintentar desde la habitación " + cuarto + "        M: menú principal");
        final = Final.Derrota;
    }
}
