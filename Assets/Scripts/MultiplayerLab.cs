// =============================================================================
// ORDEN ......... 5 de 5. El ultimo: usa PlayerAvatar, ColorCube y Ranura.
// GAMEOBJECT .... "MultiplayerLab" (Create Empty) en la escena
// COMPONENTES ... MultiplayerLab (los botones son de Odin Inspector)
// EN LA ESCENA .. "NetworkManager": NetworkManager + UnityTransport
//                 Player Prefab = PlayerAvatar
// SERVICIOS ..... Edit > Project Settings > Services: el proyecto enlazado (Project ID)
// =============================================================================
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

// Un MODULO por tema del curso. Cada boton hace UNA cosa y la cuenta por consola.
// Lo de red no vive aqui: los botones llaman a PlayerAvatar.Local, que tiene los RPC.
public class MultiplayerLab : MonoBehaviour
{
    private const string M1 = "1 · Authentication (S08)";
    private const string M2 = "2 · Red local: host y cliente (S01 · S06 · S09)";
    private const string M3 = "3 · Sesion online: Lobby + Relay (S08)";
    private const string M4 = "4 · RPC (S02)";
    private const string M5 = "5 · NetworkVariable: estado (S03 · S04)";
    private const string M6 = "6 · Spawn y dueno (S06)";
    private const string M7 = "7 · Mochila con NetworkList (S07)";
    private const string M8 = "8 · Cloud Save (S09)";
    private const string M9 = "9 · Prediccion y reconciliacion (S10)";
    private const string M10 = "10 · Lag compensation (S11)";
    private const string M11 = "11 · Validacion en servidor: trampas (S12)";

    private const string CLAVE_MOCHILA = "mochila";
    private ISession sesion;

    void Start()
    {
        NetworkManager nm = NetworkManager.Singleton;
        // ClientConnected (servidor y el que entra) · PeerConnected (los que ya estaban)
        nm.OnConnectionEvent += (red, datos) => Debug.Log("[Red] " + datos.EventType + " -> cliente " + datos.ClientId);
        nm.OnClientStopped += (eraHost) =>
            Debug.Log("[Red] me desconecte" + (string.IsNullOrEmpty(nm.DisconnectReason) ? "" : ": " + nm.DisconnectReason));
    }

    // ==== 1 · AUTHENTICATION =========================================================

    [FoldoutGroup(M1), ShowInInspector, ReadOnly]
    private string PlayerId => ServiciosListos() ? AuthenticationService.Instance.PlayerId : "(sin iniciar)";

    [FoldoutGroup(M1), ShowInInspector, ReadOnly]
    private string NombreEnUGS => ServiciosListos() ? AuthenticationService.Instance.PlayerName : "-";

