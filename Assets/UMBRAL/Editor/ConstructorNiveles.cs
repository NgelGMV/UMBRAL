using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Arma el juego con el kit modular de Blender: la escena del menú y el hotel, un solo mapa de 20 x 40 m
/// con 10 habitaciones encadenadas en zigzag (rejilla de 1 m).
/// Menú UMBRAL > 1. Preparar materiales y prefabs  /  UMBRAL > 2. Construir el juego.
/// Coordenadas del plano: X del plano = X de Unity, Y del plano = Z de Unity.
/// </summary>
public static class ConstructorNiveles
{
    const string DIR_MODELOS = "Assets/UMBRAL/Models/";
    const string DIR_MATS = "Assets/UMBRAL/Materials/";
    const string DIR_PREFABS = "Assets/UMBRAL/Prefabs/";
    const string DIR_TEX = "Assets/UMBRAL/Textures/";
    const string DIR_AUDIO = "Assets/UMBRAL/Audio/";
    const string DIR_ESCENAS = "Assets/UMBRAL/Scenes/";
    const string ESCENA_MENU = DIR_ESCENAS + "Menu.unity";
    const string ESCENA_HOTEL = DIR_ESCENAS + "Hotel.unity";

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
        puerta.sonidoDesbloqueo = Clip("desbloqueo");
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

        // --- muebles del mapa de 10 habitaciones
        r = new GameObject("PRP_Estante"); Modelo(r.name, r.transform); Caja(r, new Vector3(0f, 1f, 0f), new Vector3(1.6f, 2f, 0.4f)); Guardar(r, true);
        r = new GameObject("PRP_Mesa"); Modelo(r.name, r.transform); Caja(r, new Vector3(0f, 0.39f, 0f), new Vector3(1.6f, 0.78f, 0.8f)); Guardar(r, true);
        r = new GameObject("PRP_Caja"); Modelo(r.name, r.transform); Caja(r, new Vector3(0f, 0.4f, 0f), new Vector3(0.8f, 0.8f, 0.8f)); Guardar(r, true);

        // Palanca de puerta: el mismo modelo del interruptor, con piloto ámbar
        r = new GameObject("INT_Palanca");
        m = Modelo("INT_Interruptor", r.transform);
        Caja(r, new Vector3(0f, -0.3f, -0.8f), new Vector3(1.6f, 2.2f, 1.4f), true);
        var palanca = r.AddComponent<PalancaPuerta>();
        palanca.palanca = m.transform.Find("Palanca");
        palanca.puntoFoco = Vacio("Foco", r.transform, new Vector3(0f, 0f, -0.05f));
        palanca.sonido = Audio(r, "palanca");
        palanca.indicador = Luz(r.transform, new Vector3(0f, 0.28f, -0.14f), new Color(1f, 0.6f, 0.1f), 0.7f, 2.0f);
        palanca.indicador.gameObject.name = "LuzIndicador";
        Guardar(r);

        // Teclado numérico
        r = new GameObject("INT_Teclado");
        Modelo("INT_Teclado", r.transform);
        Caja(r, new Vector3(0f, -0.3f, -0.8f), new Vector3(1.4f, 2.2f, 1.4f), true);
        var teclado = r.AddComponent<TecladoCodigo>();
        teclado.puntoFoco = Vacio("Foco", r.transform, new Vector3(0f, 0f, -0.05f));
        teclado.sonido = Audio(r, "tecla");
        teclado.clipTecla = Clip("tecla");
        teclado.clipError = Clip("codigo_error");
        teclado.indicador = Luz(r.transform, new Vector3(0f, 0.1f, -0.12f), new Color(1f, 0.2f, 0.1f), 0.6f, 1.6f);
        teclado.indicador.gameObject.name = "LuzIndicador";
        Guardar(r);

        // Nota con el código: tres huecos para los dígitos, que se llenan al sortear el código
        r = new GameObject("INT_Nota");
        Modelo("PRP_Nota", r.transform);
        Caja(r, new Vector3(0f, -0.4f, -0.8f), new Vector3(1.4f, 2.2f, 1.4f), true);
        var nota = r.AddComponent<NotaCodigo>();
        nota.puntoFoco = Vacio("Foco", r.transform, new Vector3(0f, 0f, -0.05f));
        var modeloDigitos = AssetDatabase.LoadAssetAtPath<GameObject>(DIR_MODELOS + "PRP_Digitos.fbx").transform;
        nota.mallas = Enumerable.Range(0, 10).Select(i => modeloDigitos.Find("D" + i).GetComponent<MeshFilter>().sharedMesh).ToArray();
        nota.huecos = new MeshFilter[3];
        for (int i = 0; i < 3; i++)
        {
            var d = new GameObject("Digito_" + i);
            d.transform.SetParent(r.transform, false);
            d.transform.localPosition = new Vector3((i - 1) * 0.08f, -0.045f, -0.006f);
            nota.huecos[i] = d.AddComponent<MeshFilter>();
            d.AddComponent<MeshRenderer>().sharedMaterial = Mat("MAT_MetalOscuro");
        }
        Guardar(r);

