// =============================================================================
// MODULO ........ 9 · Prediccion: anticipacion del cliente (S10)
// PREFAB ........ "Player": agrega AnticipacionDemo
// PARA NOTARLO .. Window > Multiplayer > Network Simulator, 250 ms, en el cliente
// DOC OFICIAL ... Netcode for GameObjects · Client anticipation
// =============================================================================
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

// Lo que VES (Value) y lo que dice el servidor (AuthoritativeValue) van por separado.
// El cliente anticipa al instante; cuando llega la verdad del servidor, manda la verdad.
public class AnticipacionDemo : NetworkBehaviour
{
    public const int MAX_AMMO = 10;
    public AnticipatedNetworkVariable<int> ammo = new AnticipatedNetworkVariable<int>(MAX_AMMO);

    public override void OnNetworkSpawn()
    {
        ammo.OnAuthoritativeValueChanged += OnServerAmmo;
    }

    public override void OnNetworkDespawn()
    {
        ammo.OnAuthoritativeValueChanged -= OnServerAmmo;
    }

    // Sin anticipar: el numero cambia cuando vuelve el servidor (un RTT despues).
    [Button]
    public void ShootWaitingForServer()
    {
        if (!IsMine() || ammo.Value <= 0) return;
        ShootRpc();
    }

    // Anticipando: el numero cambia YA, y el servidor lo confirma o lo corrige.
    [Button]
    public void ShootAnticipated()
    {
        if (!IsMine() || ammo.Value <= 0) return;
        ammo.Anticipate(ammo.Value - 1);
        Debug.Log("[Municion] en pantalla ya: " + ammo.Value);
        ShootRpc();
    }

    [Button]
    public void Reload()
    {
        if (!IsMine()) return;
        ammo.Anticipate(MAX_AMMO);
        ReloadRpc();
    }

    // Lo que el cliente NO puede prever: otro jugador le quita balas.
    // Pulsalo en el jugador del OTRO (no en el tuyo).
    [Button]
    public void StealAmmo(int amount = 3)
    {
        StealRpc(amount);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void ShootRpc()
    {
        if (ammo.AuthoritativeValue <= 0) { Debug.LogWarning("[Servidor] rechazado: sin balas"); return; }
        ammo.AuthoritativeValue = ammo.AuthoritativeValue - 1;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void ReloadRpc()
    {
        ammo.AuthoritativeValue = MAX_AMMO;
    }

    [Rpc(SendTo.Server)]
    private void StealRpc(int amount)
    {
        ammo.AuthoritativeValue = Mathf.Max(0, ammo.AuthoritativeValue - Mathf.Clamp(amount, 1, 3));
    }

    private void OnServerAmmo(AnticipatedNetworkVariable<int> variable, in int previous, in int current)
    {
        Debug.Log("[Municion] servidor: " + previous + " -> " + current + " · en pantalla: " + variable.Value);
    }

    // Lo llama NGO cuando una respuesta del servidor deshace lo que anticipaste.
    public override void OnReanticipate(double lastRoundTripTime)
    {
        if (!ammo.ShouldReanticipate) return;
        Debug.LogWarning("[Municion] CORRECCION: el servidor dice " + ammo.AuthoritativeValue
                         + " (RTT " + (int)(lastRoundTripTime * 1000) + " ms)");
    }

    private bool IsMine()
    {
        if (IsSpawned && IsOwner) return true;
        Debug.Log("[Lab] usa los botones del jugador que dice (YO)");
        return false;
    }
}
