// =============================================================================
// MODULO ........ 2 · Red local: host y cliente (S01 · S06 · S09)
// GAMEOBJECT .... "NetworkManager" de la escena (NetworkManager + UnityTransport)
// PAQUETE ....... com.unity.netcode.gameobjects
// DOC OFICIAL ... Netcode for GameObjects · Connection events
// =============================================================================
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

public class NetworkStarter : MonoBehaviour
{
    void Start()
    {
        NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;
        NetworkManager.Singleton.OnClientStopped += OnClientStopped;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnConnectionEvent -= OnConnectionEvent;
        NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
    }

    [Button]
    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
    }

    [Button]
    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }

    // Asi se ve una desconexion: el servidor despawnea tu jugador.
    [Button]
    public void Shutdown()
    {
        NetworkManager.Singleton.Shutdown();
    }

    // ClientConnected (servidor y el que entra) · PeerConnected (los que ya estaban)
    private void OnConnectionEvent(NetworkManager networkManager, ConnectionEventData data)
    {
        Debug.Log("[Red] " + data.EventType + " -> cliente " + data.ClientId);
    }

    private void OnClientStopped(bool wasHost)
    {
        Debug.Log("[Red] desconectado. Motivo: " + NetworkManager.Singleton.DisconnectReason);
    }
}
