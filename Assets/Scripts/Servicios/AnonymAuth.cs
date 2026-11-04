// =============================================================================
// MODULO ........ 1 · Authentication (S08)
// GAMEOBJECT .... "Auth" (Create Empty) en la escena
// PAQUETE ....... com.unity.services.authentication
// ANTES ......... Edit > Project Settings > Services: el proyecto enlazado
// DOC OFICIAL ... docs.unity.com · Authentication · Use anonymous sign-in
// =============================================================================
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class AnonymAuth : MonoBehaviour
{
    private async void Start()
    {
        await UnityServices.InitializeAsync();   //-> Inicializar los servicios de UGS
        SetupEvents();
    }

    private void SetupEvents()
    {
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("[Auth] dentro. PlayerId " + AuthenticationService.Instance.PlayerId);
        };
        AuthenticationService.Instance.SignInFailed += (err) =>
        {
            Debug.Log(err);
        };
        AuthenticationService.Instance.SignedOut += () =>
        {
            Debug.Log("[Auth] sesion cerrada");
        };
        AuthenticationService.Instance.Expired += () =>
        {
            Debug.Log("[Auth] la sesion expiro");
        };
    }

    // En Multiplayer Play Mode cada jugador virtual tiene su propio perfil: otro PlayerId.
    [Button]
    public async Task SignInAnonymously()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (AuthenticationException ex)
        {
            Debug.LogException(ex);   // compara ex.ErrorCode con AuthenticationErrorCodes
        }
        catch (RequestFailedException ex)
        {
            Debug.LogException(ex);   // compara ex.ErrorCode con CommonErrorCodes
        }
    }

    [Button]
    public async Task UpdatePlayerName(string newName = "Jugador")
    {
        try
        {
            string name = await AuthenticationService.Instance.UpdatePlayerNameAsync(newName);
            Debug.Log("[Auth] nombre: " + name);   // UGS le agrega #1234
        }
        catch (RequestFailedException ex)
        {
            Debug.LogException(ex);
        }
    }

    [Button]
    public void SignOut()
    {
        AuthenticationService.Instance.SignOut();
    }
}