        // Fusible suelto, con un brillo tenue para poder encontrarlo
        r = new GameObject("INT_Fusible");
        Modelo("INT_Fusible", r.transform);
        Caja(r, new Vector3(0f, 1.0f, 0f), new Vector3(1.6f, 2.0f, 1.6f), true);
        r.AddComponent<Fusible>().puntoFoco = Vacio("Foco", r.transform, new Vector3(0f, 0.03f, 0f));
        Luz(r.transform, new Vector3(0f, 0.15f, 0f), new Color(1f, 0.75f, 0.3f), 0.5f, 1.2f).gameObject.name = "Brillo";
        Guardar(r);

        // Caja de fusibles: los tres fusibles ya están dentro, ocultos hasta que el jugador los coloca
        r = new GameObject("INT_CajaFusibles");
        Modelo("INT_CajaFusibles", r.transform);
        Caja(r, new Vector3(0f, -0.4f, -0.8f), new Vector3(1.6f, 2.2f, 1.4f), true);
        var cajaF = r.AddComponent<CajaFusibles>();
        cajaF.puntoFoco = Vacio("Foco", r.transform, new Vector3(0f, 0f, -0.1f));
        cajaF.sonido = Audio(r, "palanca");
        cajaF.fusibles = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            var f = Modelo("INT_Fusible", r.transform);
            f.name = "Fusible_" + i;
            f.transform.localPosition = new Vector3((i - 1) * 0.13f + 0.026f, 0f, -0.19f);
            f.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);   // de pie dentro del portafusible
            f.SetActive(false);
            cajaF.fusibles[i] = f;
        }
        cajaF.indicador = Luz(r.transform, new Vector3(0f, 0.42f, -0.2f), new Color(1f, 0.2f, 0.1f), 0.6f, 1.6f);
        cajaF.indicador.gameObject.name = "LuzIndicador";
        Guardar(r);
    }

    // ------------------------------------------------------------------ mapa de 10 habitaciones
    // Ejes de los muros: dos columnas (oeste y este) por cinco filas. El recorrido va en zigzag:
    //   fila 4 (norte):   9 -> 10 -> salida
    //   fila 3:           8 <- 7
    //   fila 2:           5 -> 6
    //   fila 1:           4 <- 3
    //   fila 0 (sur):     1 -> 2
    static readonly float[] EJES_X = { 0.5f, 10.5f, 19.5f };
    static readonly float[] EJES_Z = { 0.5f, 8.5f, 16.5f, 24.5f, 32.5f, 39.5f };
    const float CARA = 0.235f;   // del eje de un muro a la cara del papel tapiz, para lo que va colgado

    // Lo que no es muro liso. Clave: "H" + eje Z + columna (muro horizontal) o "V" + eje X + fila (muro vertical).
    // Valor: "ranura:tipo" (V = ventana; D1 a D10 = puerta).
    static readonly Dictionary<string, string> ESPECIALES = new Dictionary<string, string>
    {
        { "V10", "1:D1" }, { "H11", "2:D2" }, { "V11", "1:D3" }, { "H20", "1:D4" }, { "V12", "1:D5" },
        { "H31", "2:D6" }, { "V13", "1:D7" }, { "H40", "1:D8" }, { "V14", "1:D9" }, { "H51", "3:D10" },
        { "H00", "1:V,3:V" }, { "H01", "0:V" }, { "V04", "1:V" }, { "V23", "1:V" },
    };

    class DatosPuerta
    {
        public string numero, mensaje; public float angulo; public Cerradura cerradura; public bool placaAlReves, forzar; public int mecanismos;
        public DatosPuerta(string numero, float angulo, bool placaAlReves, Cerradura cerradura = Cerradura.Ninguna, string mensaje = "", int mecanismos = 1, bool forzar = false)
        { this.numero = numero; this.angulo = angulo; this.placaAlReves = placaAlReves; this.cerradura = cerradura; this.mensaje = mensaje; this.mecanismos = mecanismos; this.forzar = forzar; }
    }

    // id -> número, ángulo de apertura (hacia la habitación siguiente), placa del otro lado, qué la traba
    static readonly Dictionary<string, DatosPuerta> PUERTAS = new Dictionary<string, DatosPuerta>
    {
        { "D1",  new DatosPuerta("001",  90f, true) },
        { "D2",  new DatosPuerta("002", -90f, false, Cerradura.Llave) },
        { "D3",  new DatosPuerta("003", -90f, false, Cerradura.Mecanismo, "Está trabada. Busca la palanca") },
        { "D4",  new DatosPuerta("004", -90f, false) },
        { "D5",  new DatosPuerta("005",  90f, true,  Cerradura.Mecanismo, "Tiene cerradura electrónica. Usa el teclado", 1, true) },
        { "D6",  new DatosPuerta("006", -90f, false) },
        { "D7",  new DatosPuerta("007", -90f, false, Cerradura.Mecanismo, "No tiene corriente. Revisa la caja de fusibles") },
        { "D8",  new DatosPuerta("008", -90f, false, Cerradura.Llave) },
        { "D9",  new DatosPuerta("009",  90f, true,  Cerradura.Mecanismo, "Está trabada. Hay dos palancas en esta habitación", 2) },
        { "D10", new DatosPuerta("010", -90f, false, Cerradura.Mecanismo, "Tiene cerradura electrónica. Usa el teclado") },
    };

    struct Tramo { public bool v; public float c, a, b; public string esp; public Tramo(bool v, float c, float a, float b, string esp) { this.v = v; this.c = c; this.a = a; this.b = b; this.esp = esp; } }

    /// <summary>Tramos de muro entre centros de pilares: todos los ejes horizontales y verticales de la rejilla de habitaciones.</summary>
    static List<Tramo> Tramos()
    {
        var t = new List<Tramo>();
        string esp;
        for (int j = 0; j < EJES_Z.Length; j++)
            for (int c = 0; c < EJES_X.Length - 1; c++)
            { ESPECIALES.TryGetValue("H" + j + c, out esp); t.Add(new Tramo(false, EJES_Z[j], EJES_X[c], EJES_X[c + 1], esp ?? "")); }
        for (int i = 0; i < EJES_X.Length; i++)
            for (int f = 0; f < EJES_Z.Length - 1; f++)
            { ESPECIALES.TryGetValue("V" + i + f, out esp); t.Add(new Tramo(true, EJES_X[i], EJES_Z[f], EJES_Z[f + 1], esp ?? "")); }
        return t;
    }

    static GameObject Poner(string prefab, Transform padre, Vector3 pos, float rotY, string nombre = null)
    {
        var g = (GameObject)PrefabUtility.InstantiatePrefab(Prefab(prefab), padre);
        g.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotY, 0f));
        if (nombre != null) g.name = nombre;
        return g;
    }

    /// <summary>Sin sol, luz ambiental muy baja, niebla y el perfil de posprocesado.</summary>
    static void Ambiente(Transform padre)
    {
        var volGo = new GameObject("Global Volume"); volGo.transform.SetParent(padre, false);
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
    }

    [MenuItem("UMBRAL/2. Construir el juego (menú y hotel)")]
    public static void ConstruirTodo()
    {
        if (Prefab("INT_Puerta") == null || Prefab("ENT_Espectro") == null || Prefab("INT_Teclado") == null) Preparar();
        ConstruirMenu();
        ConstruirHotel();
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ESCENA_MENU, true), new EditorBuildSettingsScene(ESCENA_HOTEL, true) };
        // escenas de las versiones anteriores (un nivel por piso)
        AssetDatabase.DeleteAsset(DIR_ESCENAS + "Nivel1_Hotel.unity");
        for (int n = 1; n <= 10; n++) AssetDatabase.DeleteAsset(DIR_ESCENAS + "Nivel" + n.ToString("00") + ".unity");
        EditorSceneManager.OpenScene(ESCENA_MENU);
        Debug.Log("[UMBRAL] Juego construido: Menu y Hotel.");
    }

    // ------------------------------------------------------------------ pantalla de inicio
    /// <summary>Un tramo de pasillo con el espectro en el vano del fondo. La interfaz la arma MenuInicio al iniciar.</summary>
    public static void ConstruirMenu()
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var set = new GameObject("Pasillo").transform;
        for (int k = 0; k < 8; k++)
        {
            float z = 2f * k;
            Poner("EST_MuroEstandar", set, new Vector3(-2.25f, 0f, z), -90f);
            Poner("EST_MuroEstandar", set, new Vector3(2.25f, 0f, z), -90f);
            foreach (float x in new[] { -1f, 1f })
            {
                Poner("EST_Piso", set, new Vector3(x, -0.1f, z + 1f), k % 2 == 0 ? 0f : 90f);
                Poner("EST_Techo", set, new Vector3(x, 3f, z + 1f), 0f);
            }
        }
        for (int k = 0; k < 5; k++) Poner("PRP_Alfombra", set, new Vector3(0f, 0f, 1.5f + 3f * k), 90f);
        // fondo: vano sin puerta, con el espectro esperando en la oscuridad
        Poner("EST_MuroMedio", set, new Vector3(-2f, 0f, 16.25f), 0f);
        Poner("EST_MuroVano", set, new Vector3(-1f, 0f, 16.25f), 0f);
        Poner("EST_MarcoPuerta", set, new Vector3(-0.75f, 0f, 16.25f), 0f);
        Poner("EST_MuroMedio", set, new Vector3(1f, 0f, 16.25f), 0f);
        Poner("EST_MuroEstandar", set, new Vector3(-1f, 0f, -0.25f), 0f);
        Poner("EST_MuroMedio", set, new Vector3(-2f, 0f, -0.25f), 0f);
        Poner("EST_MuroMedio", set, new Vector3(1f, 0f, -0.25f), 0f);
        Poner("INT_Closet", set, new Vector3(1.7f, 0f, 9f), 90f);
        var lamparas = new List<LamparaPared>
        {
            Poner("INT_Lampara", set, new Vector3(-2f, 1.95f, 5f), -90f).GetComponent<LamparaPared>(),
            Poner("INT_Lampara", set, new Vector3(2f, 1.95f, 11.5f), 90f).GetComponent<LamparaPared>(),
        };
        var espectro = Poner("ENT_Espectro", set, new Vector3(0f, 1.5f, 15.7f), 180f, "Espectro");

        var camGo = new GameObject("CamaraMenu");
        camGo.tag = "MainCamera";
        camGo.transform.SetPositionAndRotation(new Vector3(0.9f, 1.55f, 1.6f), Quaternion.Euler(1.5f, -3.5f, 0f));
        var cam = camGo.AddComponent<Camera>();
        cam.nearClipPlane = 0.05f; cam.farClipPlane = 60f; cam.fieldOfView = 58f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        camGo.AddComponent<AudioListener>();

        var sistemas = new GameObject("Sistemas").transform;
        var menu = new GameObject("Menu"); menu.transform.SetParent(sistemas, false);
        var mi = menu.AddComponent<MenuInicio>();
        mi.camara = camGo.transform;
        mi.lamparas = lamparas.ToArray();
        var ambiente = Audio(sistemas.gameObject, "ambiente_hotel", true, false, 0.45f, "Ambiente");
        ambiente.playOnAwake = true;
        Ambiente(sistemas);
        EditorSceneManager.SaveScene(escena, ESCENA_MENU);
    }

    // ------------------------------------------------------------------ hotel
    public static void ConstruirHotel()
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var conteo = new SortedDictionary<string, int>();
        System.Action<string> contar = k => { int c; conteo.TryGetValue(k, out c); conteo[k] = c + 1; };

        var nivel = new GameObject("Hotel").transform;
        var tEstructura = Vacio("Estructura", nivel, Vector3.zero);
        var tMuros = Vacio("Muros", tEstructura, Vector3.zero);
        var tPilares = Vacio("Pilares", tEstructura, Vector3.zero);
        var tPisos = Vacio("Pisos", tEstructura, Vector3.zero);
        var tTechos = Vacio("Techos", tEstructura, Vector3.zero);
        var tPuertas = Vacio("Puertas", nivel, Vector3.zero);
        var cuartos = new Transform[11];
        string[] nombres = { "", "Recepcion", "Dormitorio", "Salon", "CuartoDeServicio", "Oficina", "Guardarropa", "CuartoElectrico", "DormitorioDoble", "Biblioteca", "SalaFinal" };
        for (int i = 1; i <= 10; i++) cuartos[i] = Vacio("Habitacion" + i.ToString("00") + "_" + nombres[i], nivel, Vector3.zero);

        // --- muros, marcos y puertas
        var puertas = new Dictionary<string, PuertaInteractiva>();
        foreach (var t in Tramos())
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
                    Poner("EST_MarcoPuerta", tMuros, en(p) + dir * 0.25f, rot, "EST_MarcoPuerta_" + PUERTAS[tipo].numero); contar("EST_MarcoPuerta");
                    puertas[tipo] = PonerPuerta(tPuertas, en(p) + dir * 0.4f, rot, PUERTAS[tipo]);
                    contar("INT_Puerta"); contar("PRP_PlacaNumero");
                }
                p += 2f; k++;
            }
        }
        var enOrden = Enumerable.Range(1, 10).Select(i => puertas["D" + i]).ToArray();

        // --- pilares en cada cruce de ejes
        foreach (float x in EJES_X)
            foreach (float z in EJES_Z) { Poner("EST_Pilar", tPilares, new Vector3(x, 0f, z), 0f); contar("EST_Pilar"); }

        // --- piso y techo (módulos de 2 x 2 m)
        for (int i = 0; i < 10; i++)
            for (int j = 0; j < 20; j++)
            {
                Poner("EST_Piso", tPisos, new Vector3(1f + 2f * i, -0.1f, 1f + 2f * j), (i + j) % 2 == 0 ? 0f : 90f); contar("EST_Piso");
                Poner("EST_Techo", tTechos, new Vector3(1f + 2f * i, 3f, 1f + 2f * j), 0f); contar("EST_Techo");
            }
        Caja(tPisos.gameObject, new Vector3(10f, -0.05f, 20.5f), new Vector3(20f, 0.1f, 41f));
        Caja(tTechos.gameObject, new Vector3(10f, 3.05f, 20f), new Vector3(20f, 0.1f, 40f));
        var barrera = Vacio("BarreraSalida", tEstructura, Vector3.zero).gameObject;   // la salida (puerta 010) queda en x = 18
        Caja(barrera, new Vector3(18f, 1.5f, 40.7f), new Vector3(6f, 3f, 0.2f));
        Caja(barrera, new Vector3(16.6f, 1.5f, 40.2f), new Vector3(0.2f, 3f, 1f));
        Caja(barrera, new Vector3(19.4f, 1.5f, 40.2f), new Vector3(0.2f, 3f, 1f));

        // --- utilidades para amueblar. Giros de lo que va contra un muro: mira al sur = 0, al norte = 180, al este = -90, al oeste = 90
        int cuarto = 1;
        System.Func<string, float, float, float, float, GameObject> mueble = (prefab, x, z, rot, y) =>
        { contar(prefab); return Poner(prefab, cuartos[cuarto], new Vector3(x, y, z), rot); };
        System.Func<float, float, float, bool, LamparaPared> lampara = (x, z, rot, encendida) =>
        {
            var lp = mueble("INT_Lampara", x, z, rot, 1.95f).GetComponent<LamparaPared>();
            if (!encendida) { lp.encendidaAlInicio = false; lp.gameObject.name = "INT_Lampara_Apagada"; }
            return lp;
        };
        System.Action<float, float, float, int> mesaNoche = (x, z, rot, zona) =>
        {
            var m = mueble("INT_MesaNoche", x, z, rot, 0f).GetComponent<MesaDeNoche>();
            m.zona = zona; m.gameObject.name = "INT_MesaNoche_Puerta" + zona.ToString("000");
        };
        System.Func<string, GameObject[], int, SeleccionAleatoria> alAzar = (nombre, lista, activos) =>
        {
            var sel = new GameObject(nombre).AddComponent<SeleccionAleatoria>();
            sel.transform.SetParent(cuartos[cuarto], false);
            sel.candidatos = lista; sel.activos = activos;
            return sel;
        };
        System.Func<float, float, float, PuertaInteractiva, GameObject> palancaEn = (x, z, rot, puerta) =>
        {
            var g = mueble("INT_Palanca", x, z, rot, 1.3f);
            g.GetComponent<PalancaPuerta>().puerta = puerta;
            return g;
        };
        System.Func<float, float, float, int, NotaCodigo> notaEn = (x, z, rot, posicion) =>
        {
            var n = mueble("INT_Nota", x, z, rot, 1.5f).GetComponent<NotaCodigo>();
            n.posicion = posicion;
            return n;
        };
        float O = EJES_X[0] + CARA, MO = EJES_X[1] - CARA, ME = EJES_X[1] + CARA, E = EJES_X[2] - CARA;   // caras de los muros verticales
        System.Func<int, float> S = j => EJES_Z[j] + CARA;   // cara norte del muro del eje j (pared sur de la habitación)
        System.Func<int, float> N = j => EJES_Z[j] - CARA;   // cara sur del muro del eje j (pared norte de la habitación)

        // ===== 1. Recepción: zona segura. Puerta 001 sin seguro.
        cuarto = 1;
        mueble("PRP_Mostrador", 5f, 7.2f, 0f, 0f);
        mueble("PRP_Sofa", 2.2f, 1.2f, 180f, 0f);
        mueble("PRP_Estante", 0.95f, 6.2f, -90f, 0f);
        mueble("PRP_Candelabro", 5.5f, 4f, 0f, 3f);
        mueble("PRP_Alfombra", 7f, 4f, 0f, 0f);
        lampara(0.75f, 2.4f, -90f, true); lampara(8.6f, 8.25f, 0f, true); lampara(6f, 0.75f, 180f, true);

        // ===== 2. Dormitorio: la llave de la puerta 002 está en uno de sus tres cajones.
        cuarto = 2;
        mueble("PRP_Cama", 18.2f, 2.2f, 90f, 0f); mueble("PRP_Cama", 18.2f, 6.0f, 90f, 0f);
        mesaNoche(19f, 3.6f, 90f, 2); mesaNoche(19f, 4.6f, 90f, 2); mesaNoche(13.6f, 1.0f, 180f, 2);
        mueble("INT_Closet", 12.2f, 7.95f, 0f, 0f); mueble("INT_Closet", 15.2f, 1.05f, 180f, 0f);
        mueble("PRP_Alfombra", 15f, 4.5f, 0f, 0f);
        lampara(17.5f, 0.75f, 180f, true); lampara(13.8f, 8.25f, 0f, true);

        // ===== 3. Salón: una palanca destraba la puerta 003. Aparece en uno de tres sitios.
        cuarto = 3;
        mueble("PRP_Sofa", 18.8f, 12.5f, 90f, 0f); mueble("PRP_Sofa", 15f, 15.8f, 0f, 0f);
        mueble("PRP_Mesa", 15.5f, 12.8f, 0f, 0f);
        mueble("PRP_Estante", 12.5f, 16.05f, 0f, 0f);
        mueble("INT_Closet", 11.05f, 9.8f, -90f, 0f); mueble("INT_Closet", 18.95f, 9.7f, 90f, 0f);
        mueble("PRP_Alfombra", 14.5f, 11f, 0f, 0f);
        lampara(10.75f, 14.5f, -90f, true); lampara(18.2f, 8.75f, 180f, true);
        alAzar("Palancas_003", new[] { palancaEn(13.2f, S(1), 180f, puertas["D3"]), palancaEn(E, 14.7f, 90f, puertas["D3"]), palancaEn(17.6f, N(2), 0f, puertas["D3"]) }, 1);

        // ===== 4. Cuarto de servicio: a oscuras hasta encontrar el interruptor. Puerta 004 sin seguro.
        cuarto = 4;
        var lamparas4 = new[] { lampara(0.75f, 9.6f, -90f, false), lampara(4f, 8.75f, 180f, false), lampara(6.5f, 16.25f, 0f, false) };
        foreach (var lp in lamparas4) { lp.luz.intensity = 3.4f; lp.luz.range = 10f; }
        var interruptor = mueble("INT_Interruptor", 7.2f, S(1), 180f, 1.3f).GetComponent<InterruptorLuz>();
        interruptor.lamparas = lamparas4;
        mueble("PRP_Caja", 2.0f, 10.0f, 0f, 0f); mueble("PRP_Caja", 2.86f, 10.0f, 8f, 0f); mueble("PRP_Caja", 2.4f, 10.0f, 22f, 0.8f);
        mueble("PRP_Caja", 6f, 13f, 0f, 0f); mueble("PRP_Caja", 6.9f, 13.2f, 15f, 0f); mueble("PRP_Caja", 9.4f, 15.6f, 0f, 0f);
        mueble("PRP_Estante", 0.95f, 12.5f, -90f, 0f);
        mueble("INT_Closet", 8.2f, 15.95f, 0f, 0f); mueble("INT_Closet", 1.05f, 14.8f, -90f, 0f);

        // ===== 5. Oficina: teclado con código de 3 dígitos. La nota con el código aparece en uno de tres sitios.
        cuarto = 5;
        mueble("PRP_Mesa", 3f, 21.5f, 0f, 0f); mueble("PRP_Mesa", 7f, 18.4f, 0f, 0f);
        mueble("PRP_Estante", 2.2f, 24.05f, 0f, 0f); mueble("PRP_Estante", 3.9f, 24.05f, 0f, 0f);
        mueble("INT_Closet", 1.05f, 22.8f, -90f, 0f); mueble("INT_Closet", 9.95f, 17.6f, 90f, 0f);
        mueble("PRP_Alfombra", 5.5f, 20.6f, 0f, 0f);
        lampara(0.75f, 20.8f, -90f, true); lampara(6.2f, 16.75f, 180f, true);
        var notas5 = new[] { notaEn(O, 19.2f, -90f, -1), notaEn(7.6f, N(3), 0f, -1), notaEn(8.6f, S(2), 180f, -1) };
        alAzar("Notas_005", notas5.Select(n => n.gameObject).ToArray(), 1);
        var teclado5 = mueble("INT_Teclado", MO, 21.2f, 90f, 1.45f).GetComponent<TecladoCodigo>();
        teclado5.puerta = puertas["D5"]; teclado5.notas = notas5;

        // ===== 6. Guardarropa: seis clósets. Al abrir la puerta 005 el espectro aparece siempre. Puerta 006 sin seguro.
        cuarto = 6;
        foreach (float x in new[] { 12.2f, 13.6f, 15.0f }) mueble("INT_Closet", x, 17.05f, 180f, 0f);
        foreach (float z in new[] { 18.8f, 20.2f, 21.6f }) mueble("INT_Closet", 18.95f, z, 90f, 0f);
        mueble("PRP_Sofa", 12.4f, 23.8f, 0f, 0f);
        mueble("PRP_Estante", 10.95f, 22.6f, -90f, 0f);
        mueble("PRP_Alfombra", 15f, 20.6f, 0f, 0f);
        lampara(13.9f, 24.25f, 0f, true); lampara(17.6f, 16.75f, 180f, true);

        // ===== 7. Cuarto eléctrico: la puerta 007 no tiene corriente. Hay tres fusibles repartidos en seis sitios posibles.
        cuarto = 7;
        mueble("PRP_Mesa", 16.2f, 30.9f, 0f, 0f);
        mueble("PRP_Caja", 12.0f, 31.6f, 0f, 0f); mueble("PRP_Caja", 12.9f, 31.5f, 10f, 0f); mueble("PRP_Caja", 14.4f, 25.3f, 0f, 0f); mueble("PRP_Caja", 18.6f, 25.4f, 12f, 0f);
        mueble("PRP_Estante", 14.2f, 32.05f, 0f, 0f);
        mueble("INT_Closet", 18.95f, 30.4f, 90f, 0f); mueble("INT_Closet", 12.6f, 25.05f, 180f, 0f);
        lampara(19.25f, 26.4f, 90f, true); lampara(17.8f, 32.25f, 0f, true);
        var sitios = new[] { new Vector3(16.2f, 0.79f, 30.9f), new Vector3(12.0f, 0.81f, 31.6f), new Vector3(18.9f, 0.02f, 31.8f), new Vector3(18.6f, 0.81f, 25.4f), new Vector3(11.3f, 0.02f, 26.6f), new Vector3(17.4f, 0.02f, 25.2f) };
        alAzar("Fusibles_007", sitios.Select((s, i) => mueble("INT_Fusible", s.x, s.z, 35f * i, s.y)).ToArray(), 3);
        mueble("INT_CajaFusibles", ME, 29.5f, -90f, 1.4f).GetComponent<CajaFusibles>().puerta = puertas["D7"];

        // ===== 8. Dormitorio doble: la llave de la puerta 008 está en uno de sus cuatro cajones.
        cuarto = 8;
        mueble("PRP_Cama", 1.8f, 26.5f, -90f, 0f); mueble("PRP_Cama", 1.8f, 30.3f, -90f, 0f);
        mesaNoche(1f, 27.9f, -90f, 8); mesaNoche(1f, 28.9f, -90f, 8); mesaNoche(8.6f, 25.0f, 180f, 8); mesaNoche(10f, 30.9f, 90f, 8);
        mueble("PRP_Sofa", 9.8f, 26.2f, 90f, 0f);
        mueble("INT_Closet", 6.6f, 31.95f, 0f, 0f); mueble("INT_Closet", 5.8f, 25.05f, 180f, 0f);
        mueble("PRP_Alfombra", 5.2f, 28.4f, 0f, 0f);
        lampara(8.6f, 32.25f, 0f, true); lampara(3.4f, 24.75f, 180f, true);

        // ===== 9. Biblioteca: casi a oscuras. La puerta 009 necesita dos palancas, que aparecen en dos de cuatro sitios.
        cuarto = 9;
        mueble("PRP_Estante", 2.6f, 35.4f, 90f, 0f); mueble("PRP_Estante", 6.2f, 34.6f, 90f, 0f); mueble("PRP_Estante", 6.2f, 37.6f, 90f, 0f);
        mueble("PRP_Estante", 2.2f, 39.05f, 0f, 0f); mueble("PRP_Estante", 3.9f, 39.05f, 0f, 0f);
        mueble("PRP_Mesa", 8.4f, 33.7f, 0f, 0f);
        mueble("INT_Closet", 1.05f, 38.0f, -90f, 0f); mueble("INT_Closet", 9.95f, 38.4f, 90f, 0f);
        lampara(10.25f, 34.3f, 90f, true); lampara(7f, 32.75f, 180f, false); lampara(7.2f, 39.25f, 0f, false);
        alAzar("Palancas_009", new[] { palancaEn(O, 34.2f, -90f, puertas["D9"]), palancaEn(5.6f, N(5), 0f, puertas["D9"]), palancaEn(8.7f, N(5), 0f, puertas["D9"]), palancaEn(5.4f, S(4), 180f, puertas["D9"]) }, 2);

        // ===== 10. Sala final: teclado con el código repartido en tres notas, un dígito en cada una.
        cuarto = 10;
        mueble("PRP_Mostrador", 14.5f, 34.9f, 0f, 0f);
        mueble("PRP_Sofa", 13f, 38.8f, 0f, 0f);
        mueble("PRP_Estante", 17.4f, 32.95f, 180f, 0f);
        mueble("PRP_Candelabro", 15f, 36.6f, 0f, 3f);
        mueble("INT_Closet", 12.3f, 33.05f, 180f, 0f); mueble("INT_Closet", 18.95f, 37.2f, 90f, 0f);
        mueble("PRP_Alfombra", 15f, 37f, 0f, 0f);
        lampara(13.6f, 32.75f, 180f, true); lampara(14.6f, 39.25f, 0f, true);
        var teclado10 = mueble("INT_Teclado", 16.7f, N(5), 0f, 1.45f).GetComponent<TecladoCodigo>();
        teclado10.puerta = puertas["D10"];
        teclado10.notas = new[] { notaEn(ME, 34.0f, -90f, 0), notaEn(15.4f, S(4), 180f, 1), notaEn(E, 35.2f, 90f, 2) };

        // --- jugador (Spawn Point: recepción, mirando a la puerta 001)
        var jugador = new GameObject("Jugador");
        jugador.tag = "Player";
        jugador.transform.SetPositionAndRotation(new Vector3(2.5f, 0.05f, 4f), Quaternion.Euler(0f, 90f, 0f));
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
        estadoJuego.puertas = enOrden;
        // punto de entrada de cada habitación (para reaparecer tras ser atrapado) y hacia dónde mira el jugador
        estadoJuego.reaparicion = new[]
        {
            new Vector3(2.5f, 0.05f, 4f), new Vector3(11.8f, 0.05f, 4f), new Vector3(16f, 0.05f, 9.8f), new Vector3(9.2f, 0.05f, 12f), new Vector3(4f, 0.05f, 17.8f),
            new Vector3(11.8f, 0.05f, 20f), new Vector3(16f, 0.05f, 25.8f), new Vector3(9.2f, 0.05f, 28f), new Vector3(4f, 0.05f, 33.8f), new Vector3(11.8f, 0.05f, 36f),
        };
        estadoJuego.giroReaparicion = new[] { 90f, 90f, 0f, 270f, 0f, 90f, 0f, 270f, 0f, 90f };
        estadoJuego.susto = Audio(estado, "susto", false, false, 0.9f, "Susto");
        estadoJuego.efectos = Audio(estado, "llave", false, false, 0.8f, "Efectos");
        estadoJuego.clipFusible = Clip("llave");
        var ambiente = Audio(sistemas.gameObject, "ambiente_hotel", true, false, 0.35f, "Ambiente");
        ambiente.playOnAwake = true;
        var hud = new GameObject("HUD"); hud.transform.SetParent(sistemas, false); hud.AddComponent<HudJuego>();

        var meta = new GameObject("MetaNivel"); meta.transform.SetParent(sistemas, false);
        meta.transform.position = new Vector3(18f, 1.25f, 40.05f);
        var zona = meta.AddComponent<BoxCollider>(); zona.size = new Vector3(1.6f, 2.5f, 0.6f); zona.isTrigger = true;
        meta.AddComponent<MetaNivel>().jugador = jugador.transform;

        // --- espectro: recorre las habitaciones abiertas siguiendo el mismo zigzag que el jugador
        var entidad = Poner("ENT_Espectro", sistemas, Vector3.zero, 0f, "Entidad");
        var dirGo = new GameObject("DirectorEntidad"); dirGo.transform.SetParent(sistemas, false);
        var director = dirGo.AddComponent<DirectorEntidad>();
        director.jugador = ctrl; director.camaraJugador = camGo.transform;
        director.entidad = entidad; director.visual = entidad.GetComponent<EntidadVisual>();
        director.puertas = enOrden;
        float[,] pts =
        {
            { 11.3f, 4f }, { 16f, 4f }, { 16f, 8.5f }, { 16f, 12f }, { 10.5f, 12f }, { 4f, 12f }, { 4f, 16.5f }, { 4f, 20f }, { 10.5f, 20f }, { 16f, 20f },
            { 16f, 24.5f }, { 16f, 28f }, { 10.5f, 28f }, { 4f, 28f }, { 4f, 32.5f }, { 4f, 36f }, { 10.5f, 36f }, { 18f, 36f }, { 18f, 38.6f },
        };
        director.ruta = Enumerable.Range(0, pts.GetLength(0)).Select(i => new Vector3(pts[i, 0], 1.5f, pts[i, 1])).ToArray();
        director.entradaDeCuarto = new[] { 0, 2, 4, 6, 8, 10, 12, 14, 16 };   // habitaciones 2 a 10
        director.finDeCuarto = new[] { 1, 3, 5, 7, 9, 11, 13, 15, 18 };
        director.rugido = Audio(dirGo, "entidad_rugido", true, false, 0.9f, "Rugido");
        director.zumbido = Audio(dirGo, "luz_zumbido", true, false, 0.45f, "Zumbido");
        entidad.SetActive(false);

        Ambiente(sistemas);
        EditorSceneManager.SaveScene(escena, ESCENA_HOTEL);
        Debug.Log("[UMBRAL] Hotel construido. Conteo de assets: " + string.Join(", ", conteo.Select(kv => kv.Key + "=" + kv.Value)));
    }

    static PuertaInteractiva PonerPuerta(Transform padre, Vector3 pos, float rotY, DatosPuerta d)
    {
        var g = Poner("INT_Puerta", padre, pos, rotY, "INT_Puerta_" + d.numero);
        var p = g.GetComponent<PuertaInteractiva>();
        p.numero = d.numero; p.anguloApertura = d.angulo; p.cerradura = d.cerradura;
        p.mensajeBloqueada = d.mensaje; p.mecanismosNecesarios = d.mecanismos; p.forzarEntidad = d.forzar;
        var placa = p.hoja.Find("PRP_PlacaNumero");
        if (d.placaAlReves)
        {
            placa.localPosition = new Vector3(0.6f, 1.75f, 0.05f);
            placa.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }
        var digitos = AssetDatabase.LoadAssetAtPath<GameObject>(DIR_MODELOS + "PRP_Digitos.fbx").transform;
        for (int i = 0; i < d.numero.Length; i++)
        {
            var src = digitos.Find("D" + d.numero[i]);
            var dg = new GameObject("Digito_" + d.numero[i]);
            dg.transform.SetParent(placa, false);
            dg.transform.localPosition = new Vector3((i - (d.numero.Length - 1) * 0.5f) * 0.075f, -0.04f, -0.02f);
            dg.AddComponent<MeshFilter>().sharedMesh = src.GetComponent<MeshFilter>().sharedMesh;
            dg.AddComponent<MeshRenderer>().sharedMaterial = src.GetComponent<MeshRenderer>().sharedMaterial;
        }
        return p;
    }
}
