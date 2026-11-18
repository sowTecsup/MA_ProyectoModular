// =============================================================================
// MODULO ........ 0 · El jugador (S01 · S05)
// PREFAB ........ "Player": Capsule + NetworkObject
//                 + NetworkTransform (Authority Mode = Owner) + PlayerController
//                 NetworkManager > Default Player Prefab = este prefab
// DOC OFICIAL ... Multiplayer Services · Build a session with Netcode for GameObjects
// =============================================================================
using Unity.Netcode;
using UnityEngine;

// Los modulos 5 a 10 van en este mismo prefab, cada uno como un componente aparte.
public class PlayerController : NetworkBehaviour
{
    public float speed = 5f;

    public override void OnNetworkSpawn()
    {
        // En la Hierarchy: el tuyo dice (YO). Sus botones son los que funcionan.
        gameObject.name = "Jugador " + OwnerClientId + (IsOwner ? " (YO)" : "");
        if (IsOwner) transform.position = new Vector3(OwnerClientId * 2f, 1f, 0f);
    }

    private void Update()
    {
        if (!IsOwner || TextChatManager.Escribiendo) return;   // quieto mientras escribes en el chat (modulo 12)

        var movement = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        transform.position += movement * speed * Time.deltaTime;
    }
}
