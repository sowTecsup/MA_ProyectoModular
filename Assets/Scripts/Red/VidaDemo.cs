// =============================================================================
// MODULO ........ 6 · NetworkVariable: estado (S03 · S04)
// PREFAB ........ "Player": agrega VidaDemo
// DOC OFICIAL ... Netcode for GameObjects · NetworkVariables (el ejemplo de la puerta)
// =============================================================================
using Sirenix.OdinInspector;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// El golpe es un EVENTO (RPC); la vida que baja es ESTADO (NetworkVariable).
public class VidaDemo : NetworkBehaviour
{
    public int damagePerHit = 10;   // lo usa el servidor: el cliente solo dice "quiero golpear"

    // Por defecto la escribe solo el servidor y la leen todos.
    public NetworkVariable<int> health = new NetworkVariable<int>(100);

    // Esta la escribe su dueno.
    public NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>(
        "", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        // Aqui el valor inicial YA llego, tambien para el que entra tarde.
        Debug.Log("[Vida] jugador " + OwnerClientId + " llega con " + health.Value + " de vida");
        health.OnValueChanged += OnHealthChanged;
        playerName.OnValueChanged += OnNameChanged;
    }

    public override void OnNetworkDespawn()
    {
        health.OnValueChanged -= OnHealthChanged;
        playerName.OnValueChanged -= OnNameChanged;
    }

    [Button]
    public void TakeHit()
    {
        if (!IsMine()) return;
        HitRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void HitRpc()
    {
        health.Value = health.Value > damagePerHit ? health.Value - damagePerHit : 100;   // a cero, renace
    }

    [Button]
    public void ChangeName(string newName = "Ana")
    {
        if (!IsMine()) return;
        playerName.Value = newName;   // permitido: WritePermission.Owner
    }

    // En un cliente NGO lo rechaza con un error de permisos. En el host SI deja: es el servidor.
    [Button]
    public void WriteHealthFromHere()
    {
        if (!IsMine()) return;
        health.Value = 999;
    }

    private void OnHealthChanged(int previous, int current)
    {
        Debug.Log("[Vida] jugador " + OwnerClientId + ": " + previous + " -> " + current);
    }

    private void OnNameChanged(FixedString32Bytes previous, FixedString32Bytes current)
    {
        Debug.Log("[Vida] el jugador " + OwnerClientId + " ahora se llama " + current);
    }

    private bool IsMine()
    {
        if (IsSpawned && IsOwner) return true;
        Debug.Log("[Lab] usa los botones del jugador que dice (YO)");
        return false;
    }
}
