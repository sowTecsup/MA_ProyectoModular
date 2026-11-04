// =============================================================================
// MODULO ........ 10 · Lag compensation (S11)
// PREFAB ........ "Player": agrega RebobinadoDemo
// PARA NOTARLO .. 250 ms en el Network Simulator del cliente y el otro jugador moviendose
// DOC OFICIAL ... NGO no trae API de rebobinado: la doc ("Dealing with latency") solo
//                 nombra el patron "server side rewind". Esto es el del CTX de la S11.
// =============================================================================
using Sirenix.OdinInspector;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

// El atacante ve a los demas un poco en el pasado. El servidor guarda donde estuvo
// cada jugador en cada tick y mide el golpe contra ESA posicion.
public class RebobinadoDemo : NetworkBehaviour
{
    public float reach = 3f;

    private struct Sample { public int tick; public Vector3 position; }
    private const int SIZE = 64;                       // 64 ticks a 30 Hz = 2 s de memoria
    private readonly Sample[] history = new Sample[SIZE];
    private int lastHitTick = -100;

    public override void OnNetworkSpawn()
    {
        if (IsServer) NetworkManager.NetworkTickSystem.Tick += Record;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer) NetworkManager.NetworkTickSystem.Tick -= Record;
    }

    private void Record()
    {
        int tick = NetworkManager.ServerTime.Tick;
        history[tick % SIZE] = new Sample { tick = tick, position = transform.position };
    }

    // Donde estaba en el instante t? Interpola entre los dos ticks guardados.
    public bool PositionAt(double t, out Vector3 position)
    {
        double dt = NetworkManager.ServerTime.FixedDeltaTimeAsDouble;
        int t0 = (int)System.Math.Floor(t / dt);
        Sample a = history[t0 % SIZE], b = history[(t0 + 1) % SIZE];
        position = transform.position;
        if (a.tick != t0 || b.tick != t0 + 1) return false;   // demasiado viejo
        position = Vector3.Lerp(a.position, b.position, (float)((t - t0 * dt) / dt));
        return true;
    }

    // El cliente dice CUANDO vio al rival: la hora del servidor menos lo que atrasa el interpolador.
    [Button]
    public void Hit()
    {
        if (!IsSpawned || !IsOwner) { Debug.Log("[Lab] usa los botones del jugador que dice (YO)"); return; }
        HitRpc(NetworkManager.ServerTime.Time - NetworkTransform.GetTickLatencyInSeconds());
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void HitRpc(double seenAt)
    {
        // S12: un instante que su RTT pueda explicar, y a su ritmo.
        double now = NetworkManager.ServerTime.Time;
        double rtt = NetworkManager.NetworkConfig.NetworkTransport.GetCurrentRtt(OwnerClientId) / 1000.0;
        if (seenAt > now || now - seenAt > System.Math.Min(2 * rtt + 0.2, 1.0))
        { Debug.LogWarning("[Servidor] rechazado: golpe a un instante imposible"); return; }
        if (NetworkManager.ServerTime.Tick - lastHitTick < 15)
        { Debug.LogWarning("[Servidor] rechazado: golpe antes de tiempo"); return; }
        lastHitTick = NetworkManager.ServerTime.Tick;

        foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
        {
            if (client.PlayerObject == null || client.PlayerObject == NetworkObject) continue;
            RebobinadoDemo rival = client.PlayerObject.GetComponent<RebobinadoDemo>();
            if (!rival.PositionAt(seenAt, out Vector3 wasAt)) continue;

            float distanceNow = Vector3.Distance(transform.position, rival.transform.position);
            float distanceThen = Vector3.Distance(transform.position, wasAt);   // el rival, en el pasado
            Debug.Log("[Golpe] rival " + client.ClientId + ": ahora a " + distanceNow.ToString("0.0")
                      + " m, cuando lo viste a " + distanceThen.ToString("0.0") + " m -> "
                      + (distanceThen <= reach ? "ACIERTO" : "FALLO"));
        }
    }
}
