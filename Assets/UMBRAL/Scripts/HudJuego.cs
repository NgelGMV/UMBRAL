using UnityEngine;
using UnityEngine.UI;

/// <summary>Interfaz en pantalla. Se construye por código: aviso de interacción, contador de puertas, llave y pantalla final.</summary>
[DefaultExecutionOrder(-90)]
public class HudJuego : MonoBehaviour
{
    public static HudJuego I { get; private set; }

    Text aviso, puertas, llave, titulo, subtitulo, pista, piso, textoTelon;
    Image telon;
    float pistaHasta;
    GameObject panelFinal;
    string avisoActual = null;

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
        Ajustar(aviso.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 190), new Vector2(1400, 70));

        puertas = NuevoTexto("Puertas", 34, TextAnchor.UpperLeft, transform);
        Ajustar(puertas.rectTransform, new Vector2(0f, 1f), new Vector2(340, -60), new Vector2(600, 50));

        llave = NuevoTexto("Llave", 34, TextAnchor.UpperLeft, transform);
        llave.color = new Color(0.95f, 0.78f, 0.35f);
        Ajustar(llave.rectTransform, new Vector2(0f, 1f), new Vector2(340, -108), new Vector2(600, 50));

        pista = NuevoTexto("Pista", 32, TextAnchor.MiddleCenter, transform);
        pista.color = new Color(1f, 1f, 1f, 0f);
        Ajustar(pista.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(1600, 60));

        piso = NuevoTexto("Piso", 34, TextAnchor.UpperRight, transform);
        Ajustar(piso.rectTransform, new Vector2(1f, 1f), new Vector2(-340, -60), new Vector2(600, 50));

        // Telón negro para la entrada y la salida de cada piso
        telon = NuevaImagen("Telon", new Color(0f, 0f, 0f, 0f), transform);
        telon.rectTransform.anchorMin = Vector2.zero;
        telon.rectTransform.anchorMax = Vector2.one;
        telon.rectTransform.offsetMin = Vector2.zero;
        telon.rectTransform.offsetMax = Vector2.zero;
        textoTelon = NuevoTexto("TextoTelon", 110, TextAnchor.MiddleCenter, telon.transform);
        Ajustar(textoTelon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1700, 160));
        textoTelon.color = new Color(1f, 1f, 1f, 0f);

        var fondo = NuevaImagen("PanelFinal", new Color(0f, 0f, 0f, 0.86f), transform);
        fondo.rectTransform.anchorMin = Vector2.zero;
        fondo.rectTransform.anchorMax = Vector2.one;
        fondo.rectTransform.offsetMin = Vector2.zero;
        fondo.rectTransform.offsetMax = Vector2.zero;
        panelFinal = fondo.gameObject;
        titulo = NuevoTexto("Titulo", 96, TextAnchor.MiddleCenter, fondo.transform);
        Ajustar(titulo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(1700, 140));
        subtitulo = NuevoTexto("Subtitulo", 40, TextAnchor.UpperCenter, fondo.transform);
        Ajustar(subtitulo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(1700, 220));
        panelFinal.SetActive(false);

        FijarPuertas(0);
        FijarLlave(false);
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

    public void FijarPiso(int n, int total) { piso.text = "Piso " + n + " / " + total; }

    /// <summary>Empieza en negro con el nombre del piso y se aclara.</summary>
    public void AbrirTelon(string texto) { textoTelon.text = texto; StartCoroutine(Telon(1f, 0f, 0.7f, 1.6f)); }

    /// <summary>Oscurece la pantalla y muestra el nombre del piso siguiente.</summary>
    public void CerrarTelon(string texto) { textoTelon.text = texto; StartCoroutine(Telon(0f, 1f, 0f, 0.8f)); }

    System.Collections.IEnumerator Telon(float desde, float hasta, float espera, float duracion)
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

    public void FijarPuertas(int n) { puertas.text = "Puertas: " + n; }
    public void FijarLlave(bool tiene) { llave.text = tiene ? "Llave" : ""; }

    public void MostrarFinal(string t, string s)
    {
        titulo.text = t;
        subtitulo.text = s;
        panelFinal.SetActive(true);
    }

    static Text NuevoTexto(string nombre, int tam, TextAnchor ancla, Transform padre)
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

    static Image NuevaImagen(string nombre, Color color, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static void Ajustar(RectTransform r, Vector2 ancla, Vector2 pos, Vector2 tam)
    {
        r.anchorMin = ancla;
        r.anchorMax = ancla;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = tam;
    }
}
