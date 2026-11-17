using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Teclado numérico junto a una puerta. El código de 3 dígitos se sortea al cargar la escena y queda escrito en las notas.
/// Con E se empieza a escribir; los números van con el teclado, Retroceso borra y E o Esc sueltan el teclado.
/// </summary>
public class TecladoCodigo : Interactuable
{
    public PuertaInteractiva puerta;
    public NotaCodigo[] notas;
    public AudioSource sonido;
    public AudioClip clipTecla;
    public AudioClip clipError;
    public Light indicador;   // rojo; pasa a verde con el código correcto

    /// <summary>Teclado que el jugador está usando en este momento (o null).</summary>
    public static TecladoCodigo Activo { get; private set; }

    string codigo;
    string entrada = "";
    bool resuelto;
    float errorHasta;
    JugadorInteraccion jugador;

    public override bool Disponible { get { return !resuelto; } }

    public override string Mensaje
    {
        get
        {
            if (Activo != this) return "Presiona E para usar el teclado";
            if (Time.time < errorHasta) return "Código incorrecto";
            string v = "";
            for (int i = 0; i < 3; i++) v += (i < entrada.Length ? entrada[i].ToString() : "_") + " ";
            return "Código: " + v + "    (escribe los números, E para soltar)";
        }
    }

    void Start()
    {
        codigo = Random.Range(0, 1000).ToString("000");
        foreach (var n in notas) if (n != null) n.Fijar(codigo);
    }

    void OnDisable() { if (Activo == this) Activo = null; }

    public override void Interactuar(JugadorInteraccion quien)
    {
        if (resuelto) return;
        if (Activo == this) { Soltar(); return; }
        Activo = this;
        jugador = quien;
        jugador.fijado = this;
        jugador.controlador.Bloqueado = true;
        entrada = "";
    }

    /// <summary>Deja de escribir y devuelve el control al jugador.</summary>
    public void Soltar()
    {
        if (Activo != this) return;
        Activo = null;
        if (jugador != null)
        {
            jugador.fijado = null;
            if (!EstadoJuego.I.Terminado) jugador.controlador.Bloqueado = false;
        }
    }

    void Update()
    {
        if (Activo != this || EstadoJuego.I.Pausado) return;
        if (EstadoJuego.I.Terminado) { Soltar(); return; }
        var kb = Keyboard.current;
        if (kb == null || Time.time < errorHasta) return;

        KeyControl[] fila = { kb.digit0Key, kb.digit1Key, kb.digit2Key, kb.digit3Key, kb.digit4Key, kb.digit5Key, kb.digit6Key, kb.digit7Key, kb.digit8Key, kb.digit9Key };
        KeyControl[] num = { kb.numpad0Key, kb.numpad1Key, kb.numpad2Key, kb.numpad3Key, kb.numpad4Key, kb.numpad5Key, kb.numpad6Key, kb.numpad7Key, kb.numpad8Key, kb.numpad9Key };
        for (int d = 0; d < 10; d++)
            if (fila[d].wasPressedThisFrame || num[d].wasPressedThisFrame) { Escribir(d); break; }
        if (kb.backspaceKey.wasPressedThisFrame && entrada.Length > 0) entrada = entrada.Substring(0, entrada.Length - 1);
    }

    /// <summary>Añade un dígito; con el tercero se comprueba el código.</summary>
    public void Escribir(int digito)
    {
        if (resuelto || entrada.Length >= 3) return;
        entrada += digito.ToString();
        if (sonido != null && clipTecla != null) sonido.PlayOneShot(clipTecla);
        if (entrada.Length < 3) return;

        if (entrada == codigo)
        {
            resuelto = true;
            if (indicador != null) indicador.color = new Color(0.25f, 1f, 0.35f);
            Soltar();
            if (puerta != null) puerta.ActivarMecanismo();
        }
        else
        {
            if (sonido != null && clipError != null) sonido.PlayOneShot(clipError);
            errorHasta = Time.time + 1.2f;
            entrada = "";
        }
    }
}
