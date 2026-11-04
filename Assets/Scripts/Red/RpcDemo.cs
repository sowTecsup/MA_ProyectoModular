// =============================================================================
// MODULO ........ 5 · RPC (S02)
// PREFAB ........ "Player": agrega RpcDemo
// DOC OFICIAL ... Netcode for GameObjects · RPC (el ejemplo ping / pong)
// =============================================================================
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

// Un RPC es un evento: pasa una vez y el que entra despues no lo ve.
public class RpcDemo : NetworkBehaviour
{
    private int pingCount;

    [Button]
    public void Ping()
    {
        if (!IsMine()) return;
        pingCount++;
        PingRpc(pingCount);
    }

    // Cliente -> Servidor. RpcParams dice quien lo envio.
    [Rpc(SendTo.Server)]
    public void PingRpc(int count, RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        Debug.Log("[RPC] ping " + count + " del cliente " + sender);
        PongRpc(count, "PONG!", RpcTarget.Single(sender, RpcTargetUse.Temp));
    }

    // Servidor -> SOLO el cliente que pregunto.
    [Rpc(SendTo.SpecifiedInParams)]
    private void PongRpc(int count, string message, RpcParams rpcParams)
    {
        Debug.Log("[RPC] " + message + " para el ping " + count);
    }

    [Button]
    public void SayHelloToEveryone(string text = "Hola")
    {
        if (!IsMine()) return;
        HelloRpc(text);
    }

    // Owner: solo el dueno de este jugador puede enviarlo. Lo reciben todos.
    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Owner)]
    private void HelloRpc(string text, RpcParams rpcParams = default)
    {
        Debug.Log("[RPC] el cliente " + rpcParams.Receive.SenderClientId + " dice: " + text);
    }

    private bool IsMine()
    {
        if (IsSpawned && IsOwner) return true;
        Debug.Log("[Lab] usa los botones del jugador que dice (YO)");
        return false;
    }
}
