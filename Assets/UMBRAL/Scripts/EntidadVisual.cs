using UnityEngine;

/// <summary>
/// Da vida al modelo del espectro: flota, se inclina hacia delante, mueve los brazos, abre la mandíbula y agita los jirones del manto.
/// Brazos, mandíbula y jirones son objetos hijos del FBX con el origen en su articulación.
/// </summary>
public class EntidadVisual : MonoBehaviour
{
    public Transform modelo;
    public Transform brazoIzq;
    public Transform brazoDer;
    public Transform mandibula;
    public Transform[] jirones;
    public Light luzOjos;
    public Light luzSusto;              // luz desde abajo, solo durante el susto final
    public float aperturaMandibula = 1f; // 1 o -1 según hacia dónde gire el eje al importar

    /// <summary>Durante el susto final abre los brazos y la mandíbula, tiembla y se agita más rápido.</summary>
    [HideInInspector] public bool atacando;

    Vector3 posBase;
    Quaternion rotIzq, rotDer, rotMandibula;
    Quaternion[] rotJirones;
    float luzBase, fase, boca;

    void Awake()
    {
        posBase = modelo.localPosition;
        rotIzq = brazoIzq.localRotation;
        rotDer = brazoDer.localRotation;
        if (mandibula != null) rotMandibula = mandibula.localRotation;
        rotJirones = new Quaternion[jirones.Length];
        for (int i = 0; i < jirones.Length; i++) rotJirones[i] = jirones[i].localRotation;
        luzBase = luzOjos != null ? luzOjos.intensity : 0f;
        fase = Random.value * 10f;
    }

    void OnEnable()
    {
        atacando = false;
        boca = 0f;
        if (luzSusto != null) luzSusto.enabled = false;
    }

    void Update()
    {
        float t = Time.unscaledTime + fase;
        float k = atacando ? 3.2f : 1f;

        Vector3 temblor = atacando ? new Vector3(Mathf.PerlinNoise(t * 31f, 0f) - 0.5f, Mathf.PerlinNoise(0f, t * 29f) - 0.5f, 0f) * 0.05f : Vector3.zero;
        modelo.localPosition = posBase + temblor + new Vector3(Mathf.Sin(t * 1.3f) * 0.04f, Mathf.Sin(t * 2.1f) * 0.07f, 0f);
        float cabeceo = atacando ? 14f : 8f;
        float ladeo = atacando ? Mathf.Sin(t * 23f) * 3f : Mathf.Sin(t * 1.1f) * 5f;   // en el susto la cabeza vibra en vez de mecerse
        modelo.localRotation = Quaternion.Euler(cabeceo + Mathf.Sin(t * 1.7f * k) * 3f, atacando ? 0f : Mathf.Sin(t * 0.9f) * 6f, ladeo);

        float abrir = atacando ? 40f : 0f;
        float subir = atacando ? -28f : 0f;
        brazoIzq.localRotation = rotIzq * Quaternion.Euler(subir + Mathf.Sin(t * 2.3f * k) * 9f, -abrir + Mathf.Sin(t * 1.9f * k) * 7f, 0f);
        brazoDer.localRotation = rotDer * Quaternion.Euler(subir + Mathf.Sin(t * 2.0f * k + 1.3f) * 9f, abrir + Mathf.Sin(t * 2.2f * k + 0.7f) * 7f, 0f);

        if (mandibula != null)
        {
            // En reposo cuelga y castañetea un poco; en el susto se desencaja de golpe
            float objetivo = atacando ? 58f + Mathf.Sin(t * 37f) * 5f : 7f + Mathf.Max(0f, Mathf.Sin(t * 5.3f)) * 6f;
            boca = Mathf.Lerp(boca, objetivo, Time.unscaledDeltaTime * (atacando ? 18f : 6f));
            mandibula.localRotation = rotMandibula * Quaternion.Euler(boca * aperturaMandibula, 0f, 0f);
        }

        for (int i = 0; i < jirones.Length; i++)
            jirones[i].localRotation = rotJirones[i] * Quaternion.Euler(-14f + Mathf.Sin(t * 2.6f + i * 1.7f) * 16f, 0f, Mathf.Sin(t * 2.1f + i) * 12f);

        if (luzOjos != null) luzOjos.intensity = luzBase * (0.75f + 0.35f * Mathf.PerlinNoise(t * 9f, 0.3f));
        if (luzSusto != null) luzSusto.enabled = atacando && Mathf.PerlinNoise(t * 26f, 2f) > 0.28f;   // parpadea
    }
}
