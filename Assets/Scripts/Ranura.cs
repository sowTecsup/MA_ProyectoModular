// =============================================================================
// ORDEN ......... 1 de 5. No usa ningun otro script: va primero.
// GAMEOBJECT .... ninguno: son dos structs, no componentes
// =============================================================================
using System;
using Unity.Netcode;

// S07 · Una casilla de la mochila: QUE hay y CUANTOS. Es un dato, no un objeto:
// el cubo guardado ya no existe en la red; lo que queda de el es esta fila.
public struct Ranura : INetworkSerializable, IEquatable<Ranura>
{
    public const int CUBO = 0;
    public const int TRONCO = 1;

    public int tipo;
    public int cantidad;

    // Sin esto COMPILA y REVIENTA al replicarse.
    public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
    {
        s.SerializeValue(ref tipo);
        s.SerializeValue(ref cantidad);
    }

    // Sin esto NO COMPILA (CS0315): la lista compara casillas para saber si algo cambio.
    public bool Equals(Ranura otra) => tipo == otra.tipo && cantidad == otra.cantidad;

    public string Nombre() => tipo == CUBO ? "Cubo" : "Tronco";
}

// S10 · La verdad del servidor sobre los pasos, con la ultima secuencia que aplico.
public struct EstadoPasos : INetworkSerializable
{
    public int pasos;
    public int secuencia;

    public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
    {
        s.SerializeValue(ref pasos);
        s.SerializeValue(ref secuencia);
    }
}
