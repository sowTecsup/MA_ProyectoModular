// =============================================================================
// ORDEN ......... 3 de 5. Usa Sospechas.
// GAMEOBJECT .... prefab "ColorCube" (3D Object > Cube, escala 0.5)
// COMPONENTES ... NetworkObject   Dont Destroy With Owner = SI
//                 ColorCube
// ARRASTRAR ..... el prefab al campo cubePrefab del PlayerAvatar
// =============================================================================
using Unity.Netcode;
using UnityEngine;

// S06 · Un objeto de red que nace, cambia de dueno y muere por codigo, y avisa en
// cada paso con su evento. Se pinta del color de su dueno.
public class ColorCube : NetworkBehaviour
{
    // Un color por clientId. El 0 es el host.
    private static readonly Color[] COLORES = { Color.white, Color.red, Color.green, Color.blue, Color.yellow };

    public static Color ColorDe(ulong clientId) => COLORES[clientId % (ulong)COLORES.Length];

    public override void OnNetworkSpawn()
    {
        Pintar(OwnerClientId);   // aqui el dueno ya vale; en Start todavia no
        Debug.Log("[Cubo " + NetworkObjectId + "] nace, dueno " + OwnerClientId);
    }

    // Llega a TODAS las maquinas: por eso se repinta aqui.
    protected override void OnOwnershipChanged(ulong anterior, ulong actual)
    {
        Pintar(actual);
        Debug.Log("[Cubo " + NetworkObjectId + "] dueno " + anterior + " -> " + actual);
    }

    public override void OnNetworkDespawn()
    {
        Debug.Log("[Cubo " + NetworkObjectId + "] desaparece");
    }

    // S06 + S12 · Regalar SOLO a quien esta cerca: el alcance se mide en el servidor.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void RegalarRpc(ulong destinatario)
    {
        if (!NetworkManager.ConnectedClients.TryGetValue(destinatario, out NetworkClient c) || c.PlayerObject == null) return;
        if (Vector3.Distance(c.PlayerObject.transform.position, transform.position) > PlayerAvatar.ALCANCE_MANOS)
        {
            Sospechas.Anotar(NetworkManager, OwnerClientId, "regalo a distancia");
            return;
        }
        NetworkObject.ChangeOwnership(destinatario);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void ConsumirRpc()
    {
        NetworkObject.Despawn();
    }

    private void Pintar(ulong dueno)
    {
        GetComponent<Renderer>().material.color = ColorDe(dueno);
    }
}
