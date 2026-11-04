// =============================================================================
// MODULO ........ 7 · Spawn y dueno (S06)
// PREFAB ........ "Player": agrega SpawnDemo y arrastra el prefab ColorCube a cubePrefab
// DOC OFICIAL ... Netcode for GameObjects · Object spawning / NetworkObject ownership
// =============================================================================
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

// Un cliente no puede hacer Spawn: lo pide, y el servidor lo crea A SU NOMBRE.
public class SpawnDemo : NetworkBehaviour
{
    public NetworkObject cubePrefab;
    public float reach = 3f;

    [Button]
    public void SpawnCube()
    {
        if (!IsMine()) return;
        SpawnCubeRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void SpawnCubeRpc()
    {
        Vector3 position = transform.position + Vector3.forward * 1.5f;
        NetworkManager.SpawnManager.InstantiateAndSpawn(cubePrefab, OwnerClientId, position: position);
    }

    [Button]
    public void GiveMyCube(ulong toClientId = 1)
    {
        if (!IsMine()) return;
        NetworkObject cube = MyCube();
        if (cube != null) GiveCubeRpc(cube, toClientId);
    }

    // S12: el servidor no se fia. Que sea tuyo y que el otro este cerca.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void GiveCubeRpc(NetworkObjectReference cubeRef, ulong toClientId)
    {
        if (!cubeRef.TryGet(out NetworkObject cube)) return;
        if (cube.OwnerClientId != OwnerClientId) { Debug.LogWarning("[Servidor] rechazado: ese cubo no es tuyo"); return; }
        if (!NetworkManager.ConnectedClients.TryGetValue(toClientId, out NetworkClient client) || client.PlayerObject == null) return;
        if (Vector3.Distance(client.PlayerObject.transform.position, cube.transform.position) > reach)
        { Debug.LogWarning("[Servidor] rechazado: el otro jugador esta lejos"); return; }

        cube.ChangeOwnership(toClientId);
    }

    [Button]
    public void DespawnMyCube()
    {
        if (!IsMine()) return;
        NetworkObject cube = MyCube();
        if (cube != null) DespawnCubeRpc(cube);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void DespawnCubeRpc(NetworkObjectReference cubeRef)
    {
        if (!cubeRef.TryGet(out NetworkObject cube)) return;
        if (cube.OwnerClientId != OwnerClientId) return;
        cube.Despawn();
    }

    private NetworkObject MyCube()
    {
        foreach (NetworkObject obj in NetworkManager.SpawnManager.SpawnedObjectsList)
            if (obj.IsOwner && obj.PrefabIdHash == cubePrefab.PrefabIdHash) return obj;
        Debug.Log("[Spawn] no tienes cubos: pide uno");
        return null;
    }

    private bool IsMine()
    {
        if (IsSpawned && IsOwner) return true;
        Debug.Log("[Lab] usa los botones del jugador que dice (YO)");
        return false;
    }
}
