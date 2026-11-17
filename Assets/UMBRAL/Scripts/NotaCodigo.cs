using UnityEngine;

/// <summary>
/// Nota de papel con el código de un teclado. Puede mostrar el código completo o un solo dígito
/// (cuando el código está repartido en varias notas). Los números se ven en el papel y también se leen con E.
/// </summary>
public class NotaCodigo : Interactuable
{
    public MeshFilter[] huecos;      // tres posiciones para los dígitos sobre el papel
    public Mesh[] mallas;            // mallas de los dígitos 0 a 9 (PRP_Digitos)
    public int posicion = -1;        // -1: muestra el código completo; 0, 1 o 2: solo ese dígito

    string codigo = "000";
    static readonly string[] ORDINAL = { "primer", "segundo", "tercer" };

    public override string Mensaje { get { return "Presiona E para leer la nota"; } }

    public void Fijar(string nuevo)
    {
        codigo = nuevo;
        for (int i = 0; i < huecos.Length; i++)
        {
            bool visible = posicion < 0 || posicion == i;
            huecos[i].sharedMesh = visible ? mallas[codigo[i] - '0'] : null;
        }
    }

    public override void Interactuar(JugadorInteraccion jugador)
    {
        if (posicion < 0) HudJuego.I.MostrarPista("La nota dice: " + codigo[0] + " " + codigo[1] + " " + codigo[2], 6f);
        else HudJuego.I.MostrarPista("La nota dice: el " + ORDINAL[posicion] + " número es " + codigo[posicion], 6f);
    }
}
