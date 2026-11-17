using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interfaz en pantalla, construida por código: aviso de interacción, pistas, contador de puertas, habitación actual,
/// inventario, telón de entrada, pausa y las pantallas de derrota y de victoria.
/// </summary>
[DefaultExecutionOrder(-90)]
public class HudJuego : MonoBehaviour
{
    public static HudJuego I { get; private set; }

    Text aviso, pista, puertas, inventario, cuarto, textoTelon;
    Text tituloDerrota, textoDerrota, datosVictoria;
    Image telon;
    GameObject panelDerrota, panelVictoria, panelPausa;
    string avisoActual = null;
    float pistaHasta;

    void Awake()
    {
        I = this;
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var escala = gameObject.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920, 1080);
        escala.matchWidthOrHeight = 0.5f;

        var mira = NuevaImagen("Mira", new Color(1f, 1f, 1f, 0.55f), transform);
        Ajustar(mira.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6, 6));

        aviso = NuevoTexto("Aviso", 40, TextAnchor.MiddleCenter, transform);
        Ajustar(aviso.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 190), new Vector2(1700, 70));

        pista = NuevoTexto("Pista", 32, TextAnchor.MiddleCenter, transform);
        pista.color = new Color(1f, 1f, 1f, 0f);
        Ajustar(pista.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(1800, 60));

        puertas = NuevoTexto("Puertas", 34, TextAnchor.UpperLeft, transform);
        Ajustar(puertas.rectTransform, new Vector2(0f, 1f), new Vector2(340, -60), new Vector2(600, 50));

        inventario = NuevoTexto("Inventario", 34, TextAnchor.UpperLeft, transform);
        inventario.color = new Color(0.95f, 0.78f, 0.35f);
        Ajustar(inventario.rectTransform, new Vector2(0f, 1f), new Vector2(340, -108), new Vector2(600, 50));

        cuarto = NuevoTexto("Cuarto", 34, TextAnchor.UpperRight, transform);
        Ajustar(cuarto.rectTransform, new Vector2(1f, 1f), new Vector2(-340, -60), new Vector2(600, 50));

        // Pausa
        var pausa = Panel("PanelPausa", new Color(0f, 0f, 0f, 0.78f), transform);
        panelPausa = pausa.gameObject;
        Ajustar(NuevoTexto("Titulo", 96, TextAnchor.MiddleCenter, pausa.transform).rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(1700, 140));
        pausa.transform.Find("Titulo").GetComponent<Text>().text = "PAUSA";
        var op = NuevoTexto("Opciones", 40, TextAnchor.UpperCenter, pausa.transform);
        Ajustar(op.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -70), new Vector2(1700, 120));
        op.text = "Esc: continuar        M: menú principal";
        panelPausa.SetActive(false);

        // Telón negro para la entrada y la salida
        telon = Panel("Telon", new Color(0f, 0f, 0f, 0f), transform);
        textoTelon = NuevoTexto("TextoTelon", 110, TextAnchor.MiddleCenter, telon.transform);
        Ajustar(textoTelon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1700, 160));
        textoTelon.color = new Color(1f, 1f, 1f, 0f);

        // Derrota
        var derrota = Panel("PanelDerrota", new Color(0f, 0f, 0f, 0.86f), transform);
        panelDerrota = derrota.gameObject;
        tituloDerrota = NuevoTexto("Titulo", 96, TextAnchor.MiddleCenter, derrota.transform);
        tituloDerrota.color = new Color(0.85f, 0.12f, 0.1f);
        Ajustar(tituloDerrota.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 110), new Vector2(1700, 140));
        textoDerrota = NuevoTexto("Texto", 38, TextAnchor.UpperCenter, derrota.transform);
        Ajustar(textoDerrota.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(1700, 260));
        panelDerrota.SetActive(false);

        // Victoria
        var victoria = Panel("PanelVictoria", Color.black, transform);
        panelVictoria = victoria.gameObject;
        var tv = NuevoTexto("Titulo", 130, TextAnchor.MiddleCenter, victoria.transform);
        tv.text = "ESCAPASTE";
        tv.color = new Color(0.95f, 0.82f, 0.45f);
        Ajustar(tv.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 290), new Vector2(1700, 170));
        var sv = NuevoTexto("Subtitulo", 44, TextAnchor.MiddleCenter, victoria.transform);
        sv.text = "Cruzaste las diez puertas y saliste del hotel";
        Ajustar(sv.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(1700, 70));
        var linea = NuevaImagen("Linea", new Color(0.95f, 0.82f, 0.45f, 0.6f), victoria.transform);
        Ajustar(linea.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 110), new Vector2(700, 3));
        datosVictoria = NuevoTexto("Datos", 46, TextAnchor.UpperCenter, victoria.transform);
        datosVictoria.lineSpacing = 1.35f;
        Ajustar(datosVictoria.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(1700, 280));
        var ov = NuevoTexto("Opciones", 38, TextAnchor.MiddleCenter, victoria.transform);
        ov.text = "Enter: jugar de nuevo        M: menú principal";
        Ajustar(ov.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -300), new Vector2(1700, 60));
        var cv = NuevoTexto("Creditos", 28, TextAnchor.MiddleCenter, victoria.transform);
        cv.text = "UMBRAL    Angel Márquez y Eriol Tuñón    Animación y Producción Audiovisual, FISC-UTP";
        cv.color = new Color(1f, 1f, 1f, 0.6f);
        Ajustar(cv.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 70), new Vector2(1800, 50));
        panelVictoria.SetActive(false);

        FijarPuertas(0, 10);
        FijarInventario(false, 0);
        FijarCuarto(1, 10);
        MostrarAviso("");
    }

    void Update()
    {
        float resta = pistaHasta - Time.unscaledTime;
        var c = pista.color; c.a = Mathf.Clamp01(resta); pista.color = c;
    }

    /// <summary>Mensaje breve en la parte superior que se desvanece solo.</summary>
    public void MostrarPista(string texto, float segundos)
    {
        pista.text = texto;
        pistaHasta = Time.unscaledTime + segundos;
    }

    public void MostrarAviso(string texto)
    {
        if (texto == avisoActual) return;
        avisoActual = texto;
        aviso.text = texto;
    }

    public void FijarPuertas(int n, int total) { puertas.text = "Puertas: " + n + " / " + total; }
    public void FijarCuarto(int n, int total) { cuarto.text = "Habitación " + n + " / " + total; }

    public void FijarInventario(bool llave, int fusibles)
    {
        string s = llave ? "Llave" : "";
        if (fusibles > 0) s += (s.Length > 0 ? "    " : "") + "Fusibles: " + fusibles;
        inventario.text = s;
    }

    public void MostrarPausa(bool visible) { panelPausa.SetActive(visible); }

    /// <summary>Empieza en negro con un título y se aclara.</summary>
    public void AbrirTelon(string texto) { textoTelon.text = texto; StartCoroutine(Telon(1f, 0f, 0.9f, 1.6f)); }

    /// <summary>Oscurece la pantalla.</summary>
    public void CerrarTelon(string texto) { textoTelon.text = texto; StartCoroutine(Telon(0f, 1f, 0f, 0.9f)); }

    IEnumerator Telon(float desde, float hasta, float espera, float duracion)
    {
        FijarTelon(desde);
        float t = 0f;
        while (t < espera) { t += Time.unscaledDeltaTime; yield return null; }
        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duracion;
            FijarTelon(Mathf.Lerp(desde, hasta, t));
            yield return null;
        }
        FijarTelon(hasta);
    }

    void FijarTelon(float a)
    {
        telon.color = new Color(0f, 0f, 0f, a);
        textoTelon.color = new Color(1f, 1f, 1f, a);
    }

    public void MostrarDerrota(string titulo, string texto)
    {
        tituloDerrota.text = titulo;
        textoDerrota.text = texto;
        panelDerrota.SetActive(true);
    }

    public void MostrarVictoria(float segundos, int puertasAbiertas, int total, int vecesAtrapado)
    {
        int s = Mathf.FloorToInt(segundos);
        datosVictoria.text = "Tiempo: " + (s / 60).ToString("00") + ":" + (s % 60).ToString("00")
            + "\nPuertas abiertas: " + puertasAbiertas + " de " + total
            + "\nVeces que te atrapó el espectro: " + vecesAtrapado;
        panelVictoria.SetActive(true);
    }

    // ------------------------------------------------------------------ utilidades de interfaz (las usa también el menú)
    public static Text NuevoTexto(string nombre, int tam, TextAnchor ancla, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        var t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = tam;
        t.alignment = ancla;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var s = go.AddComponent<Shadow>();
        s.effectColor = new Color(0f, 0f, 0f, 0.85f);
        s.effectDistance = new Vector2(2f, -2f);
        return t;
    }

    public static Image NuevaImagen(string nombre, Color color, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>Imagen que cubre toda la pantalla.</summary>
    public static Image Panel(string nombre, Color color, Transform padre)
    {
        var img = NuevaImagen(nombre, color, padre);
        img.rectTransform.anchorMin = Vector2.zero;
        img.rectTransform.anchorMax = Vector2.one;
        img.rectTransform.offsetMin = Vector2.zero;
        img.rectTransform.offsetMax = Vector2.zero;
        return img;
    }

    public static void Ajustar(RectTransform r, Vector2 ancla, Vector2 pos, Vector2 tam)
    {
        r.anchorMin = ancla;
        r.anchorMax = ancla;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = tam;
    }
}
