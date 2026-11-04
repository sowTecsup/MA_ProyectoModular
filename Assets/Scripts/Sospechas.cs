// =============================================================================
// ORDEN ......... 2 de 5. No usa ningun otro script.
// GAMEOBJECT .... ninguno: es una clase estatica, vive en el servidor
// =============================================================================
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// S12 · Un rechazo no es una condena: el lag tambien hace cosas raras. Se cuenta, y
// solo pasado el limite se expulsa. Al host no se le expulsa: ES el servidor.
public static class Sospechas
{
    public const int LIMITE = 30;
    private static readonly Dictionary<ulong, int> cuenta = new Dictionary<ulong, int>();

    public static void Anotar(NetworkManager nm, ulong clientId, string motivo)
    {
        if (clientId == NetworkManager.ServerClientId)
        {
            Debug.LogWarning("[Validacion] host: " + motivo + " (rechazado, pero al host no se le expulsa)");
            return;
        }

        cuenta[clientId] = cuenta.TryGetValue(clientId, out int c) ? c + 1 : 1;
        Debug.LogWarning("[Validacion] cliente " + clientId + ": " + motivo + " (" + cuenta[clientId] + "/" + LIMITE + ")");
        if (cuenta[clientId] < LIMITE) return;

        cuenta.Remove(clientId);
        nm.DisconnectClient(clientId, "Demasiadas acciones invalidas");   // le llega en DisconnectReason (S09)
    }
}
