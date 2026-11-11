using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Arma los 10 pisos del hotel con el kit modular de Blender. Rejilla de 1 m, huella de 20 x 40 m por piso.
/// Menú UMBRAL > 1. Preparar materiales y prefabs  /  UMBRAL > 2. Construir los 10 niveles.
/// Coordenadas del plano: X del plano = X de Unity, Y del plano = Z de Unity.
/// El piso 1 sigue el plano del primer avance; los demás lo varían con las opciones de la tabla NIVELES.
/// </summary>
public static class ConstructorNiveles
{
    const string DIR_MODELOS = "Assets/UMBRAL/Models/";
    const string DIR_MATS = "Assets/UMBRAL/Materials/";
    const string DIR_PREFABS = "Assets/UMBRAL/Prefabs/";
    const string DIR_TEX = "Assets/UMBRAL/Textures/";
    const string DIR_AUDIO = "Assets/UMBRAL/Audio/";
    const string DIR_ESCENAS = "Assets/UMBRAL/Scenes/";
    static string Escena(int n) { return DIR_ESCENAS + "Nivel" + n.ToString("00") + ".unity"; }

    // nombre -> r, g, b (lineal, igual que en Blender), metálico, suavidad, emisión
    static readonly Dictionary<string, float[]> MATS = new Dictionary<string, float[]>
    {
        { "MAT_Papel",         new[] { 0.33f, 0.30f, 0.20f, 0f, 0.10f, 0f } },
        { "MAT_Madera",        new[] { 0.21f, 0.11f, 0.055f, 0f, 0.40f, 0f } },
        { "MAT_MaderaOscura",  new[] { 0.085f, 0.045f, 0.025f, 0f, 0.45f, 0f } },
        { "MAT_PisoClaro",     new[] { 0.26f, 0.16f, 0.085f, 0f, 0.50f, 0f } },
        { "MAT_PisoOscuro",    new[] { 0.20f, 0.12f, 0.065f, 0f, 0.50f, 0f } },
        { "MAT_Techo",         new[] { 0.27f, 0.25f, 0.21f, 0f, 0.05f, 0f } },
        { "MAT_Laton",         new[] { 0.78f, 0.57f, 0.22f, 0.85f, 0.65f, 0f } },
        { "MAT_MetalOscuro",   new[] { 0.07f, 0.07f, 0.08f, 0.8f, 0.55f, 0f } },
        { "MAT_TelaRoja",      new[] { 0.33f, 0.035f, 0.045f, 0f, 0.05f, 0f } },
        { "MAT_AlfombraBorde", new[] { 0.52f, 0.39f, 0.15f, 0f, 0.05f, 0f } },
        { "MAT_Sabana",        new[] { 0.70f, 0.67f, 0.60f, 0f, 0.05f, 0f } },
        { "MAT_Vidrio",        new[] { 0.02f, 0.03f, 0.06f, 0f, 0.92f, 0f } },
        { "MAT_Pantalla",      new[] { 1.00f, 0.78f, 0.45f, 0f, 0.40f, 2.5f } },
        { "MAT_Llama",         new[] { 1.00f, 0.62f, 0.20f, 0f, 0.50f, 5.0f } },
        { "MAT_Cera",          new[] { 0.80f, 0.76f, 0.66f, 0f, 0.30f, 0f } },
        { "MAT_EntManto",      new[] { 0.012f, 0.012f, 0.016f, 0f, 0.02f, 0f } },
        { "MAT_EntHueso",      new[] { 0.34f, 0.30f, 0.24f, 0f, 0.20f, 0f } },
        { "MAT_EntOjos",       new[] { 1.00f, 0.90f, 0.65f, 0f, 0.50f, 9.0f } },
        { "MAT_EntCara",       new[] { 1.00f, 1.00f, 1.00f, 0f, 0.25f, 0f } },
        { "MAT_EntDiente",     new[] { 0.50f, 0.44f, 0.28f, 0f, 0.55f, 0f } },
        { "MAT_EntNegro",      new[] { 0f, 0f, 0f, 0f, 0f, 0f } },
    };

    // material -> textura de color, mapa de normales, metros que cubre una repetición (las UV del kit van a 1 unidad = 1 m)
    static readonly Dictionary<string, object[]> TEXTURAS = new Dictionary<string, object[]>
    {
        { "MAT_Papel",         new object[] { "T_Papel",         "T_Papel_N",  1f } },
        { "MAT_Madera",        new object[] { "T_Madera",        "T_Madera_N", 1f } },
        { "MAT_MaderaOscura",  new object[] { "T_MaderaOscura",  "T_Madera_N", 1f } },
        { "MAT_PisoClaro",     new object[] { "T_PisoClaro",     "T_Piso_N",   1f } },
        { "MAT_PisoOscuro",    new object[] { "T_PisoOscuro",    "T_Piso_N",   1f } },
        { "MAT_Techo",         new object[] { "T_Techo",         "T_Techo_N",  2f } },
        { "MAT_TelaRoja",      new object[] { "T_TelaRoja",      "T_Tela_N",   1f } },
        { "MAT_AlfombraBorde", new object[] { "T_AlfombraBorde", "T_Tela_N",   0.5f } },
        { "MAT_Sabana",        new object[] { "T_Sabana",        "T_Sabana_N", 1f } },
    };

    // ------------------------------------------------------------------ materiales
    [MenuItem("UMBRAL/1. Preparar materiales y prefabs")]
    public static void Preparar()
    {
        foreach (var d in new[] { DIR_MATS, DIR_PREFABS, DIR_ESCENAS }) Directory.CreateDirectory(d);
        AssetDatabase.Refresh();
        Materiales();
        ConfigurarURP();
        Prefabs();
        AssetDatabase.SaveAssets();
        Debug.Log("[UMBRAL] Materiales y prefabs listos.");
    }

