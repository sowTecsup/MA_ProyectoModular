# MA_ProyectoModular · Todo lo visto, a botones

**Desarrollo de Videojuegos para Móviles Avanzado** · 6.º ciclo · C26S

Un solo proyecto con **todo lo visto de la semana 1 a la 12**, en versión sencilla.
Un script, `MultiplayerLab`, agrupa los temas en **11 módulos plegables**; cada botón
(Odin Inspector) hace una sola cosa y la cuenta por consola. Se le da Play, se abre el
módulo y se van pulsando botones.

Unity `6000.3.9f1` · Netcode for GameObjects **`2.13.3`** · Multiplayer Services `2.4.0`
· Authentication `3.8.0` · Cloud Save `3.4.1` · Multiplayer Play Mode `2.0.2` · Multiplayer Tools `2.2.12`.

## Los módulos

| # | Módulo | Semana | Botones |
|---|---|---|---|
| 1 | Authentication | S08 | Iniciar sesión anónima · Cambiar nombre en UGS · Cerrar sesión |
| 2 | Red local: host y cliente | S01 · S06 · S09 | Host · Cliente · Desconectar |
| 3 | Sesión online: Lobby + Relay | S08 | Crear sesión · Unirse por código · Unirse rápido · Ver jugadores · Salir |
| 4 | RPC | S02 | Saludar a todos |
| 5 | NetworkVariable: estado | S03 · S04 | Cambiar mi nombre · Ver nombres de todos |
| 6 | Spawn y dueño | S06 | Pedir cubo · Regalar mi cubo · Consumir mi cubo |
| 7 | Mochila con NetworkList | S07 | Guardar mi cubo · Soltar un cubo · Ver mochila |
| 8 | Cloud Save | S09 | Guardar la mochila en la nube · Cargarla y pedir reponerla |
| 9 | Predicción y reconciliación | S10 | Paso sin predicción · Paso con predicción · Medir RTT |
| 10 | Lag compensation | S11 | Golpear (rebobina al rival al instante en que lo viste) |
| 11 | Validación en servidor | S12 | Siete trampas: golpe del pasado y del futuro, 10 pasos de golpe, guardar lo ajeno, reponer 999, teletransporte, escribir mi vida |

## Los scripts (`Assets/Scripts`)

Cinco, y cada uno dice arriba en qué GameObject va y qué hay que arrastrar.
**La escena `Scenes/Lab` ya viene montada.**

1. `Ranura` — la casilla de la mochila y el estado de los pasos (structs)
2. `Sospechas` — el registro de rechazos del servidor; a los 30, expulsa
3. `ColorCube` — el cubo que nace, cambia de dueño y muere
4. `PlayerAvatar` — **todo lo que viaja por la red**: variables, lista y RPC
5. `MultiplayerLab` — los once módulos de botones

## Cómo se prueba

1. **Window → Multiplayer → Play Mode** → activa un segundo jugador.
2. En su ventana, menú **Layout** → marca **Inspector** y **Hierarchy** (solo se ven en Play).
3. **Play**. Selecciona `MultiplayerLab` en las dos ventanas.
4. **En red local:** módulo 2, *Host* en una y *Cliente* en la otra.
   **Por internet:** módulo 1 en las dos, *Crear sesión* en una y *Unirse por código*
   en la otra con el código que sale en la consola.
5. Ve abriendo módulos. Para los módulos 9 y 10, **Window → Multiplayer → Network
   Simulator** a 250 ms en el cliente, y mueve al otro jugador con **WASD**.

El host puede probar casi todo él solo, también las trampas: el servidor las rechaza
igual, pero **al host no lo expulsa** (es el servidor) y **sí** le deja escribir su vida.

## Antes de la primera vez

Los servicios usan el proyecto de Unity Cloud **S7MA** (organización `juanneyraalonzo`),
el del curso. Si se cambia de organización: **Edit → Project Settings → Services** y
enlazar otro proyecto con Lobby, Relay y Cloud Save activos.

> Odin Inspector va dentro (`Assets/Plugins/Sirenix`) para que los botones funcionen
> al clonar. Es un asset de pago.
