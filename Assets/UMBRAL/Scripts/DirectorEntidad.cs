using System.Collections;
using UnityEngine;

/// <summary>
/// Controla al espectro en el mapa de 10 habitaciones.
/// Aviso: las lámparas parpadean y zumban unos segundos. Después el espectro recorre las habitaciones ya abiertas,
/// de puerta en puerta, hasta el fondo de la última. Atrapa al jugador en cuanto entra a la habitación donde está,
/// a menos que esté escondido en un clóset. La habitación 1 (recepción) es zona segura.
/// Aparece al tomar una llave, al abrir puertas (según una probabilidad) y cada cierto tiempo. Con cada puerta abierta
/// avisa menos, va más rápido y aparece más seguido.
/// </summary>
public class DirectorEntidad : MonoBehaviour
{
    [Header("Referencias")]
    public JugadorController jugador;
    public Transform camaraJugador;
    public GameObject entidad;
    public EntidadVisual visual;
    public PuertaInteractiva[] puertas;   // en orden: 001 a 010
    public AudioSource rugido;
    public AudioSource zumbido;           // luces fallando durante el aviso

    [Header("Recorrido")]
    public Vector3[] ruta;                // puntos de paso: centro de cada habitación y cada puerta
    public int[] entradaDeCuarto;         // índice de la ruta donde empieza cada habitación, de la 2 a la 10
    public int[] finDeCuarto;             // índice de la ruta donde termina cada habitación, de la 2 a la 10
    public int cuartosAtras = 2;          // el espectro sale esta cantidad de habitaciones detrás del jugador

    [Header("Ajustes: valor al inicio y con todas las puertas abiertas")]
    public float alcance = 9f;            // dentro de la misma habitación, distancia a la que atrapa
    public Vector2 segundosDeAviso = new Vector2(4.0f, 2.6f);
    public Vector2 velocidad = new Vector2(13f, 17f);
    public Vector2 probabilidadPorPuerta = new Vector2(0.30f, 0.60f);
    public Vector2 intervaloMin = new Vector2(45f, 20f);
    public Vector2 intervaloMax = new Vector2(70f, 32f);
    public Vector2 probabilidadDoble = new Vector2(0f, 0.45f);   // vuelve a pasar en sentido contrario

    public bool EnCurso { get; private set; }
    float proxima;

    // habitación según la fila (de sur a norte) y la columna (oeste, este): el recorrido va en zigzag
    static readonly int[,] CUARTOS = { { 1, 2 }, { 4, 3 }, { 5, 6 }, { 8, 7 }, { 9, 10 } };

    /// <summary>Número de habitación (1 a 10) que corresponde a un punto del mapa.</summary>
    public static int CuartoEn(Vector3 p)
    {
        int fila = Mathf.Clamp(Mathf.FloorToInt((p.z - 0.5f) / 8f), 0, 4);
        return CUARTOS[fila, p.x < 10.5f ? 0 : 1];
    }

    /// <summary>Última habitación a la que se puede llegar con las puertas abiertas hasta ahora.</summary>
    public int UltimoCuartoAbierto()
    {
        int m = 1;
        for (int i = 0; i < puertas.Length - 1 && puertas[i].Abierta; i++) m = i + 2;
        return m;
    }

    float Avance { get { return Mathf.Clamp01(EstadoJuego.I.PuertasAbiertas / 9f); } }
    static float Entre(Vector2 v, float t) { return Mathf.Lerp(v.x, v.y, t); }

    void Start()
    {
        if (entidad != null) entidad.SetActive(false);
        EstadoJuego.I.AlTomarLlave += Lanzar;
        EstadoJuego.I.AlAbrirPuerta += AlAbrirPuerta;
        Programar();
    }

    void Programar()
    {
        proxima = Time.time + Random.Range(Entre(intervaloMin, Avance), Entre(intervaloMax, Avance));
    }

