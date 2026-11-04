// =============================================================================
// MODULO ........ 8 · Mochila con NetworkList (S07)
// PREFAB ........ "Player": agrega MochilaDemo
// DOC OFICIAL ... Netcode for GameObjects · NetworkVariables / custom NetworkVariables
// =============================================================================
using System;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

// Una casilla: QUE hay y CUANTOS. Es un dato, no un objeto de la escena.
public struct Item : INetworkSerializable, IEquatable<Item>
{
    public int type;
    public int amount;

    // Sin esto COMPILA y REVIENTA al replicarse.
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref type);
        serializer.SerializeValue(ref amount);
    }

    // Sin esto NO COMPILA (CS0315): la lista compara casillas para saber si algo cambio.
    public bool Equals(Item other) => type == other.type && amount == other.amount;
}

public class MochilaDemo : NetworkBehaviour
{
    public static readonly string[] NAMES = { "Madera", "Piedra", "Pocion" };
    public const int MAX_STACK = 99;

    // La ve su dueno, la escribe el servidor.
    public NetworkList<Item> backpack;

    // Una NetworkList se crea en Awake, NO en la declaracion (reserva memoria nativa).
    private void Awake()
    {
        backpack = new NetworkList<Item>(null, NetworkVariableReadPermission.Owner,
                                               NetworkVariableWritePermission.Server);
    }

    public override void OnNetworkSpawn()
    {
        backpack.OnListChanged += OnBackpackChanged;
        // Lo que ya habia llega entero y SIN eventos: se lee aqui.
        if (IsOwner) Debug.Log("[Mochila] al llegar tiene " + backpack.Count + " casillas");
    }

    public override void OnNetworkDespawn()
    {
        backpack.OnListChanged -= OnBackpackChanged;
    }

    [Button]
    public void AddItem(int type = 0, int amount = 1)
    {
        if (!IsMine()) return;
        AddItemRpc(type, amount);
    }

    // S12: el servidor comprueba que el pedido sea posible.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void AddItemRpc(int type, int amount)
    {
        if (type < 0 || type >= NAMES.Length || amount < 1 || amount > MAX_STACK)
        { Debug.LogWarning("[Servidor] rechazado: casilla imposible (" + type + ", " + amount + ")"); return; }

        int i = Find(type);
        if (i == -1) { backpack.Add(new Item { type = type, amount = amount }); return; }   // -> Add

        Item item = backpack[i];   // backpack[i].amount++ no compila: el indice devuelve una copia
        item.amount = Mathf.Min(item.amount + amount, MAX_STACK);
        backpack[i] = item;        // -> Value
    }

    [Button]
    public void RemoveItem(int type = 0)
    {
        if (!IsMine()) return;
        RemoveItemRpc(type);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void RemoveItemRpc(int type)
    {
        int i = Find(type);
        if (i == -1) return;
        Item item = backpack[i];
        item.amount--;
        if (item.amount == 0) backpack.RemoveAt(i);   // -> RemoveAt
        else backpack[i] = item;                       // -> Value
    }

    // ReadPermission.Owner: en las demas maquinas la mochila sale vacia.
    [Button]
    public void PrintBackpack()
    {
        Debug.Log("[Mochila de " + OwnerClientId + "] " + backpack.Count + " casillas");
        foreach (Item item in backpack) Debug.Log("   " + NAMES[item.type] + " x" + item.amount);
    }

    private int Find(int type)
    {
        for (int i = 0; i < backpack.Count; i++)
            if (backpack[i].type == type) return i;
        return -1;
    }

    private void OnBackpackChanged(NetworkListEvent<Item> e)
    {
        Debug.Log("[Mochila de " + OwnerClientId + "] " + e.Type + ", casilla " + e.Index + ": "
                  + NAMES[e.Value.type] + " x" + e.Value.amount);
    }

    private bool IsMine()
    {
        if (IsSpawned && IsOwner) return true;
        Debug.Log("[Lab] usa los botones del jugador que dice (YO)");
        return false;
    }
}
