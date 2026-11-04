# MA_ProyectoModular · Todo lo visto, un script por módulo

**Desarrollo de Videojuegos para Móviles Avanzado** · 6.º ciclo · C26S

Lo visto de la semana 1 a la 12, como el proyecto del ciclo pasado (`S7MA` → `S14MA`):
**un script por módulo**, cada uno con sus botones de Odin Inspector, y cada uno armado
sobre el ejemplo de la documentación oficial de su tema. Se copia un archivo y se lleva
a otro juego.

Unity `6000.3.9f1` · Netcode for GameObjects **`2.13.3`** · Multiplayer Services `2.4.0`
· Authentication `3.8.0` · Cloud Save `3.4.1` · Multiplayer Play Mode `2.0.2` · Multiplayer Tools `2.2.12`.

## Los módulos

**Servicios** (`Scripts/Servicios`, un GameObject cada uno en la escena):

| # | Script | Semana | Doc oficial |
|---|---|---|---|
| 1 | `AnonymAuth` | S08 | Authentication · Use anonymous sign-in |
| 2 | `NetworkStarter` | S01 · S06 · S09 | NGO · Connection events |
| 3 | `SessionManager` | S08 | Multiplayer Services · Create / Join a session |
| 4 | `CloudSaveManager` | S09 | Cloud Save · Unity SDK tutorial |

**Red** (`Scripts/Red`, componentes del prefab `Player`):

| # | Script | Semana | Doc oficial |
|---|---|---|---|
| 0 | `PlayerController` | S01 · S05 | Multiplayer Services · Build a session with NGO |
| 5 | `RpcDemo` | S02 | NGO · RPC (ping / pong) |
| 6 | `VidaDemo` | S03 · S04 | NGO · NetworkVariables (la puerta) |
| 7 | `SpawnDemo` + `ColorCube` | S06 | NGO · Object spawning / NetworkObject ownership |
| 8 | `MochilaDemo` | S07 | NGO · NetworkVariables (NetworkList) |
| 9 | `AnticipacionDemo` | S10 | NGO · Client anticipation |
| 10 | `RebobinadoDemo` | S11 | NGO no tiene API: patrón *server side rewind* del CTX de la S11 |

La **validación en servidor (S12)** no es un script aparte: son las comprobaciones
marcadas `// S12` dentro de los RPC de `SpawnDemo`, `MochilaDemo`, `AnticipacionDemo` y
`RebobinadoDemo`.

`Editor/OdinEnNetworkBehaviour.cs` hace que Odin dibuje los botones en los
`NetworkBehaviour` (Netcode los dibuja con su propio Inspector y ahí no salen). Se copia
una vez a cualquier proyecto.

## Cómo se prueba

1. **Window → Multiplayer → Play Mode** → activa un segundo jugador. En su ventana,
   menú **Layout** → **Inspector** y **Hierarchy** (solo se ven en Play).
2. **Play**. En red local: `NetworkManager` → **Start Host** en una ventana y
   **Start Client** en la otra. Por internet: `Auth` → **Sign In Anonymously** en las dos,
   `Sesion` → **Create Session** en una y **Join Session By Code** en la otra.
3. Los botones de red están en el jugador: en la Hierarchy, el tuyo se llama
   **Jugador N (YO)**.
4. Para los módulos 9 y 10: **Window → Multiplayer → Network Simulator** a 250 ms en el
   cliente.

## Antes de la primera vez

Los servicios usan el proyecto de Unity Cloud **S7MA** (organización `juanneyraalonzo`),
el del curso. Para otro: **Edit → Project Settings → Services**.

> Odin Inspector va dentro (`Assets/Plugins/Sirenix`) para que los botones funcionen al
> clonar. Es un asset de pago.
