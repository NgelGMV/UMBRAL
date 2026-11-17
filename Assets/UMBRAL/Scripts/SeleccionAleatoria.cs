using UnityEngine;

/// <summary>
/// Deja activos solo algunos objetos de una lista, elegidos al azar al cargar la escena.
/// Se usa para que la palanca, la nota del código y los fusibles no estén siempre en el mismo sitio.
/// </summary>
[DefaultExecutionOrder(-200)]
public class SeleccionAleatoria : MonoBehaviour
{
    public GameObject[] candidatos;
    public int activos = 1;

    void Awake()
    {
        var orden = new int[candidatos.Length];
        for (int i = 0; i < orden.Length; i++) orden[i] = i;
        for (int i = orden.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = orden[i]; orden[i] = orden[j]; orden[j] = tmp;
        }
        for (int i = 0; i < orden.Length; i++) candidatos[orden[i]].SetActive(i < activos);
    }
}