    void Update()
    {
        if (EnCurso || EstadoJuego.I.Terminado || Time.time < proxima) return;
        if (CuartoEn(camaraJugador.position) >= 2) Lanzar();
        else proxima = Time.time + 5f;   // el jugador sigue en la recepción: se vuelve a intentar en un momento
    }

    void AlAbrirPuerta(PuertaInteractiva p)
    {
        if (p == puertas[0] || p == puertas[puertas.Length - 1]) return;
        if (p.forzarEntidad || Random.value < Entre(probabilidadPorPuerta, Avance)) Lanzar();
    }

    public void Lanzar()
    {
        if (!EnCurso && !EstadoJuego.I.Terminado && UltimoCuartoAbierto() >= 2) StartCoroutine(Secuencia());
    }

    IEnumerator Secuencia()
    {
        EnCurso = true;
        float aviso = Entre(segundosDeAviso, Avance);
        foreach (var l in LamparaPared.Todas) l.Parpadear(aviso);
        if (rugido != null) { rugido.volume = 0f; rugido.Play(); }
        if (zumbido != null) zumbido.Play();

        float t = 0f;
        while (t < aviso)
        {
            t += Time.deltaTime;
            if (rugido != null) rugido.volume = Mathf.Lerp(0f, 0.9f, t / aviso);
            yield return null;
        }
        if (zumbido != null) zumbido.Stop();

        // Sale unas habitaciones detrás del jugador y llega hasta el fondo de la última habitación abierta
        int ultimo = UltimoCuartoAbierto();
        int cuartoJugador = Mathf.Clamp(CuartoEn(camaraJugador.position), 2, ultimo);
        int desde = entradaDeCuarto[Mathf.Max(2, cuartoJugador - cuartosAtras) - 2];
        int hasta = finDeCuarto[ultimo - 2];
        float v = Entre(velocidad, Avance);
        int pasadas = Random.value < Entre(probabilidadDoble, Avance) ? 2 : 1;

        Transform e = entidad.transform;
        bool atrapado = false;
        for (int p = 0; p < pasadas && !atrapado && !EstadoJuego.I.Terminado; p++)
        {
            int paso = hasta >= desde ? 1 : -1;
            e.position = ruta[desde];
            if (hasta != desde) e.rotation = Quaternion.LookRotation(ruta[desde + paso] - ruta[desde]);
            entidad.SetActive(true);
            for (int i = desde; i != hasta && !atrapado && !EstadoJuego.I.Terminado; i += paso)
            {
                Vector3 destino = ruta[i + paso];
                while ((e.position - destino).sqrMagnitude > 0.01f && !EstadoJuego.I.Terminado)
                {
                    e.position = Vector3.MoveTowards(e.position, destino, v * Time.deltaTime);
                    Vector3 dir = destino - e.position;
                    if (dir.sqrMagnitude > 0.01f) e.rotation = Quaternion.Slerp(e.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 12f);
                    foreach (var l in LamparaPared.Todas)
                        if ((l.transform.position - e.position).sqrMagnitude < 49f) l.ApagarUnRato(2.5f);
                    if (Atrapa(e.position)) { atrapado = true; break; }
                    yield return null;
                }
            }
            if (atrapado) break;
            entidad.SetActive(false);
            int tmp = desde; desde = hasta; hasta = tmp;
            if (p + 1 < pasadas) yield return new WaitForSeconds(1.4f);   // silencio breve antes de volver
        }

        if (rugido != null) rugido.Stop();
        EnCurso = false;
        if (atrapado) EstadoJuego.I.Perder(visual, e);   // el espectro queda activo para el susto final
        else { entidad.SetActive(false); Programar(); }
    }

    /// <summary>Atrapa si el espectro está en la misma habitación que el jugador y este no está escondido.</summary>
    bool Atrapa(Vector3 desde)
    {
        if (jugador.Oculto) return false;
        Vector3 ojos = camaraJugador.position;
        int cuarto = CuartoEn(ojos);
        if (cuarto < 2 || CuartoEn(desde) != cuarto) return false;
        return (ojos - desde).magnitude < alcance;
    }
}
