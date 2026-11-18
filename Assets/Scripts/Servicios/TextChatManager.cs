// =============================================================================
// MODULO ........ 12 · Chat de texto con Vivox (S14)
// GAMEOBJECT .... "Vivox" (el mismo del modulo 11, VoiceManager): este script
//                 escribe en el canal que abre VoiceManager.
// PAQUETE ....... com.unity.services.vivox
// ANTES ......... VoiceManager: Initialize And Login y luego Join Proximity
//                 (o Join Group). Esos canales ya se abren con TextAndAudio.
// DOC OFICIAL ... docs.unity.com · Vivox Unity SDK · Text chat (Send and receive messages)
// =============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Services.Vivox;
using UnityEngine;

// El texto viaja por Vivox igual que la voz: no pasa por NetworkManager ni por RPC.
// Hay dos tipos: mensaje de CANAL (lo ven todos los del canal) y mensaje DIRECTO (a un jugador).
[RequireComponent(typeof(VoiceManager))]
public class TextChatManager : MonoBehaviour
{
    [Header("Ventana de chat (para probar)")]
    public bool showWindow = true;
    public int maxLines = 50;

    // PlayerController mira esto para no mover al jugador mientras escribes.
    public static bool Escribiendo { get; private set; }

    VoiceManager voice;
    readonly List<string> lines = new List<string>();
    string draft = "";
    Vector2 scroll;
    bool subscribed;

    private void Awake() => voice = GetComponent<VoiceManager>();

    private void OnEnable()
    {
        voice.OnLoggedIn += Subscribe;
        if (voice.LoggedIn) Subscribe();   // por si el login ya ocurrio
    }

    private void OnDisable()
    {
        voice.OnLoggedIn -= Subscribe;
        Unsubscribe();
        Escribiendo = false;
    }

    // ---- 1 · Escuchar -----------------------------------------------------------
    private void Subscribe()
    {
        if (subscribed) return;
        VivoxService.Instance.ChannelMessageReceived += OnChannelMessage;
        VivoxService.Instance.DirectedMessageReceived += OnDirectMessage;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        VivoxService.Instance.ChannelMessageReceived -= OnChannelMessage;
        VivoxService.Instance.DirectedMessageReceived -= OnDirectMessage;
        subscribed = false;
    }

    // Tu propio mensaje tambien llega por aqui (FromSelf = true): asi se ve igual para todos.
    private void OnChannelMessage(VivoxMessage m) => AddLine(Format(m, ""));
    private void OnDirectMessage(VivoxMessage m) => AddLine(Format(m, "[privado] "));

    private static string Format(VivoxMessage m, string prefix) =>
        prefix + (m.FromSelf ? "Yo" : m.SenderDisplayName) + ": " + m.MessageText;

    // ---- 2 · Enviar -------------------------------------------------------------
    [Button]   // al canal abierto por VoiceManager
    public async Task Send(string text = "Hola equipo!")
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (voice.Channel == null) { Debug.Log("[Texto] no hay canal: abre uno en VoiceManager"); return; }

        try { await VivoxService.Instance.SendChannelTextMessageAsync(voice.Channel, text); }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    [Button]   // a un solo jugador (su PlayerId sale de VoiceManager > Print Participants)
    public async Task SendDirect(string playerId, string text = "Psst, solo para ti")
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        try { await VivoxService.Instance.SendDirectTextMessageAsync(playerId, text); }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    // ---- 3 · Historial ----------------------------------------------------------
    [Button]   // los ultimos mensajes del canal (los que llegaron mientras no estabas)
    public async Task LoadHistory(int count = 10)
    {
        if (voice.Channel == null) return;
        try
        {
            var history = await VivoxService.Instance.GetChannelTextMessageHistoryAsync(voice.Channel, count);
            foreach (VivoxMessage m in history.OrderBy(h => h.ReceivedTime))
                AddLine(m.ReceivedTime.ToLocalTime().ToString("HH:mm") + "  " + Format(m, ""));
        }
        catch (Exception ex) { Debug.LogException(ex); }
    }

    // ---- 4 · Ventana de prueba (IMGUI) ------------------------------------------
    private void AddLine(string line)
    {
        Debug.Log("[Texto] " + line);
        lines.Add(line);
        if (lines.Count > maxLines) lines.RemoveAt(0);
        scroll.y = float.MaxValue;   // baja hasta el ultimo mensaje
    }

    private void OnGUI()
    {
        if (!showWindow || voice.Channel == null) return;

        var area = new Rect(10, Screen.height - 250, 380, 240);
        GUILayout.BeginArea(area, GUI.skin.box);

        scroll = GUILayout.BeginScrollView(scroll);
        foreach (string line in lines) GUILayout.Label(line);
        GUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        GUI.SetNextControlName("chat");
        draft = GUILayout.TextField(draft, 200);
        bool enter = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return;
        if ((GUILayout.Button("Enviar", GUILayout.Width(60)) || (enter && GUI.GetNameOfFocusedControl() == "chat"))
            && draft.Length > 0)
        {
            _ = Send(draft);
            draft = "";
        }
        GUILayout.EndHorizontal();

        Escribiendo = GUI.GetNameOfFocusedControl() == "chat";
        GUILayout.EndArea();
    }
}
