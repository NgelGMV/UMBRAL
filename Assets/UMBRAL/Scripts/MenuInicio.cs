using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Pantalla de inicio: título, botones de jugar y salir, controles y créditos sobre un pasillo del hotel
/// con el espectro al fondo. Se construye por código. Enter también empieza la partida y Esc cierra el juego.
/// </summary>
public class MenuInicio : MonoBehaviour
{
    public const string ESCENA_JUEGO = "Hotel";

    public Transform camara;
    public LamparaPared[] lamparas;

    Image telon;
    Vector3 posCamara;
    float proximoParpadeo;
    bool saliendo;

    void Awake()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (camara != null) posCamara = camara.position;

        // Los botones necesitan un EventSystem con el módulo del Input System nuevo
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var escala = gameObject.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920, 1080);
        escala.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        // Franja oscura a la izquierda para que el texto se lea sobre el pasillo
        var franja = HudJuego.NuevaImagen("Franja", new Color(0f, 0f, 0f, 0.62f), transform);
        franja.rectTransform.anchorMin = new Vector2(0f, 0f);
        franja.rectTransform.anchorMax = new Vector2(0f, 1f);
        franja.rectTransform.pivot = new Vector2(0f, 0.5f);
        franja.rectTransform.anchoredPosition = Vector2.zero;
        franja.rectTransform.sizeDelta = new Vector2(820f, 0f);

        var titulo = HudJuego.NuevoTexto("Titulo", 170, TextAnchor.MiddleLeft, transform);
        titulo.text = "UMBRAL";
        titulo.color = new Color(0.95f, 0.82f, 0.45f);
        Izquierda(titulo.rectTransform, 300f, new Vector2(700, 200));
        var sub = HudJuego.NuevoTexto("Subtitulo", 36, TextAnchor.MiddleLeft, transform);
        sub.text = "Diez puertas te separan de la salida.\nNo estás solo en el hotel.";
        sub.lineSpacing = 1.2f;
        Izquierda(sub.rectTransform, 150f, new Vector2(700, 110));

        Boton("JUGAR", 0f, Jugar);
        Boton("SALIR", -110f, Salir);

        var ctrl = HudJuego.NuevoTexto("Controles", 28, TextAnchor.UpperLeft, transform);
        ctrl.text = "WASD: moverte        Shift: correr        Ratón: mirar\nE: interactuar        Esc: pausa\n\nCuando las luces parpadeen, escóndete en un clóset.";
        ctrl.lineSpacing = 1.25f;
        ctrl.color = new Color(1f, 1f, 1f, 0.85f);
        Izquierda(ctrl.rectTransform, -300f, new Vector2(700, 190));

        var cred = HudJuego.NuevoTexto("Creditos", 24, TextAnchor.LowerLeft, transform);
        cred.text = "Angel Márquez y Eriol Tuñón\nAnimación y Producción Audiovisual, FISC-UTP";
        cred.color = new Color(1f, 1f, 1f, 0.6f);
        Izquierda(cred.rectTransform, -470f, new Vector2(700, 70));

        telon = HudJuego.Panel("Telon", Color.black, transform);
        StartCoroutine(Fundido(1f, 0f, 1.5f, null));
    }

    static void Izquierda(RectTransform r, float y, Vector2 tam)
    {
        r.anchorMin = r.anchorMax = new Vector2(0f, 0.5f);
        r.pivot = new Vector2(0f, 0.5f);
        r.anchoredPosition = new Vector2(110f, y);
        r.sizeDelta = tam;
    }

    void Boton(string texto, float y, UnityEngine.Events.UnityAction accion)
    {
        var fondo = HudJuego.NuevaImagen("Boton" + texto, new Color(0.95f, 0.82f, 0.45f, 0.55f), transform);
        fondo.raycastTarget = true;
        Izquierda(fondo.rectTransform, y, new Vector2(420, 86));
        var b = fondo.gameObject.AddComponent<Button>();
        b.targetGraphic = fondo;
        var colores = b.colors;
        colores.normalColor = new Color(1f, 1f, 1f, 0.3f);
        colores.highlightedColor = new Color(1f, 1f, 1f, 0.75f);
        colores.pressedColor = Color.white;
        colores.selectedColor = new Color(1f, 1f, 1f, 0.3f);
        colores.colorMultiplier = 1f;
        b.colors = colores;
        b.onClick.AddListener(accion);
        var t = HudJuego.NuevoTexto("Texto", 44, TextAnchor.MiddleLeft, fondo.transform);
        t.text = texto;
        t.rectTransform.anchorMin = Vector2.zero;
        t.rectTransform.anchorMax = Vector2.one;
        t.rectTransform.offsetMin = new Vector2(34f, 0f);
        t.rectTransform.offsetMax = Vector2.zero;
    }

    void Update()
    {
        // La cámara se mece apenas, como alguien que respira parado en el pasillo
        if (camara != null)
        {
            float t = Time.unscaledTime;
            camara.position = posCamara + new Vector3(Mathf.Sin(t * 0.4f) * 0.05f, Mathf.Sin(t * 0.9f) * 0.025f, Mathf.Sin(t * 0.25f) * 0.12f);
        }
        if (lamparas != null && Time.time >= proximoParpadeo)
        {
            proximoParpadeo = Time.time + Random.Range(3f, 7f);
            if (lamparas.Length > 0) lamparas[Random.Range(0, lamparas.Length)].Parpadear(Random.Range(0.5f, 1.4f));
        }

        var kb = Keyboard.current;
        if (kb == null || saliendo) return;
        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) Jugar();
        else if (kb.escapeKey.wasPressedThisFrame) Salir();
    }

    void Jugar()
    {
        if (saliendo) return;
        saliendo = true;
        Progreso.Reiniciar();
        StartCoroutine(Fundido(0f, 1f, 0.8f, () => SceneManager.LoadScene(ESCENA_JUEGO)));
    }

    void Salir()
    {
        if (saliendo) return;
        saliendo = true;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator Fundido(float desde, float hasta, float duracion, System.Action alTerminar)
    {
        telon.raycastTarget = false;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duracion;
            telon.color = new Color(0f, 0f, 0f, Mathf.Lerp(desde, hasta, t));
            yield return null;
        }
        if (alTerminar != null) alTerminar();
    }
}
