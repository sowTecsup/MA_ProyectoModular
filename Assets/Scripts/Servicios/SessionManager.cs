// =============================================================================
// MODULO ........ 3 · Sesion online: Lobby + Relay (S08)
// GAMEOBJECT .... "Sesion" (Create Empty) en la escena. Necesita el modulo 1.
// PAQUETE ....... com.unity.services.multiplayer
// ANTES ......... un NetworkManager con UnityTransport en la escena
// DOC OFICIAL ... docs.unity.com · Multiplayer Services · Create a session / Join a session
// =============================================================================
using System;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Services.Multiplayer;
using UnityEngine;

// Una sesion junta Lobby + Relay + NetworkManager: crearla arranca el host,
// unirse arranca el cliente. Reemplaza al TestLobby y al RelayManager de antes.
public class SessionManager : MonoBehaviour
{
    public ISession Session;

    [Button]
    public async Task CreateSession(int maxPlayers = 4)
    {
        try
        {
            var options = new SessionOptions
            {
                MaxPlayers = maxPlayers
            }.WithRelayNetwork();

            Session = await MultiplayerService.Instance.CreateSessionAsync(options);
            SetupEvents();
            Debug.Log("[Sesion] creada. Codigo para unirse: " + Session.Code);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    [Button]
    public async Task JoinSessionByCode(string joinCode)
    {
        try
        {
            Session = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode);
            SetupEvents();
            Debug.Log("[Sesion] dentro de " + Session.Id);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    // Solo aparecen las sesiones publicas y con sitio.
    [Button]
    public async Task QuerySessions()
    {
        try
        {
            QuerySessionsResults results = await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions());
            Debug.Log("[Sesion] encontradas: " + results.Sessions.Count);
            foreach (ISessionInfo info in results.Sessions)
                Debug.Log("   Id " + info.Id + " · libres " + info.AvailableSlots + "/" + info.MaxPlayers);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    [Button]
    public async Task JoinSessionById(string sessionId)
    {
        try
        {
            Session = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId);
            SetupEvents();
            Debug.Log("[Sesion] dentro de " + Session.Id);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    [Button]
    public void PrintPlayers()
    {
        if (Session == null) return;
        foreach (IReadOnlyPlayer player in Session.Players)
            Debug.Log("[Sesion] jugador " + player.Id + (player.Id == Session.Host ? " (host)" : ""));
    }

    [Button]
    public async Task LeaveSession()
    {
        if (Session == null) return;
        try
        {
            await Session.LeaveAsync();
            Session = null;
            Debug.Log("[Sesion] fuera");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private void SetupEvents()
    {
        Session.PlayerJoined += playerId => Debug.Log("[Sesion] entra " + playerId);
        Session.PlayerHasLeft += playerId => Debug.Log("[Sesion] se fue " + playerId);
    }
}
