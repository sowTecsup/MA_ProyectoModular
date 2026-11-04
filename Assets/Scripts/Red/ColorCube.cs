// =============================================================================
// MODULO ........ 7 · Spawn y dueno (S06) · el objeto que se crea
// PREFAB ........ "ColorCube": Cube (escala 0.5) + NetworkObject + ColorCube
//                 NetworkObject > Dont Destroy With Owner = SI
// DOC OFICIAL ... Netcode for GameObjects · NetworkObject ownership
// =============================================================================
using Unity.Netcode;
using UnityEngine;

// Cada paso de su vida llega como un evento. Se pinta del color de su dueno.
public class ColorCube : NetworkBehaviour
{
    private static readonly Color[] COLORS = { Color.white, Color.red, Color.green, Color.blue, Color.yellow };

    public override void OnNetworkSpawn()
    {
        Paint(OwnerClientId);   // aqui el dueno ya vale; en Start todavia no
        Debug.Log("[Cubo " + NetworkObjectId + "] nace, dueno " + OwnerClientId);
    }

    // Llega a TODAS las maquinas: por eso se repinta aqui.
    protected override void OnOwnershipChanged(ulong previous, ulong current)
    {
        Paint(current);
        Debug.Log("[Cubo " + NetworkObjectId + "] dueno " + previous + " -> " + current);
    }

    public override void OnNetworkDespawn()
    {
        Debug.Log("[Cubo " + NetworkObjectId + "] desaparece");
    }

    private void Paint(ulong owner)
    {
        GetComponent<Renderer>().material.color = COLORS[owner % (ulong)COLORS.Length];
    }
}