    static Material Mat(string nombre) { return AssetDatabase.LoadAssetAtPath<Material>(DIR_MATS + nombre + ".mat"); }

    static AudioClip Clip(string nombre)
    {
        var c = AssetDatabase.LoadAssetAtPath<AudioClip>(DIR_AUDIO + nombre + ".wav");
        if (c == null) Debug.LogError("[UMBRAL] Falta el sonido " + nombre + ".wav en " + DIR_AUDIO);
        return c;
    }

    static Texture2D Tex(string nombre, bool normales)
    {
        string ruta = DIR_TEX + nombre + ".png";
        var imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp == null) { Debug.LogError("[UMBRAL] Falta la textura " + ruta); return null; }
        var tipo = normales ? TextureImporterType.NormalMap : TextureImporterType.Default;
        if (imp.textureType != tipo || imp.anisoLevel != 8 || imp.wrapMode != TextureWrapMode.Repeat)
        {
            imp.textureType = tipo; imp.anisoLevel = 8; imp.wrapMode = TextureWrapMode.Repeat; imp.mipmapEnabled = true;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    static void Materiales()
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        foreach (var kv in MATS)
        {
            var m = Mat(kv.Key);
            if (m == null) { m = new Material(lit); AssetDatabase.CreateAsset(m, DIR_MATS + kv.Key + ".mat"); }
            var v = kv.Value;
            var lineal = new Color(v[0], v[1], v[2], 1f);
            m.SetColor("_BaseColor", lineal.gamma);
            m.SetFloat("_Metallic", v[3]);
            m.SetFloat("_Smoothness", v[4]);
            object[] tx;
            if (TEXTURAS.TryGetValue(kv.Key, out tx))
            {
                // Con textura, el color base queda en blanco: el tono lo da la imagen
                float rep = 1f / (float)tx[2];
                m.SetColor("_BaseColor", Color.white);
                m.SetTexture("_BaseMap", Tex((string)tx[0], false));
                m.SetTextureScale("_BaseMap", new Vector2(rep, rep));
                m.SetTexture("_BumpMap", Tex((string)tx[1], true));
                m.SetFloat("_BumpScale", 1f);
                m.EnableKeyword("_NORMALMAP");
            }
            if (v[5] > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                m.SetColor("_EmissionColor", lineal * v[5]);
            }
            EditorUtility.SetDirty(m);
        }
        var manto = Mat("MAT_EntManto");
        manto.SetFloat("_Cull", 0f);                 // el manto se ve por dentro y por fuera
        manto.SetFloat("_SpecularHighlights", 0f); manto.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");       // tela sin brillos: queda como silueta
        manto.SetFloat("_EnvironmentReflections", 0f); manto.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        // Cuencas y garganta: negro sin iluminación, para que ninguna luz las aclare
        var vacio = Mat("MAT_EntNegro");
        vacio.shader = Shader.Find("Universal Render Pipeline/Unlit");
        vacio.SetColor("_BaseColor", Color.black);
        // La textura de la cara cubre x de -0.15 a 0.15 m y la altura de 2.38 a 2.72 m del modelo (las UV van en metros)
        var cara = Mat("MAT_EntCara");
        cara.SetTexture("_BaseMap", Tex("T_EspectroCara", false));
        cara.SetTextureScale("_BaseMap", new Vector2(1f / 0.30f, 1f / 0.34f));
        cara.SetTextureOffset("_BaseMap", new Vector2(0.5f, 0f));
        foreach (var viejo in new[] { "MAT_Entidad", "MAT_EntidadOjos" }) AssetDatabase.DeleteAsset(DIR_MATS + viejo + ".mat");
        AssetDatabase.SaveAssets();

        // Los FBX usan los materiales compartidos de Assets/UMBRAL/Materials en lugar de copias por modelo
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { DIR_MODELOS.TrimEnd('/') }))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            var imp = (ModelImporter)AssetImporter.GetAtPath(ruta);
            imp.bakeAxisConversion = true;
            imp.importCameras = false;
            imp.importLights = false;
            imp.importAnimation = false;
            imp.animationType = ModelImporterAnimationType.None;
            foreach (var m in AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Material>())
            {
                var destino = Mat(m.name);
                if (destino != null) imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name), destino);
            }
            imp.SaveAndReimport();
        }
    }

    static void ConfigurarURP()
    {
        // Forward+ : permite muchas luces puntuales por objeto (18 lámparas de pared)
        var renderer = AssetDatabase.LoadMainAssetAtPath("Assets/Settings/PC_Renderer.asset");
        if (renderer != null)
        {
            var so = new SerializedObject(renderer);
            var modo = so.FindProperty("m_RenderingMode");
            if (modo != null) { modo.intValue = 2; so.ApplyModifiedPropertiesWithoutUndo(); }
        }
    }

    // ------------------------------------------------------------------ utilidades
    static GameObject Modelo(string nombre, Transform padre)
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(DIR_MODELOS + nombre + ".fbx");
        var g = (GameObject)PrefabUtility.InstantiatePrefab(src);
        g.transform.SetParent(padre, false);
        return g;
    }

    static GameObject Prefab(string nombre) { return AssetDatabase.LoadAssetAtPath<GameObject>(DIR_PREFABS + nombre + ".prefab"); }

    static BoxCollider Caja(GameObject g, Vector3 centro, Vector3 tam, bool trigger = false)
    {
        var c = g.AddComponent<BoxCollider>();
        c.center = centro; c.size = tam; c.isTrigger = trigger;
        return c;
    }

    static Transform Vacio(string nombre, Transform padre, Vector3 pos)
    {
        var g = new GameObject(nombre);
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        return g.transform;
    }

    static AudioSource Audio(GameObject padre, string clip, bool bucle = false, bool espacial = true, float volumen = 0.8f, string nombre = "Sonido")
    {
        var g = new GameObject(nombre);
        g.transform.SetParent(padre.transform, false);
        g.transform.localPosition = new Vector3(0f, 1f, 0f);
        var a = g.AddComponent<AudioSource>();
        a.playOnAwake = false; a.loop = bucle; a.volume = volumen;
        a.spatialBlend = espacial ? 1f : 0f; a.minDistance = 2f; a.maxDistance = 20f;
        a.clip = Clip(clip);
        return a;
    }

    static Light Luz(Transform padre, Vector3 pos, Color color, float intensidad, float rango, LightShadows sombras = LightShadows.None)
    {
        var g = new GameObject("Luz");
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        var l = g.AddComponent<Light>();
        l.type = LightType.Point; l.color = color; l.intensity = intensidad; l.range = rango; l.shadows = sombras;
        return l;
    }

    static void Guardar(GameObject g, bool estatico = false)
    {
        if (estatico) foreach (var t in g.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
        PrefabUtility.SaveAsPrefabAsset(g, DIR_PREFABS + g.name + ".prefab");
        Object.DestroyImmediate(g);
    }

    // ------------------------------------------------------------------ prefabs
    static void Prefabs()
    {
        GameObject r, m;

        // Estructura
        r = new GameObject("EST_MuroEstandar"); Modelo(r.name, r.transform); Caja(r, new Vector3(1f, 1.5f, 0f), new Vector3(2f, 3f, 0.5f)); Guardar(r, true);
        r = new GameObject("EST_MuroMedio"); Modelo(r.name, r.transform); Caja(r, new Vector3(0.5f, 1.5f, 0f), new Vector3(1f, 3f, 0.5f)); Guardar(r, true);
        r = new GameObject("EST_MuroVentana"); Modelo(r.name, r.transform); Caja(r, new Vector3(1f, 1.5f, 0f), new Vector3(2f, 3f, 0.5f)); Guardar(r, true);
        r = new GameObject("EST_MuroVano"); Modelo(r.name, r.transform);
        Caja(r, new Vector3(0.125f, 1.5f, 0f), new Vector3(0.25f, 3f, 0.5f));
        Caja(r, new Vector3(1.875f, 1.5f, 0f), new Vector3(0.25f, 3f, 0.5f));
        Caja(r, new Vector3(1f, 2.75f, 0f), new Vector3(1.5f, 0.5f, 0.5f)); Guardar(r, true);
        r = new GameObject("EST_MarcoPuerta"); Modelo(r.name, r.transform);
        Caja(r, new Vector3(0.075f, 1.25f, 0f), new Vector3(0.15f, 2.5f, 0.5f));
        Caja(r, new Vector3(1.425f, 1.25f, 0f), new Vector3(0.15f, 2.5f, 0.5f));
        Caja(r, new Vector3(0.75f, 2.4f, 0f), new Vector3(1.5f, 0.2f, 0.5f)); Guardar(r, true);
        r = new GameObject("EST_Pilar"); Modelo(r.name, r.transform); Caja(r, new Vector3(0f, 1.5f, 0f), new Vector3(1f, 3f, 1f)); Guardar(r, true);
        r = new GameObject("EST_Piso"); Modelo(r.name, r.transform); Guardar(r, true);
        r = new GameObject("EST_Techo"); Modelo(r.name, r.transform); Guardar(r, true);

        // Puerta: la raíz queda en la bisagra. "Hoja" es el modelo de Blender (pivote en la bisagra) y es lo único que rota.
        r = new GameObject("INT_Puerta");
        m = Modelo("INT_Puerta", r.transform); m.name = "Hoja";
        Caja(m, new Vector3(0.6f, 1.15f, 0f), new Vector3(1.2f, 2.3f, 0.1f));
        var placa = Modelo("PRP_PlacaNumero", m.transform);
        placa.transform.localPosition = new Vector3(0.6f, 1.75f, -0.05f);
        var puerta = r.AddComponent<PuertaInteractiva>();
        puerta.hoja = m.transform;
        puerta.areaDeteccion = Caja(r, new Vector3(0.6f, 1.25f, 0f), new Vector3(2f, 2.5f, 2f), true);
        puerta.puntoFoco = Vacio("Foco", r.transform, new Vector3(0.6f, 1.2f, 0f));
        puerta.sonido = Audio(r, "puerta_abrir");
        puerta.sonidoCerrada = Clip("puerta_cerrada");
        Guardar(r);

        // Clóset
        r = new GameObject("INT_Closet");
        m = Modelo("INT_Closet", r.transform);
        Caja(r, new Vector3(0f, 1.1f, 0f), new Vector3(1.2f, 2.2f, 0.6f));
        Caja(r, new Vector3(0f, 1.1f, -0.85f), new Vector3(1.4f, 2.2f, 1.3f), true);
        var closet = r.AddComponent<ClosetEscondite>();
        closet.hojaIzq = m.transform.Find("HojaIzq");
        closet.hojaDer = m.transform.Find("HojaDer");
        closet.puntoFoco = Vacio("Foco", r.transform, new Vector3(0f, 1.3f, -0.3f));
        closet.sonido = Audio(r, "closet");
        Guardar(r);

        // Mesa de noche con cajón (la llave va dentro del cajón, desactivada por defecto)
        r = new GameObject("INT_MesaNoche");
        m = Modelo("INT_MesaNoche", r.transform);
        Caja(r, new Vector3(0f, 0.3f, 0f), new Vector3(0.5f, 0.6f, 0.5f));
        Caja(r, new Vector3(0f, 0.9f, -0.75f), new Vector3(1.3f, 1.8f, 1.3f), true);
        var mesa = r.AddComponent<MesaDeNoche>();
        mesa.cajon = m.transform.Find("Cajon");
        var llave = Modelo("INT_Llave", mesa.cajon);
        llave.transform.localPosition = new Vector3(0f, 0.013f, 0.2f);
        llave.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
        llave.SetActive(false);
        mesa.llave = llave;
        mesa.puntoFoco = Vacio("Foco", r.transform, new Vector3(0f, 0.5f, -0.3f));
        mesa.sonido = Audio(r, "cajon_abrir");
        mesa.sonidoLlave = Clip("llave");
        Guardar(r);

        // Lámpara de pared
        r = new GameObject("INT_Lampara");
        m = Modelo("INT_Lampara", r.transform);
        var lamp = r.AddComponent<LamparaPared>();
        lamp.luz = Luz(r.transform, new Vector3(0f, 0.1f, -0.2f), new Color(1f, 0.72f, 0.45f), 2.6f, 7.5f);
        lamp.pantalla = m.GetComponent<MeshRenderer>();
        lamp.indiceMaterial = System.Array.FindIndex(lamp.pantalla.sharedMaterials, x => x != null && x.name == "MAT_Pantalla");
        Guardar(r);

        // Interruptor
        r = new GameObject("INT_Interruptor");
        m = Modelo("INT_Interruptor", r.transform);
        Caja(r, new Vector3(0f, -0.3f, -0.8f), new Vector3(1.6f, 2.2f, 1.4f), true);
        var inter = r.AddComponent<InterruptorLuz>();
        inter.palanca = m.transform.Find("Palanca");
        inter.puntoFoco = Vacio("Foco", r.transform, new Vector3(0f, 0f, -0.05f));
        inter.sonido = Audio(r, "palanca");
        inter.indicador = Luz(r.transform, new Vector3(0f, 0.28f, -0.14f), new Color(1f, 0.15f, 0.08f), 0.9f, 2.4f);
        inter.indicador.gameObject.name = "LuzIndicador";
        Guardar(r);

        // Props
        r = new GameObject("PRP_Cama"); Modelo(r.name, r.transform); Caja(r, new Vector3(0f, 0.3f, 0f), new Vector3(1.6f, 0.6f, 2.1f)); Guardar(r, true);
        r = new GameObject("PRP_Mostrador"); Modelo(r.name, r.transform); Caja(r, new Vector3(0f, 0.55f, 0f), new Vector3(4f, 1.1f, 1f)); Guardar(r, true);
        r = new GameObject("PRP_Sofa"); Modelo(r.name, r.transform); Caja(r, new Vector3(0f, 0.425f, 0f), new Vector3(2f, 0.85f, 0.9f)); Guardar(r, true);
        r = new GameObject("PRP_Alfombra"); Modelo(r.name, r.transform); Guardar(r, true);
        r = new GameObject("PRP_Candelabro"); m = Modelo(r.name, r.transform);
        m.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        // Foco hacia abajo con sombras (un solo mapa de sombras, sin costuras en el techo) y un relleno sin sombras
        var foco = Luz(r.transform, new Vector3(0f, -0.6f, 0f), new Color(1f, 0.74f, 0.46f), 9f, 14f, LightShadows.Soft);
        foco.type = LightType.Spot; foco.spotAngle = 165f; foco.innerSpotAngle = 120f;
        foco.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Luz(r.transform, new Vector3(0f, -0.6f, 0f), new Color(1f, 0.74f, 0.46f), 4f, 13f).gameObject.name = "LuzRelleno";
        Guardar(r);

        // Espectro: el modelo de Blender tiene el origen en el suelo; la raíz viaja a 1.5 m de altura
        r = new GameObject("ENT_Espectro");
        m = Modelo("ENT_Espectro", r.transform);
        m.transform.localPosition = new Vector3(0f, -1.5f, 0f);
        foreach (var mr in r.GetComponentsInChildren<MeshRenderer>()) mr.shadowCastingMode = ShadowCastingMode.Off;
        var vis = r.AddComponent<EntidadVisual>();
        vis.modelo = m.transform;
        vis.brazoIzq = m.transform.Find("BrazoIzq");
        vis.brazoDer = m.transform.Find("BrazoDer");
        vis.mandibula = m.transform.Find("Mandibula");
        vis.jirones = m.transform.Cast<Transform>().Where(t => t.name.StartsWith("Jiron")).ToArray();
        // Luz desde abajo que solo se enciende en el susto final
        vis.luzSusto = Luz(m.transform, new Vector3(0f, 2.28f, 0.36f), new Color(0.75f, 0.95f, 0.85f), 0.9f, 1.1f);
        vis.luzSusto.gameObject.name = "LuzSusto";
        vis.luzSusto.enabled = false;
        // La luz va dentro de la capucha, detrás de la máscara: alumbra el pasillo y deja la cara en sombra
        vis.luzOjos = Luz(m.transform, new Vector3(0f, 2.5f, 0.02f), new Color(1f, 0.90f, 0.68f), 2.4f, 6.5f);
        vis.luzOjos.gameObject.name = "LuzOjos";
        Guardar(r);
    }

    // ------------------------------------------------------------------ niveles
    public const int TOTAL_NIVELES = 10;

    /// <summary>Opciones de un piso. Banderas: P = puerta a mitad del pasillo, O / E = habitaciones del lado oeste / este unidas
    /// en un salón, M = decoración en espejo, A = apagón (casi todo el pasillo a oscuras).</summary>
    class Def
    {
        public bool partido, salonO, salonE, espejo, apagon;
        public float prob, aviso, vel, intMin, intMax, doble;
        public Def(string banderas, float prob, float aviso, float vel, float intMin, float intMax, float doble)
        {
            partido = banderas.Contains("P"); salonO = banderas.Contains("O"); salonE = banderas.Contains("E");
            espejo = banderas.Contains("M"); apagon = banderas.Contains("A");
            this.prob = prob; this.aviso = aviso; this.vel = vel; this.intMin = intMin; this.intMax = intMax; this.doble = doble;
        }
    }

    // banderas, probabilidad de aparición por puerta, segundos de aviso, velocidad (m/s), intervalo entre apariciones (s), probabilidad de doble pasada
    static readonly Def[] NIVELES =
    {
        new Def("",     0.30f, 4.0f, 13.0f, 60f, 90f, 0.00f),   // piso 1: plano del primer avance
        new Def("M",    0.35f, 3.8f, 13.5f, 50f, 75f, 0.00f),
        new Def("P",    0.40f, 3.6f, 14.0f, 45f, 65f, 0.00f),
        new Def("OM",   0.45f, 3.4f, 14.5f, 40f, 60f, 0.10f),
        new Def("PA",   0.50f, 3.2f, 15.0f, 35f, 52f, 0.15f),
        new Def("E",    0.55f, 3.0f, 15.5f, 30f, 46f, 0.25f),
        new Def("POM",  0.60f, 2.8f, 16.0f, 26f, 40f, 0.30f),
        new Def("OEA",  0.68f, 2.6f, 16.5f, 22f, 34f, 0.40f),
        new Def("PEMA", 0.76f, 2.4f, 17.0f, 18f, 28f, 0.50f),
        new Def("POEA", 0.85f, 2.2f, 18.0f, 14f, 22f, 0.60f),
    };

    struct Tramo { public bool v; public float c, a, b; public string esp; public Tramo(bool v, float c, float a, float b, string esp) { this.v = v; this.c = c; this.a = a; this.b = b; this.esp = esp; } }

    /// <summary>Tramos de muro entre centros de pilares. esp = "ranura:tipo" (V = ventana; otro texto = id de puerta).</summary>
    static List<Tramo> Tramos(Def d)
    {
        var t = new List<Tramo>
        {
            new Tramo(false, 0.5f, 0.5f, 19.5f, "1:V,2:V,6:V,7:V"),   // fachada sur
            new Tramo(false, 39.5f, 0.5f, 19.5f, "4:SAL"),            // muro norte con la salida
            new Tramo(true, 0.5f, 0.5f, 10.5f, ""), new Tramo(true, 0.5f, 10.5f, 20.5f, "1:V,2:V"), new Tramo(true, 0.5f, 20.5f, 30.5f, "1:V,2:V"), new Tramo(true, 0.5f, 30.5f, 39.5f, ""),
            new Tramo(true, 19.5f, 0.5f, 10.5f, ""), new Tramo(true, 19.5f, 10.5f, 20.5f, "1:V,2:V"), new Tramo(true, 19.5f, 20.5f, 30.5f, "1:V,2:V"), new Tramo(true, 19.5f, 30.5f, 39.5f, ""),
            new Tramo(false, 10.5f, 0.5f, 6.5f, ""), new Tramo(false, 10.5f, 6.5f, 13.5f, "1:REC"), new Tramo(false, 10.5f, 13.5f, 19.5f, ""),
            new Tramo(false, 30.5f, 0.5f, 6.5f, ""), new Tramo(false, 30.5f, 6.5f, 13.5f, "1:SEG"), new Tramo(false, 30.5f, 13.5f, 19.5f, ""),
            new Tramo(true, 6.5f, 10.5f, 20.5f, "2:H1"), new Tramo(true, 6.5f, 20.5f, 30.5f, "2:H3"),
            new Tramo(true, 13.5f, 10.5f, 20.5f, "2:H2"), new Tramo(true, 13.5f, 20.5f, 30.5f, "2:H4"),
        };
        if (!d.salonO) t.Add(new Tramo(false, 20.5f, 0.5f, 6.5f, ""));     // muro entre las dos habitaciones del oeste
        if (!d.salonE) t.Add(new Tramo(false, 20.5f, 13.5f, 19.5f, ""));   // muro entre las dos habitaciones del este
        if (d.partido) t.Add(new Tramo(false, 20.5f, 6.5f, 13.5f, "1:MED")); // puerta a mitad del pasillo
        return t;
    }

    /// <summary>Número de la primera puerta principal del piso: las puertas principales se numeran de corrido en todo el juego.</summary>
    static int PrimeraPuerta(int n)
    {
        int k = 1;
        for (int i = 0; i < n - 1; i++) k += NIVELES[i].partido ? 4 : 3;
        return k;
    }

    static GameObject Poner(string prefab, Transform padre, Vector3 pos, float rotY, string nombre = null)
    {
        var g = (GameObject)PrefabUtility.InstantiatePrefab(Prefab(prefab), padre);
        g.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotY, 0f));
        if (nombre != null) g.name = nombre;
        return g;
    }

    [MenuItem("UMBRAL/2. Construir los 10 niveles")]
    public static void ConstruirTodos()
    {
        if (Prefab("INT_Puerta") == null || Prefab("ENT_Espectro") == null) Preparar();
        var escenas = new List<EditorBuildSettingsScene>();
        for (int n = 1; n <= TOTAL_NIVELES; n++) { Construir(n); escenas.Add(new EditorBuildSettingsScene(Escena(n), true)); }
        EditorBuildSettings.scenes = escenas.ToArray();
        AssetDatabase.DeleteAsset(DIR_ESCENAS + "Nivel1_Hotel.unity");   // escena del prototipo de un solo nivel
        EditorSceneManager.OpenScene(Escena(1));
        Debug.Log("[UMBRAL] " + TOTAL_NIVELES + " niveles construidos.");
    }

    public static void Construir(int n)
    {
        Def def = NIVELES[n - 1];
        System.Func<float, float> MX = x => def.espejo ? 20f - x : x;      // espejo de la decoración respecto al eje del pasillo
        System.Func<float, float> MR = r => def.espejo ? -r : r;
        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var conteo = new SortedDictionary<string, int>();
        System.Action<string> contar = k => { int c; conteo.TryGetValue(k, out c); conteo[k] = c + 1; };

        var nivel = new GameObject("Nivel" + n.ToString("00")).transform;
        var tEstructura = Vacio("Estructura", nivel, Vector3.zero);
        var tMuros = Vacio("Muros", tEstructura, Vector3.zero);
        var tPilares = Vacio("Pilares", tEstructura, Vector3.zero);
        var tPisos = Vacio("Pisos", tEstructura, Vector3.zero);
        var tTechos = Vacio("Techos", tEstructura, Vector3.zero);
        var tInter = Vacio("Interactivos", nivel, Vector3.zero);
        var tPuertas = Vacio("Puertas", tInter, Vector3.zero);
        var tClosets = Vacio("Closets", tInter, Vector3.zero);
        var tMesas = Vacio("MesasDeNoche", tInter, Vector3.zero);
        var tLamparas = Vacio("Lamparas", tInter, Vector3.zero);
        var tProps = Vacio("Props", nivel, Vector3.zero);

        // --- puertas del piso: id -> número, ángulo de apertura (hacia la zona siguiente), con seguro, placa del otro lado
        int num = PrimeraPuerta(n);
        var datos = new Dictionary<string, object[]>();
        datos["REC"] = new object[] { (num++).ToString("000"), -90f, false, false };
        if (def.partido) datos["MED"] = new object[] { (num++).ToString("000"), -90f, false, false };
        datos["SEG"] = new object[] { (num++).ToString("000"), -90f, true, false };
        datos["SAL"] = new object[] { (num++).ToString("000"), -90f, false, false };
        datos["H1"] = new object[] { (n * 100 + 1).ToString(), -90f, false, false };
        datos["H3"] = new object[] { (n * 100 + 3).ToString(), -90f, false, false };
        datos["H2"] = new object[] { (n * 100 + 2).ToString(), 90f, false, true };
        datos["H4"] = new object[] { (n * 100 + 4).ToString(), 90f, false, true };

        // --- muros, marcos y puertas
        var puertas = new Dictionary<string, PuertaInteractiva>();
        foreach (var t in Tramos(def))
        {
            var esp = new Dictionary<int, string>();
            foreach (var par in t.esp.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries)) { var kv = par.Split(':'); esp[int.Parse(kv[0])] = kv[1]; }
            float ini = t.a + 0.5f, fin = t.b - 0.5f, p = ini; int k = 0;
            float rot = t.v ? -90f : 0f;
            Vector3 dir = t.v ? Vector3.forward : Vector3.right;
            System.Func<float, Vector3> en = s => t.v ? new Vector3(t.c, 0f, s) : new Vector3(s, 0f, t.c);
            while (fin - p > 1e-4f)
            {
                if (fin - p < 2f - 1e-4f) { Poner("EST_MuroMedio", tMuros, en(p), rot); contar("EST_MuroMedio"); break; }
                string tipo; esp.TryGetValue(k, out tipo);
                if (tipo == null) { Poner("EST_MuroEstandar", tMuros, en(p), rot); contar("EST_MuroEstandar"); }
                else if (tipo == "V") { Poner("EST_MuroVentana", tMuros, en(p), rot); contar("EST_MuroVentana"); }
                else
                {
                    Poner("EST_MuroVano", tMuros, en(p), rot); contar("EST_MuroVano");
                    Poner("EST_MarcoPuerta", tMuros, en(p) + dir * 0.25f, rot, "EST_MarcoPuerta_" + tipo); contar("EST_MarcoPuerta");
                    var d = datos[tipo];
                    puertas[tipo] = PonerPuerta(tPuertas, en(p) + dir * 0.4f, rot, (string)d[0], (float)d[1], (bool)d[2], (bool)d[3]);
                    contar("INT_Puerta"); contar("PRP_PlacaNumero");
                }
                p += 2f; k++;
            }
        }

        // --- pilares
        foreach (float x in new[] { 0.5f, 6.5f, 13.5f, 19.5f })
            foreach (float z in new[] { 10.5f, 20.5f, 30.5f }) { Poner("EST_Pilar", tPilares, new Vector3(x, 0f, z), 0f); contar("EST_Pilar"); }
        foreach (float x in new[] { 0.5f, 19.5f })
            foreach (float z in new[] { 0.5f, 39.5f }) { Poner("EST_Pilar", tPilares, new Vector3(x, 0f, z), 0f); contar("EST_Pilar"); }

        // --- piso y techo (módulos de 2 x 2 m)
        for (int i = 0; i < 10; i++)
            for (int j = 0; j < 20; j++)
            {
                Poner("EST_Piso", tPisos, new Vector3(1f + 2f * i, -0.1f, 1f + 2f * j), (i + j) % 2 == 0 ? 0f : 90f); contar("EST_Piso");
                Poner("EST_Techo", tTechos, new Vector3(1f + 2f * i, 3f, 1f + 2f * j), 0f); contar("EST_Techo");
            }
        Caja(tPisos.gameObject, new Vector3(10f, -0.05f, 20.5f), new Vector3(20f, 0.1f, 41f));
        Caja(tTechos.gameObject, new Vector3(10f, 3.05f, 20f), new Vector3(20f, 0.1f, 40f));
        var barrera = Vacio("BarreraSalida", tEstructura, Vector3.zero).gameObject;
        Caja(barrera, new Vector3(10f, 1.5f, 40.7f), new Vector3(6f, 3f, 0.2f));
        Caja(barrera, new Vector3(8.6f, 1.5f, 40.2f), new Vector3(0.2f, 3f, 1f));
        Caja(barrera, new Vector3(11.4f, 1.5f, 40.2f), new Vector3(0.2f, 3f, 1f));

        // --- lámparas de pared: x, z, giro (sale hacia +X = -90, hacia -X = 90), zona (0 normal, 1 sala final, 2 pasillo que se apaga en los apagones)
        float O = 0.75f, E = 19.25f, PO = 6.75f, PE = 13.25f;
        var lamparas = new[]
        {
            new[] { O, 3f, -90f, 0f }, new[] { O, 8.5f, -90f, 0f }, new[] { E, 3f, 90f, 0f }, new[] { E, 8.5f, 90f, 0f },          // recepción
            new[] { PO, 13.5f, -90f, 0f }, new[] { PO, 21.6f, -90f, 2f }, new[] { PO, 23.8f, -90f, 2f },                           // pasillo oeste
            new[] { PE, 18.5f, 90f, 2f }, new[] { PE, 21.6f, 90f, 2f }, new[] { PE, 28.5f, 90f, 0f },                              // pasillo este
            new[] { O, 12.2f, -90f, 0f }, new[] { O, 22.2f, -90f, 0f }, new[] { E, 12.2f, 90f, 0f }, new[] { E, 22.2f, 90f, 0f },  // habitaciones
            new[] { O, 32f, -90f, 1f }, new[] { O, 38f, -90f, 1f }, new[] { E, 36.5f, 90f, 1f }, new[] { E, 38f, 90f, 1f },        // sala final (apagadas)
        };
        var lamparasSala = new List<LamparaPared>();
        foreach (var l in lamparas)
        {
            var g = Poner("INT_Lampara", tLamparas, new Vector3(MX(l[0]), 1.95f, l[1]), MR(l[2])); contar("INT_Lampara");
            var lp = g.GetComponent<LamparaPared>();
            if (l[3] == 1f) { lp.encendidaAlInicio = false; lp.luz.intensity = 3.8f; lp.luz.range = 12f; lamparasSala.Add(lp); g.name = "INT_Lampara_SalaFinal"; }
            else if (l[3] == 2f && def.apagon) { lp.encendidaAlInicio = false; g.name = "INT_Lampara_Fundida"; }
        }

        // --- clósets: x, z, giro (frente hacia +X = -90, hacia -X = 90, hacia -Z = 0)
        var closets = new[]
        {
            new[] { 7.05f, 18.6f, -90f }, new[] { 7.05f, 29.0f, -90f }, new[] { 12.95f, 12.6f, 90f }, new[] { 12.95f, 22.6f, 90f },   // pasillo
            new[] { 3.1f, 19.95f, 0f }, new[] { 16.9f, 19.95f, 0f }, new[] { 3.1f, 29.95f, 0f }, new[] { 16.9f, 29.95f, 0f },         // habitaciones
            new[] { 1.05f, 34.6f, -90f }, new[] { 18.95f, 34.6f, 90f },                                                               // sala final
        };
        foreach (var c in closets)
        {
            float x = MX(c[0]);
            // sin el muro que divide las habitaciones, el clóset que se apoyaba en él pasa delante del pilar de la pared exterior
            bool sinMuro = Mathf.Approximately(c[1], 19.95f) && ((x < 6.5f && def.salonO) || (x > 13.5f && def.salonE));
            if (sinMuro) Poner("INT_Closet", tClosets, new Vector3(x < 10f ? 1.3f : 18.7f, 0f, 20.5f), x < 10f ? -90f : 90f);
            else Poner("INT_Closet", tClosets, new Vector3(x, 0f, c[1]), MR(c[2]));
            contar("INT_Closet");
        }

        // --- habitaciones: cama y mesa de noche. La llave la reparte EstadoJuego al azar al cargar el piso.
        int h = 0;
        foreach (float yb in new[] { 11f, 21f })
        {
            foreach (bool este in new[] { false, true })
            {
                string hab = (n * 100 + h + 1).ToString();
                Poner("PRP_Cama", tProps, new Vector3(este ? 18.2f : 1.8f, 0f, yb + 4f), este ? 90f : -90f, "PRP_Cama_" + hab); contar("PRP_Cama");
                Poner("INT_MesaNoche", tMesas, new Vector3(este ? 19f : 1f, 0f, yb + 5.25f), este ? 90f : -90f, "INT_MesaNoche_" + hab); contar("INT_MesaNoche");
                h++;
            }
        }

        // --- recepción y pasillo
        Poner("PRP_Mostrador", tProps, new Vector3(MX(4.5f), 0f, 6.5f), 0f); contar("PRP_Mostrador");
        Poner("PRP_Sofa", tProps, new Vector3(MX(15f), 0f, 3.45f), 180f); contar("PRP_Sofa");
        Poner("PRP_Sofa", tProps, new Vector3(MX(15f), 0f, 7.05f), 0f); contar("PRP_Sofa");
        Poner("PRP_Candelabro", tProps, new Vector3(MX(12.5f), 3f, 5.2f), 0f); contar("PRP_Candelabro");
        for (int i = 0; i < 9; i++) { Poner("PRP_Alfombra", tProps, new Vector3(10f, 0f, 12.5f + 2f * i), 0f); contar("PRP_Alfombra"); }

        // --- interruptor de la sala final
        var inter = Poner("INT_Interruptor", tInter, new Vector3(MX(E), 1.3f, 32.6f), MR(90f)).GetComponent<InterruptorLuz>(); contar("INT_Interruptor");
        inter.lamparas = lamparasSala.ToArray();

        // --- jugador (Spawn Point del plano: 10.0, 2.6 mirando al norte)
        var jugador = new GameObject("Jugador");
        jugador.tag = "Player";
        jugador.transform.position = new Vector3(10f, 0.05f, 2.6f);
        var cc = jugador.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.3f; cc.center = new Vector3(0f, 0.9f, 0f); cc.stepOffset = 0.3f;
        var camGo = new GameObject("CamaraJugador");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(jugador.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 1.62f, 0f);
        var cam = camGo.AddComponent<Camera>();
        cam.nearClipPlane = 0.05f; cam.farClipPlane = 80f; cam.fieldOfView = 70f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        camGo.AddComponent<AudioListener>();
        Luz(camGo.transform, new Vector3(0.15f, -0.15f, 0.1f), new Color(1f, 0.82f, 0.6f), 0.9f, 4.5f).gameObject.name = "LuzJugador";
        var ctrl = jugador.AddComponent<JugadorController>(); ctrl.camara = camGo.transform;
        ctrl.pasos = Audio(jugador, "paso_1", false, false, 0.5f, "Pasos");
        ctrl.pasos.transform.localPosition = Vector3.zero;
        ctrl.clipsPasos = new[] { Clip("paso_1"), Clip("paso_2"), Clip("paso_3"), Clip("paso_4") };
        var interaccion = jugador.AddComponent<JugadorInteraccion>(); interaccion.controlador = ctrl; interaccion.camara = camGo.transform;

        // --- sistemas
        var sistemas = new GameObject("Sistemas").transform;
        var estado = new GameObject("EstadoJuego"); estado.transform.SetParent(sistemas, false);
        var estadoJuego = estado.AddComponent<EstadoJuego>();
        estadoJuego.jugador = ctrl;
        estadoJuego.nivel = n;
        estadoJuego.totalNiveles = TOTAL_NIVELES;
        estadoJuego.susto = Audio(estado, "susto", false, false, 0.9f, "Susto");
        var ambiente = Audio(sistemas.gameObject, "ambiente_hotel", true, false, 0.35f, "Ambiente");
        ambiente.playOnAwake = true;
        var hud = new GameObject("HUD"); hud.transform.SetParent(sistemas, false); hud.AddComponent<HudJuego>();

        var meta = new GameObject("MetaNivel"); meta.transform.SetParent(sistemas, false);
        meta.transform.position = new Vector3(10f, 1.25f, 40.05f);
        var zona = meta.AddComponent<BoxCollider>(); zona.size = new Vector3(1.6f, 2.5f, 0.6f); zona.isTrigger = true;
        meta.AddComponent<MetaNivel>().jugador = jugador.transform;

        // --- espectro y su director: cada piso sube la frecuencia, la velocidad y acorta el aviso
        var entidad = Poner("ENT_Espectro", sistemas, Vector3.zero, 0f, "Entidad");
        var dirGo = new GameObject("DirectorEntidad"); dirGo.transform.SetParent(sistemas, false);
        var director = dirGo.AddComponent<DirectorEntidad>();
        director.jugador = ctrl; director.camaraJugador = camGo.transform;
        director.entidad = entidad; director.visual = entidad.GetComponent<EntidadVisual>();
        director.puertaRecepcion = puertas["REC"]; director.puertaSalaFinal = puertas["SEG"]; director.puertaSalida = puertas["SAL"];
        director.probabilidadPorPuerta = def.prob; director.segundosDeAviso = def.aviso; director.velocidad = def.vel;
        director.intervaloMin = def.intMin; director.intervaloMax = def.intMax; director.probabilidadDoble = def.doble;
        director.rugido = Audio(dirGo, "entidad_rugido", true, false, 0.9f, "Rugido");
        director.zumbido = Audio(dirGo, "luz_zumbido", true, false, 0.45f, "Zumbido");
        entidad.SetActive(false);

        // --- ambiente: sin sol, luz ambiental muy baja y niebla
        var volGo = new GameObject("Global Volume"); volGo.transform.SetParent(sistemas, false);
        var vol = volGo.AddComponent<Volume>(); vol.isGlobal = true;
        vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.03f, 0.029f, 0.038f);
        RenderSettings.reflectionIntensity = 0.3f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.012f, 0.012f, 0.018f);
        RenderSettings.fogDensity = 0.03f;

        EditorSceneManager.SaveScene(escena, Escena(n));
        Debug.Log("[UMBRAL] Nivel " + n + " construido. Conteo de assets: " + string.Join(", ", conteo.Select(kv => kv.Key + "=" + kv.Value)));
    }

    static PuertaInteractiva PonerPuerta(Transform padre, Vector3 pos, float rotY, string numero, float angulo, bool seguro, bool placaAlReves)
    {
        var g = Poner("INT_Puerta", padre, pos, rotY, "INT_Puerta_" + numero);
        var p = g.GetComponent<PuertaInteractiva>();
        p.numero = numero; p.anguloApertura = angulo; p.tieneSeguro = seguro;
        var placa = p.hoja.Find("PRP_PlacaNumero");
        if (placaAlReves)
        {
            placa.localPosition = new Vector3(0.6f, 1.75f, 0.05f);
            placa.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }
        var digitos = AssetDatabase.LoadAssetAtPath<GameObject>(DIR_MODELOS + "PRP_Digitos.fbx").transform;
        float paso = numero.Length > 3 ? 0.062f : 0.075f;   // cuatro dígitos (piso 10) van más juntos para caber en la placa
        for (int i = 0; i < numero.Length; i++)
        {
            var src = digitos.Find("D" + numero[i]);
            var d = new GameObject("Digito_" + numero[i]);
            d.transform.SetParent(placa, false);
            d.transform.localPosition = new Vector3((i - (numero.Length - 1) * 0.5f) * paso, -0.04f, -0.02f);
            d.AddComponent<MeshFilter>().sharedMesh = src.GetComponent<MeshFilter>().sharedMesh;
            d.AddComponent<MeshRenderer>().sharedMaterial = src.GetComponent<MeshRenderer>().sharedMaterial;
        }
        return p;
    }
}
