using System.Collections;
using UnityEngine;

/// <summary>
/// Controla al espectro del pasillo.
/// Aviso: las lámparas parpadean y zumban unos segundos. Después el espectro cruza el pasillo.
/// Atrapa al jugador si lo tiene a la vista, a menos de "alcance" metros, y no está escondido en un clóset.
/// Aparece al tomar la llave, al abrir puertas (según una probabilidad) y cada cierto tiempo. La recepción es zona segura.
/// </summary>
public class DirectorEntidad : MonoBehaviour
{
    [Header("Referencias")]
    public JugadorController jugador;
    public Transform camaraJugador;
    public GameObject entidad;
    public EntidadVisual visual;
    public PuertaInteractiva puertaRecepcion;   // primera puerta: no dispara la entidad
    public PuertaInteractiva puertaSalaFinal;   // puerta con seguro: si está abierta, la entidad sigue hasta la sala
    public PuertaInteractiva puertaSalida;      // salida del piso
    public AudioSource rugido;
    public AudioSource zumbido;                 // luces fallando durante el aviso

    [Header("Recorrido")]
    public Vector3 inicio = new Vector3(10f, 1.5f, 11.3f);
    public Vector3 finPasillo = new Vector3(10f, 1.5f, 29.6f);
    public Vector3 finSala = new Vector3(10f, 1.5f, 38.6f);
    public float zonaSeguraZ = 10.25f;

    [Header("Ajustes")]
    public float segundosDeAviso = 4f;
    public float velocidad = 13f;
    public float alcance = 7f;   // distancia a la que atrapa: el jugador alcanza a verla venir
    [Range(0f, 1f)] public float probabilidadPorPuerta = 0.3f;

    [Header("Apariciones por tiempo (0 = desactivadas)")]
    public float intervaloMin = 60f;
    public float intervaloMax = 90f;
    [Range(0f, 1f)] public float probabilidadDoble = 0f;      // vuelve a pasar en sentido contrario
    [Range(0f, 1f)] public float probabilidadReversa = 0.3f;  // entra por el fondo del pasillo

    public bool EnCurso { get; private set; }
    float proxima;

    void Start()
    {
        if (entidad != null) entidad.SetActive(false);
        EstadoJuego.I.AlTomarLlave += Lanzar;
        EstadoJuego.I.AlAbrirPuerta += AlAbrirPuerta;
        Programar();
    }

    void Programar()
    {
        proxima = intervaloMax > 0f ? Time.time + Random.Range(intervaloMin, intervaloMax) : float.PositiveInfinity;
    }

    void Update()
    {
        if (EnCurso || EstadoJuego.I.Terminado || Time.time < proxima) return;
        if (camaraJugador.position.z > zonaSeguraZ) Lanzar();
        else proxima = Time.time + 5f;   // el jugador sigue en la recepción: se vuelve a intentar en un momento
    }

    void AlAbrirPuerta(PuertaInteractiva p)
    {
        if (p == puertaRecepcion || p == puertaSalida) return;
        if (Random.value < probabilidadPorPuerta) Lanzar();
    }

    public void Lanzar()
    {
        if (!EnCurso && !EstadoJuego.I.Terminado) StartCoroutine(Secuencia());
    }

    IEnumerator Secuencia()
    {
        EnCurso = true;
        foreach (var l in LamparaPared.Todas) l.Parpadear(segundosDeAviso);
        if (rugido != null) { rugido.volume = 0f; rugido.Play(); }
        if (zumbido != null) zumbido.Play();

        float t = 0f;
        while (t < segundosDeAviso)
        {
            t += Time.deltaTime;
            if (rugido != null) rugido.volume = Mathf.Lerp(0f, 0.9f, t / segundosDeAviso);
            yield return null;
        }
        if (zumbido != null) zumbido.Stop();

        Vector3 a = inicio;
        Vector3 b = (puertaSalaFinal != null && puertaSalaFinal.Abierta) ? finSala : finPasillo;
        if (Random.value < probabilidadReversa) { Vector3 tmp = a; a = b; b = tmp; }
        int pasadas = Random.value < probabilidadDoble ? 2 : 1;

        Transform e = entidad.transform;
        bool atrapado = false;
        for (int p = 0; p < pasadas && !atrapado && !EstadoJuego.I.Terminado; p++)
        {
            e.SetPositionAndRotation(a, Quaternion.LookRotation(b - a));
            entidad.SetActive(true);
            while ((e.position - b).sqrMagnitude > 0.01f && !EstadoJuego.I.Terminado)
            {
                e.position = Vector3.MoveTowards(e.position, b, velocidad * Time.deltaTime);
                foreach (var l in LamparaPared.Todas)
                    if ((l.transform.position - e.position).sqrMagnitude < 36f) l.ApagarUnRato(2.5f);
                if (Atrapa(e.position)) { atrapado = true; break; }
                yield return null;
            }
            if (atrapado) break;
            entidad.SetActive(false);
            Vector3 c = a; a = b; b = c;
            if (p + 1 < pasadas) yield return new WaitForSeconds(1.4f);   // silencio breve antes de volver
        }

        if (rugido != null) rugido.Stop();
        EnCurso = false;
        if (atrapado) EstadoJuego.I.Perder(visual, e);   // el espectro queda activo para el susto final
        else { entidad.SetActive(false); Programar(); }
    }

    bool Atrapa(Vector3 desde)
    {
        if (jugador.Oculto) return false;
        Vector3 ojos = camaraJugador.position;
        if (ojos.z < zonaSeguraZ) return false;
        if ((ojos - desde).magnitude > alcance) return false;
        RaycastHit golpe;
        if (Physics.Linecast(desde, ojos, out golpe, ~0, QueryTriggerInteraction.Ignore))
            return golpe.transform.IsChildOf(jugador.transform);
        return true;
    }
}
