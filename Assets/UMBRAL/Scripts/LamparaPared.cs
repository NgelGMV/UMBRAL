using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Lámpara de pared. Parpadea cuando se acerca una entidad y se apaga a su paso.</summary>
public class LamparaPared : MonoBehaviour
{
    public Light luz;
    public Renderer pantalla;
    public int indiceMaterial;
    public bool encendidaAlInicio = true;

    public static readonly List<LamparaPared> Todas = new List<LamparaPared>();

    public bool Encendida { get; private set; }
    float intensidadBase;
    Color emisionBase = Color.black;
    MaterialPropertyBlock bloque;
    Coroutine rutina;
    static readonly int idEmision = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        intensidadBase = luz.intensity;
        bloque = new MaterialPropertyBlock();
        if (pantalla != null && indiceMaterial < pantalla.sharedMaterials.Length)
            emisionBase = pantalla.sharedMaterials[indiceMaterial].GetColor(idEmision);
        Encendida = encendidaAlInicio;
        Aplicar(Encendida ? 1f : 0f);
    }

    void OnEnable() { Todas.Add(this); }
    void OnDisable() { Todas.Remove(this); }

    void Aplicar(float k)
    {
        luz.intensity = intensidadBase * k;
        luz.enabled = k > 0.01f;
        if (pantalla != null)
        {
            bloque.SetColor(idEmision, emisionBase * k);
            pantalla.SetPropertyBlock(bloque, indiceMaterial);
        }
    }

    public void Encender()
    {
        Encendida = true;
        if (rutina == null) Aplicar(1f);
    }

    public void Parpadear(float segundos)
    {
        if (!Encendida) return;
        if (rutina != null) StopCoroutine(rutina);
        rutina = StartCoroutine(RutinaParpadeo(segundos));
    }

    public void ApagarUnRato(float segundos)
    {
        if (!Encendida) return;
        if (rutina != null) StopCoroutine(rutina);
        rutina = StartCoroutine(RutinaApagado(segundos));
    }

    IEnumerator RutinaParpadeo(float segundos)
    {
        float fin = Time.time + segundos;
        while (Time.time < fin)
        {
            Aplicar(Random.value < 0.45f ? Random.Range(0f, 0.25f) : Random.Range(0.7f, 1.1f));
            yield return new WaitForSeconds(Random.Range(0.04f, 0.16f));
        }
        rutina = null;
        Aplicar(Encendida ? 1f : 0f);
    }

    IEnumerator RutinaApagado(float segundos)
    {
        Aplicar(0f);
        yield return new WaitForSeconds(segundos);
        rutina = null;
        Aplicar(Encendida ? 1f : 0f);
    }
}
