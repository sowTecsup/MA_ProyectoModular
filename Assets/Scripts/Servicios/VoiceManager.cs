// =============================================================================
// MODULO ........ 11 · Vivox: voz de proximidad y canal de chat (S14)
// GAMEOBJECT .... "Vivox" (Create Empty) en la escena, junto al modulo 12
//                 (TextChatManager). Necesita el modulo 1 (Auth) y el modulo 3
//                 (Sesion): el canal se llama como la sesion.
// PAQUETE ....... com.unity.services.vivox
// ANTES ......... Edit > Project Settings > Services: proyecto enlazado, y en el
//                 Dashboard: Development > Products > Vivox Voice and Text Chat
// DOC OFICIAL ... docs.unity.com · Vivox Unity SDK · Get started · Positional channels
// =============================================================================
using System;
using System.Linq;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Netcode;
using Unity.Services.Vivox;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

// El audio NO viaja por NetworkManager ni por el Relay: Vivox tiene sus propios
// servidores y su propia conexion. Por eso hay que salir del canal a mano.
public class VoiceManager : MonoBehaviour
{
    [Header("Canal de proximidad")]
    public int audibleDistance = 30;          // mas lejos que esto, no se oye (en metros de Unity)
    public int conversationalDistance = 1;    // dentro de esto, volumen completo
    public float fadeIntensity = 1.1f;        // 32 x 1 / 1.1 = 29 <= 30: el volumen llega a casi cero en el borde

    string channel;                           // "voz-" + id de la sesion
    float nextPositionUpdate;

    // El modulo 12 (chat de texto) escribe en este mismo canal y avisa cuando hay sesion de Vivox.
    public string Channel => channel;
    public bool LoggedIn { get; private set; }
    public event Action OnLoggedIn;

    private void Start()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);