    // En MPPM cada jugador virtual recibe su propio perfil: dos jugadores, dos PlayerId.
    [FoldoutGroup(M1), Button]
    public async void IniciarSesionAnonima()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("[Auth] dentro. PlayerId " + AuthenticationService.Instance.PlayerId);
        }
        catch (System.Exception ex) { Debug.LogException(ex); }
    }

    [FoldoutGroup(M1), Button]
    public async void CambiarNombreEnUGS(string nombre = "Jugador")
    {
        try
        {
            string final = await AuthenticationService.Instance.UpdatePlayerNameAsync(nombre);
            Debug.Log("[Auth] nombre: " + final);   // UGS le agrega #1234
        }
        catch (System.Exception ex) { Debug.LogException(ex); }
    }

    [FoldoutGroup(M1), Button]
    public void CerrarSesion()
    {
        if (!ServiciosListos()) return;
        AuthenticationService.Instance.SignOut();
        Debug.Log("[Auth] sesion cerrada");
    }

    // ==== 2 · RED LOCAL ===============================================================

    [FoldoutGroup(M2), ShowInInspector, ReadOnly]
    private string Estado
    {
        get
        {
            NetworkManager nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsListening) return "sin red";
            if (nm.IsHost) return "HOST · conectados: " + nm.ConnectedClientsIds.Count;
            return "CLIENTE " + nm.LocalClientId;
        }
    }

    [FoldoutGroup(M2), Button]
    public void Host() => NetworkManager.Singleton.StartHost();

    [FoldoutGroup(M2), Button]
    public void Cliente() => NetworkManager.Singleton.StartClient();

    // Asi se ve una desconexion: el servidor despawnea tu avatar y tus cubos se quedan.
    [FoldoutGroup(M2), Button]
    public void Desconectar() => NetworkManager.Singleton.Shutdown();

    // ==== 3 · SESION ONLINE (Lobby + Relay) ==========================================

    [FoldoutGroup(M3), ShowInInspector, ReadOnly]
    private string Codigo => sesion != null ? sesion.Code : "(sin sesion)";

    [FoldoutGroup(M3), ShowInInspector, ReadOnly]
    private string Jugadores => sesion != null ? sesion.PlayerCount + " / " + sesion.MaxPlayers : "-";

    // Crear: Lobby + Relay + StartHost, en una sola llamada. Pide el modulo 1 antes.
    [FoldoutGroup(M3), Button]
    public async void CrearSesion(int maxJugadores = 4)
    {
        try
        {
            SessionOptions opciones = new SessionOptions { MaxPlayers = maxJugadores }.WithRelayNetwork();
            sesion = await MultiplayerService.Instance.CreateSessionAsync(opciones);
            Escuchar(sesion);
            Debug.Log("[Sesion] creada. Codigo para los demas: " + sesion.Code);
        }
        catch (System.Exception ex) { Debug.LogException(ex); }
    }

    // Unirse: Lobby + Relay + StartClient, tambien en una llamada.
    [FoldoutGroup(M3), Button]
    public async void UnirsePorCodigo(string codigo)
    {
        try
        {
            sesion = await MultiplayerService.Instance.JoinSessionByCodeAsync(codigo);
            Escuchar(sesion);
            Debug.Log("[Sesion] dentro de " + sesion.Id);
        }
        catch (System.Exception ex) { Debug.LogException(ex); }
    }

    // Entra a la primera sesion con sitio; si no hay ninguna, la crea.
    [FoldoutGroup(M3), Button]
    public async void UnirseRapido()
    {
        try
        {
            QuickJoinOptions rapido = new QuickJoinOptions { CreateSession = true };
            SessionOptions opciones = new SessionOptions { MaxPlayers = 4 }.WithRelayNetwork();
            sesion = await MultiplayerService.Instance.MatchmakeSessionAsync(rapido, opciones);
            Escuchar(sesion);
            Debug.Log("[Sesion] dentro de " + sesion.Id + (sesion.IsHost ? " (la cree yo)" : ""));
        }
        catch (System.Exception ex) { Debug.LogException(ex); }
    }

    [FoldoutGroup(M3), Button]
    public void VerJugadores()
    {
        if (sesion == null) return;
        foreach (IReadOnlyPlayer p in sesion.Players)
            Debug.Log("[Sesion] jugador " + p.Id + (p.Id == sesion.Host ? " (host)" : ""));
    }

    [FoldoutGroup(M3), Button]
    public async void SalirDeLaSesion()
    {
        if (sesion == null) return;
        try
        {
            await sesion.LeaveAsync();
            Debug.Log("[Sesion] fuera");
            sesion = null;
        }
        catch (System.Exception ex) { Debug.LogException(ex); }
    }

    private void Escuchar(ISession s)
    {
        s.PlayerJoined += id => Debug.Log("[Sesion] entra " + id);
        s.PlayerHasLeft += id => Debug.Log("[Sesion] se fue " + id);
    }

    // ==== 4 · RPC ======================================================================

    [FoldoutGroup(M4), Button]
    public void SaludarATodos(string texto = "Hola")
    {
        if (Yo() != null) Yo().SaludarRpc(texto);
    }

    // ==== 5 · NETWORKVARIABLE ==========================================================

    [FoldoutGroup(M5), ShowInInspector, ReadOnly]
    private string MiEstado => PlayerAvatar.Local == null ? "-" :
        PlayerAvatar.Local.nombre.Value + " · vida " + PlayerAvatar.Local.vida.Value +
        " · xp " + PlayerAvatar.Local.experiencia.Value + " · nivel " + PlayerAvatar.Local.Nivel;

    // nombre tiene WritePermission.Owner: el dueno lo escribe y todos lo ven, tambien el que llega tarde.
    [FoldoutGroup(M5), Button]
    public void CambiarMiNombre(string nombre = "Ana")
    {
        if (Yo() != null) Yo().nombre.Value = nombre;
    }

    [FoldoutGroup(M5), Button]
    public void VerNombresDeTodos()
    {
        foreach (PlayerAvatar a in FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None))
            Debug.Log("[Estado] jugador " + a.OwnerClientId + ": " + a.nombre.Value + " · vida " + a.vida.Value);
    }

    // ==== 6 · SPAWN Y DUENO ============================================================

    [FoldoutGroup(M6), Button]
    public void PedirCubo()
    {
        if (Yo() != null) Yo().PedirCuboRpc();
    }

    // Prueba con un clientId que este lejos: el servidor lo rechaza (modulo 11).
    [FoldoutGroup(M6), Button]
    public void RegalarMiCubo(ulong destinatario = 1)
    {
        ColorCube cubo = MiCubo();
        if (cubo != null) cubo.RegalarRpc(destinatario);
    }

    [FoldoutGroup(M6), Button]
    public void ConsumirMiCubo()
    {
        ColorCube cubo = MiCubo();
        if (cubo != null) cubo.ConsumirRpc();
    }

    // ==== 7 · MOCHILA ==================================================================

    [FoldoutGroup(M7), Button]
    public void GuardarMiCubo()
    {
        ColorCube cubo = MiCubo();
        if (cubo != null) Yo().GuardarRpc(cubo.NetworkObject);
    }

    [FoldoutGroup(M7), Button]
    public void SoltarUnCubo()
    {
        if (Yo() != null) Yo().SoltarRpc();
    }

    // La mochila tiene ReadPermission.Owner: en las demas maquinas sale vacia.
    [FoldoutGroup(M7), Button]
    public void VerMochila()
    {
        if (Yo() == null) return;
        Debug.Log("[Mochila] " + Yo().mochila.Count + " casillas");
        foreach (Ranura r in Yo().mochila) Debug.Log("   " + r.Nombre() + " x" + r.cantidad);
    }

    // ==== 8 · CLOUD SAVE ===============================================================

    [FoldoutGroup(M8), Button]
    public async void GuardarMochilaEnLaNube()
    {
        if (Yo() == null || !ServiciosListos()) { Debug.Log("[Nube] falta el modulo 1 o estar en red"); return; }
        List<Ranura> lista = new List<Ranura>();
        foreach (Ranura r in Yo().mochila) lista.Add(r);
        try
        {
            await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object> { { CLAVE_MOCHILA, lista } });
            Debug.Log("[Nube] mochila guardada: " + lista.Count + " casillas");
        }
        catch (System.Exception ex) { Debug.LogException(ex); }
    }

    // El cliente la lee de la nube y PIDE reponerla: el servidor decide (S12, version 5).
    [FoldoutGroup(M8), Button]
    public async void CargarMochilaDeLaNube()
    {
        if (Yo() == null || !ServiciosListos()) { Debug.Log("[Nube] falta el modulo 1 o estar en red"); return; }
        try
        {
            Dictionary<string, Unity.Services.CloudSave.Models.Item> datos =
                await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { CLAVE_MOCHILA });
            if (!datos.TryGetValue(CLAVE_MOCHILA, out Unity.Services.CloudSave.Models.Item item))
            {
                Debug.Log("[Nube] no hay mochila guardada");
                return;
            }
            Ranura[] nube = item.Value.GetAs<Ranura[]>();
            Debug.Log("[Nube] llegaron " + nube.Length + " casillas; se las pido al servidor");
            Yo().PedirReponerMochilaRpc(nube);
        }
        catch (System.Exception ex) { Debug.LogException(ex); }
    }

    // ==== 9 · PREDICCION Y RECONCILIACION ==============================================
    // Para notarlo: Window > Multiplayer > Network Simulator, 250 ms, en el cliente.

    [FoldoutGroup(M9), ShowInInspector, ReadOnly]
    private string Pasos => PlayerAvatar.Local == null ? "-" :
        "en pantalla " + PlayerAvatar.Local.pasosPredichos + " · servidor " + PlayerAvatar.Local.pasosServidor.Value.pasos +
        " · correcciones " + PlayerAvatar.Local.correcciones;

    [FoldoutGroup(M9), ShowInInspector, ReadOnly]
    private string Reloj
    {
        get
        {
            NetworkManager nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsListening) return "-";
            return "LocalTime tick " + nm.LocalTime.Tick + " · ServerTime tick " + nm.ServerTime.Tick;
        }
    }

    [FoldoutGroup(M9), Button]
    public void PasoSinPrediccion()
    {
        if (Yo() != null) Yo().Paso(false);
    }

    [FoldoutGroup(M9), Button]
    public void PasoConPrediccion()
    {
        if (Yo() != null) Yo().Paso(true);
    }

    [FoldoutGroup(M9), Button]
    public void MedirRtt()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening) return;
        if (nm.IsServer) { Debug.Log("[Red] soy el servidor: mi RTT es 0"); return; }
        ulong rtt = nm.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId);
        Debug.Log("[Red] RTT " + rtt + " ms · el servidor va " + (nm.LocalTime.Tick - nm.ServerTime.Tick) + " ticks por detras de mi");
    }

    // ==== 10 · LAG COMPENSATION =========================================================
    // El rival en movimiento (WASD en la otra ventana) y 250 ms en el Network Simulator:
    // el log compara donde esta AHORA con donde estaba cuando lo viste.

    [FoldoutGroup(M10), Button]
    public void Golpear()
    {
        if (Yo() != null) Yo().Golpear();
    }

    // ==== 11 · VALIDACION EN SERVIDOR: lo que haria un cliente modificado ==============
    // En la consola del SERVIDOR sale cada rechazo; a los 30, expulsa al cliente.

    [FoldoutGroup(M11), Button]
    public void TrampaGolpeDelPasado()
    {
        if (Yo() != null) Yo().GolpearRpc(NetworkManager.Singleton.ServerTime.Time - 5.0);
    }

    [FoldoutGroup(M11), Button]
    public void TrampaGolpeDelFuturo()
    {
        if (Yo() != null) Yo().GolpearRpc(NetworkManager.Singleton.ServerTime.Time + 1.0);
    }

    [FoldoutGroup(M11), Button]
    public void TrampaDiezPasosDeGolpe()
    {
        if (Yo() == null) return;
        for (int i = 0; i < 10; i++) Yo().Paso(true);   // el servidor acepta RAFAGA = 4
    }

    [FoldoutGroup(M11), Button]
    public void TrampaGuardarCuboAjeno()
    {
        if (Yo() == null) return;
        foreach (ColorCube cubo in FindObjectsByType<ColorCube>(FindObjectsSortMode.None))
            if (!cubo.IsOwner) { Yo().GuardarRpc(cubo.NetworkObject); return; }
        Debug.Log("[Trampa] no hay cubos de otro jugador");
    }

    [FoldoutGroup(M11), Button]
    public void TrampaReponer999Troncos()
    {
        if (Yo() != null) Yo().PedirReponerMochilaRpc(new[] { new Ranura { tipo = Ranura.TRONCO, cantidad = 999 } });
    }

    [FoldoutGroup(M11), Button]
    public void TrampaTeletransporte()
    {
        if (Yo() != null) Yo().TrampaTeletransporte();
    }

    [FoldoutGroup(M11), Button]
    public void TrampaEscribirMiVida()
    {
        if (Yo() != null) Yo().TrampaEscribirVida();
    }

    // ==== Ayudas ===========================================================================

    private PlayerAvatar Yo()
    {
        if (PlayerAvatar.Local == null && Application.isPlaying) Debug.Log("[Lab] primero entra a la red (modulo 2 o 3)");
        return PlayerAvatar.Local;
    }

    private ColorCube MiCubo()
    {
        foreach (ColorCube cubo in FindObjectsByType<ColorCube>(FindObjectsSortMode.None))
            if (cubo.IsOwner) return cubo;
        Debug.Log("[Lab] no tienes cubos: pide uno (modulo 6)");
        return null;
    }

    private bool ServiciosListos()
    {
        return UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn;
    }
}
