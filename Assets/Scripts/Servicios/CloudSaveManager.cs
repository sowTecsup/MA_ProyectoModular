// =============================================================================
// MODULO ........ 4 · Cloud Save (S09)
// GAMEOBJECT .... "CloudSave" (Create Empty) en la escena. Necesita el modulo 1.
// PAQUETE ....... com.unity.services.cloudsave
// DOC OFICIAL ... docs.unity.com · Cloud Save · Unity SDK tutorial
// =============================================================================
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Services.CloudSave;
using UnityEngine;

public class CloudSaveManager : MonoBehaviour
{
    // Un objeto entero se guarda como JSON bajo una sola clave.
    public PlayerProfile profile = new PlayerProfile();

    [Button]
    public async Task SaveData(string key = "monedas", string value = "10")
    {
        try
        {
            var playerData = new Dictionary<string, object> { { key, value } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(playerData);
            Debug.Log("[Nube] guardado " + key + " = " + value);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    [Button]
    public async Task LoadData(string key = "monedas")
    {
        try
        {
            var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { key });
            if (playerData.TryGetValue(key, out var item))
                Debug.Log("[Nube] " + key + " = " + item.Value.GetAs<string>());
            else
                Debug.Log("[Nube] no hay nada guardado en " + key);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    [Button]
    public async Task DeleteData(string key = "monedas")
    {
        try
        {
            await CloudSaveService.Instance.Data.Player.DeleteAsync(key, new Unity.Services.CloudSave.Models.Data.Player.DeleteOptions());
            Debug.Log("[Nube] borrado " + key);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    [Button]
    public async Task SaveProfile()
    {
        try
        {
            string json = JsonUtility.ToJson(profile);
            await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object> { { "player_profile", json } });
            Debug.Log("[Nube] perfil guardado: " + json);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    [Button]
    public async Task LoadProfile()
    {
        try
        {
            var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { "player_profile" });
            if (playerData.TryGetValue("player_profile", out var item))
            {
                profile = JsonUtility.FromJson<PlayerProfile>(item.Value.GetAs<string>());
                Debug.Log("[Nube] perfil cargado: nivel " + profile.level + ", oro " + profile.gold);
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }
}

[Serializable]
public class PlayerProfile
{
    public string characterName = "Ana";
    public int level = 1;
    public int gold = 0;
}