#endif
    }

    // ---- 1 · Arrancar: Core y Auth ya estan (modulo 1) --------------------------
    [Button]
    public async Task InitializeAndLogin()
    {
        try
        {
            await VivoxService.Instance.InitializeAsync();
            await VivoxService.Instance.LoginAsync();   // entra con el PlayerId de Authentication
            Debug.Log("[Voz] dentro. PlayerId " + VivoxService.Instance.SignedInPlayerId);

            if (!LoggedIn)   // los eventos se suscriben una sola vez, aunque se pulse el boton dos veces
            {
                VivoxService.Instance.ParticipantAddedToChannel += p => Debug.Log("[Voz] se oye a " + p.PlayerId);
                VivoxService.Instance.ParticipantRemovedFromChannel += p => Debug.Log("[Voz] ya no se oye a " + p.PlayerId);
            }
            LoggedIn = true;
            OnLoggedIn?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    // ---- 2 · Entrar a un canal ------------------------------------------------
    [Button]   // para probar el microfono: solo te oyes a ti mismo
    public async Task JoinEcho()
    {
        try { await VivoxService.Instance.JoinEchoChannelAsync("eco", ChatCapability.AudioOnly); }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    // TextAndAudio: el MISMO canal lleva la voz y el chat de texto (AudioOnly o TextOnly para uno solo).
    [Button]   // canal de grupo: se oye a todos, esten donde estén
    public async Task JoinGroup(string sessionId)
    {
        try
        {
            channel = "voz-" + sessionId;
            await VivoxService.Instance.JoinGroupChannelAsync(channel, ChatCapability.TextAndAudio);
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    // ---- 3 · Voz de proximidad --------------------------------------------------
    [Button]   // usa la sesion del modulo 3
    public async Task JoinProximity()
    {
        var session = FindAnyObjectByType<SessionManager>()?.Session;
        if (session == null) { Debug.Log("[Voz] no hay sesion: crea o entra a una primero"); return; }

        try
        {
            channel = "voz-" + session.Id;
            var props = new Channel3DProperties(audibleDistance, conversationalDistance,
                                                fadeIntensity, AudioFadeModel.InverseByDistance);
            await VivoxService.Instance.JoinPositionalChannelAsync(channel, ChatCapability.TextAndAudio, props);

            // La voz no sabe de NGO: cuando la red se detiene, salimos del canal.
            NetworkManager.Singleton.OnClientStopped -= OnClientStopped;   // sin duplicar si se entra dos veces
            NetworkManager.Singleton.OnClientStopped += OnClientStopped;
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    private void Update()
    {
        // Cada cliente dice DONDE esta su avatar, 2-4 veces por segundo (doc oficial).
        if (channel == null || Time.time < nextPositionUpdate) return;

        var nm = NetworkManager.Singleton;
        var me = (nm != null && nm.LocalClient != null) ? nm.LocalClient.PlayerObject : null;
        if (me == null) return;
        if (!VivoxService.Instance.ActiveChannels.ContainsKey(channel)) return;   // aun no esta dentro

        VivoxService.Instance.Set3DPosition(me.gameObject, channel);
        nextPositionUpdate = Time.time + 0.3f;
    }

    private async void OnClientStopped(bool wasHost)
    {
        NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
        await LeaveChannel();
    }

    [Button]
    public async Task LeaveChannel()
    {
        if (channel == null) return;
        try
        {
            await VivoxService.Instance.LeaveChannelAsync(channel);
            channel = null;
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    // ---- 4 · Microfono y volumenes ---------------------------------------------
    [Button]
    public void MuteMic(bool mute = true)
    {
        if (mute) VivoxService.Instance.MuteInputDevice();    // nadie te oye, en ningun canal
        else VivoxService.Instance.UnmuteInputDevice();
    }

    [Button]
    public void PrintInputDevices()
    {
        foreach (VivoxInputDevice d in VivoxService.Instance.AvailableInputDevices)
            Debug.Log("[Voz] microfono: " + d.DeviceName + (d.DeviceID == VivoxService.Instance.ActiveInputDevice.DeviceID ? "  (activo)" : ""));
    }

    [Button]
    public async Task SetInputDevice(string nameContains)
    {
        var device = VivoxService.Instance.AvailableInputDevices.FirstOrDefault(d => d.DeviceName.Contains(nameContains));
        if (device == null) { Debug.Log("[Voz] no hay un microfono con ese nombre"); return; }
        await VivoxService.Instance.SetActiveInputDeviceAsync(device);
    }

    [Button]   // -50 a 50, 0 por defecto
    public void SetMicVolume(int value = 0) => VivoxService.Instance.SetInputDeviceVolume(Mathf.Clamp(value, -50, 50));

    [Button]
    public void SetOutputVolume(int value = 0) => VivoxService.Instance.SetOutputDeviceVolume(Mathf.Clamp(value, -50, 50));

    [Button]
    public async Task SetChannelVolume(int value = 0)
    {
        if (channel != null) await VivoxService.Instance.SetChannelVolumeAsync(channel, Mathf.Clamp(value, -50, 50));
    }

    [Button]
    public void PrintParticipants()
    {
        if (channel == null || !VivoxService.Instance.ActiveChannels.TryGetValue(channel, out var participants)) return;
        foreach (VivoxParticipant p in participants)
            Debug.Log("[Voz] " + p.PlayerId + (p.IsSelf ? " (yo)" : "") + "  habla=" + p.SpeechDetected +
                      "  silenciado=" + p.IsMuted + "  volumen local=" + p.LocalVolume);
    }

    // Solo cambia lo que OYES tu; los demas lo siguen oyendo igual.
    [Button]
    public void SetParticipantVolume(string playerId, int value = 0)
    {
        var p = Find(playerId);
        if (p != null) p.SetLocalVolume(Mathf.Clamp(value, -50, 50));
    }

    [Button]
    public void MuteParticipant(string playerId, bool mute = true)
    {
        var p = Find(playerId);
        if (p == null) return;
        if (mute) p.MutePlayerLocally(); else p.UnmutePlayerLocally();
    }

    // Bloquear silencia en los dos sentidos (audio y texto) entre tu y ese jugador.
    [Button]
    public async Task BlockPlayer(string playerId) => await VivoxService.Instance.BlockPlayerAsync(playerId);

    private VivoxParticipant Find(string playerId)
    {
        if (channel == null || !VivoxService.Instance.ActiveChannels.TryGetValue(channel, out var participants)) return null;
        return participants.FirstOrDefault(p => p.PlayerId == playerId);
    }
}
