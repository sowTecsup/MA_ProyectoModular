// =============================================================================
// ORDEN ......... 4 de 5. Usa Ranura, EstadoPasos, Sospechas y ColorCube.
// GAMEOBJECT .... prefab "PlayerAvatar" (3D Object > Capsule)
// COMPONENTES ... NetworkObject
//                 NetworkTransform   Authority Mode = Owner
//                 PlayerAvatar
// ARRASTRAR ..... prefab ColorCube a cubePrefab
//                 este prefab a NetworkManager > Player Prefab
// TECLAS ........ WASD mover. Todo lo demas, con los botones de MultiplayerLab.
// =============================================================================
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

// Todo lo que viaja por la red vive aqui: solo un NetworkBehaviour puede tener
// NetworkVariable, NetworkList y RPC. MultiplayerLab solo aprieta sus botones.
public class PlayerAvatar : NetworkBehaviour
{
    public static PlayerAvatar Local;          // el avatar de ESTA maquina
    public const float ALCANCE_MANOS = 2.5f;   // guardar y regalar (S12)

    public NetworkObject cubePrefab;
    public float velocidad = 5f;
    public float alcanceGolpe = 3f;

    // ---- S03 · S04 · Estado: lo que el que llega tarde tambien ve --------------
    // El nombre lo escribe su dueno; la vida y la experiencia, SOLO el servidor.
    public NetworkVariable<FixedString32Bytes> nombre =
        new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> vida = new(100);
    public NetworkVariable<int> experiencia = new(0);
    public int Nivel => 1 + experiencia.Value / 100;   // se deriva: nunca se envia (S12)

    // ---- S07 · La mochila: la ve su dueno, la escribe el servidor ---------------
    public NetworkList<Ranura> mochila;

    // ---- S10 · Pasos: el servidor devuelve su verdad solo al dueno --------------
    public NetworkVariable<EstadoPasos> pasosServidor = new(readPerm: NetworkVariableReadPermission.Owner);
    public int pasosPredichos;
    public int correcciones;
    private struct Prediccion { public int secuencia; public int valor; }
    private readonly List<Prediccion> pendientes = new List<Prediccion>();
    private int secuencia;
    private float enviadoEn;

    // ---- Solo en el servidor: historial (S11), presupuesto y vigilancia (S12) ---
    private struct Muestra { public int tick; public Vector3 pos; }
    private const int TAM = 64;                    // 64 ticks a 30 Hz = 2 s de memoria
    private readonly Muestra[] historial = new Muestra[TAM];
    private const int RAFAGA = 4;
    private int presupuesto = RAFAGA;
    private int ultimoGolpe = -100;
    private int tickDeSpawn;
    private Vector3 ultimaPos;
    private bool mochilaRepuesta;

    // Una NetworkList se crea en Awake, NO en la declaracion (reserva memoria nativa).
    void Awake()
    {
        mochila = new NetworkList<Ranura>(null, NetworkVariableReadPermission.Owner,
                                                NetworkVariableWritePermission.Server);
    }

    public override void OnNetworkSpawn()
    {
        GetComponent<Renderer>().material.color = ColorCube.ColorDe(OwnerClientId);
        vida.OnValueChanged += AlCambiarVida;
        experiencia.OnValueChanged += AlCambiarExperiencia;
        mochila.OnListChanged += AlCambiarMochila;

        if (IsOwner)
        {
            Local = this;
            transform.position = new Vector3(OwnerClientId * 1.5f, 1f, 0f);   // uno al lado del otro
            pasosServidor.OnValueChanged += Reconciliar;
        }
        if (IsServer)
        {
            tickDeSpawn = NetworkManager.ServerTime.Tick;
            NetworkManager.NetworkTickSystem.Tick += AlTickDelServidor;
        }
        Debug.Log("[Jugador " + OwnerClientId + "] aparece" + (IsOwner ? " <- YO" : ""));
    }

    public override void OnNetworkDespawn()
    {
        vida.OnValueChanged -= AlCambiarVida;
        experiencia.OnValueChanged -= AlCambiarExperiencia;
        mochila.OnListChanged -= AlCambiarMochila;
        if (IsOwner) { pasosServidor.OnValueChanged -= Reconciliar; Local = null; }
        if (IsServer) NetworkManager.NetworkTickSystem.Tick -= AlTickDelServidor;
    }

    // S05 · Con Authority Mode = Owner el dueno mueve y el NetworkTransform replica.
    void Update()
    {
        if (!IsOwner || Keyboard.current == null) return;
        Keyboard k = Keyboard.current;
        Vector3 dir = new Vector3((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0), 0f,
                                  (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
        transform.position += dir * velocidad * Time.deltaTime;
    }

    // ---- S02 · RPC: un evento que no deja rastro --------------------------------

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Owner)]
    public void SaludarRpc(string texto, RpcParams rpcParams = default)
    {
        Debug.Log("[RPC] el cliente " + rpcParams.Receive.SenderClientId + " dice: " + texto);
    }

    // ---- S06 · Spawn con dueno ---------------------------------------------------

    // Un cliente no puede hacer Spawn: lo pide, y el servidor lo crea A SU NOMBRE.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void PedirCuboRpc()
    {
        CrearCubo();
    }

    private void CrearCubo()
    {
        Vector3 delante = transform.position + Vector3.forward * 1.5f;
        NetworkObject.InstantiateAndSpawn(cubePrefab.gameObject, NetworkManager,
                                          ownerClientId: OwnerClientId, position: delante);
    }

    // ---- S07 + S12 · Mochila, con las preguntas del servidor ----------------------

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void GuardarRpc(NetworkObjectReference cuboRef)
    {
        if (!cuboRef.TryGet(out NetworkObject cubo)) return;                       // ya no existe
        if (cubo.OwnerClientId != OwnerClientId)                                    // que? lo tuyo
        { Sospechas.Anotar(NetworkManager, OwnerClientId, "guardar lo ajeno"); return; }
        if (Vector3.Distance(cubo.transform.position, transform.position) > ALCANCE_MANOS)   // donde?
        { Sospechas.Anotar(NetworkManager, OwnerClientId, "guardar desde lejos"); return; }
        cubo.Despawn();
        Guardar(Ranura.CUBO, 1);
    }

    // Soltar = se quita el dato y nace UN cubo (no EL cubo) a tu nombre.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void SoltarRpc()
    {
        int i = Buscar(Ranura.CUBO);
        if (i == -1) return;
        Ranura r = mochila[i];
        r.cantidad--;
        if (r.cantidad == 0) mochila.RemoveAt(i);
        else mochila[i] = r;
        CrearCubo();   // no PedirCuboRpc: el servidor no es el dueno y el permiso Owner lo frenaria
    }

    // ---- S09 + S12 · La mochila que vuelve de Cloud Save, sin duplicar -----------

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void PedirReponerMochilaRpc(Ranura[] nube)
    {
        // 1) una vez, y nunca encima de lo que ya hay: si no, guardar y cargar DUPLICA
        if (mochilaRepuesta || mochila.Count > 0) { Debug.Log("[Mochila] manda la copia del servidor: se ignora la nube"); return; }
        mochilaRepuesta = true;
        if (nube == null || nube.Length > 28) return;                              // forma posible
        foreach (Ranura r in nube)                                                  // contenido posible
        {
            if ((r.tipo == Ranura.CUBO || r.tipo == Ranura.TRONCO) && r.cantidad >= 1 && r.cantidad <= 99)
                Guardar(r.tipo, r.cantidad);
            else
                Sospechas.Anotar(NetworkManager, OwnerClientId, "casilla imposible: " + r.cantidad);
        }
    }

    // ---- S04 + S11 + S12 · Golpear (evento) que baja la vida (estado) ------------

    // El cliente dice CUANDO vio al rival: la hora del servidor menos lo que atrasa el interpolador.
    public void Golpear()
    {
        GolpearRpc(NetworkManager.ServerTime.Time - NetworkTransform.GetTickLatencyInSeconds());
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void GolpearRpc(double visto)
    {
        double ahora = NetworkManager.ServerTime.Time;
        double rtt = NetworkManager.NetworkConfig.NetworkTransport.GetCurrentRtt(OwnerClientId) / 1000.0;
        double maxAtraso = System.Math.Min(2 * rtt + 0.2, 1.0);                     // su RTT lo explica
        if (visto > ahora || ahora - visto > maxAtraso)                             // cuando?
        { Sospechas.Anotar(NetworkManager, OwnerClientId, "golpe a un instante imposible"); return; }

        int tick = NetworkManager.ServerTime.Tick;
        if (tick - ultimoGolpe < 15)                                                // a su ritmo?
        { Sospechas.Anotar(NetworkManager, OwnerClientId, "golpe antes de tiempo"); return; }
        ultimoGolpe = tick;

        Debug.Log("[Golpe de " + OwnerClientId + "] lo vio hace " + (int)((ahora - visto) * 1000) + " ms");
        foreach (NetworkClient c in NetworkManager.ConnectedClientsList)
        {
            if (c.PlayerObject == null || c.PlayerObject == NetworkObject) continue;
            PlayerAvatar rival = c.PlayerObject.GetComponent<PlayerAvatar>();
            if (!rival.PosicionEn(visto, out Vector3 dondeEstaba)) continue;

            float hoy = Vector3.Distance(transform.position, rival.transform.position);
            float entonces = Vector3.Distance(transform.position, dondeEstaba);       // el rival, en el pasado
            Debug.Log("   rival " + c.ClientId + ": ahora a " + hoy.ToString("0.0") + " m, cuando lo viste a " + entonces.ToString("0.0") + " m");
            if (entonces > alcanceGolpe) continue;

            rival.vida.Value = rival.vida.Value > 10 ? rival.vida.Value - 10 : 100;   // a cero, renace
            experiencia.Value += 25;   // el resultado lo pone el servidor, nunca el cliente
        }
    }

    // S11 · Donde estaba en el instante t? Interpola entre los dos ticks guardados.
    public bool PosicionEn(double t, out Vector3 pos)
    {
        double dt = NetworkManager.ServerTime.FixedDeltaTimeAsDouble;
        int t0 = (int)System.Math.Floor(t / dt);
        Muestra a = historial[t0 % TAM], b = historial[(t0 + 1) % TAM];
        pos = transform.position;
        if (a.tick != t0 || b.tick != t0 + 1) return false;   // demasiado viejo
        pos = Vector3.Lerp(a.pos, b.pos, (float)((t - t0 * dt) / dt));
        return true;
    }

    // ---- S10 · Prediccion y reconciliacion, con un contador de pasos -------------

    // Sin prediccion se espera al servidor; con prediccion se aplica YA y se corrige despues.
    public void Paso(bool predecir)
    {
        secuencia++;
        if (predecir)
        {
            pasosPredichos++;
            pendientes.Add(new Prediccion { secuencia = secuencia, valor = pasosPredichos });
        }
        enviadoEn = Time.realtimeSinceStartup;
        PasoRpc(secuencia);
    }

    // S12 · El servidor gana una entrada por tick y deja acumular unas pocas (RAFAGA).
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void PasoRpc(int sec)
    {
        int pasos = pasosServidor.Value.pasos;
        if (presupuesto > 0) { presupuesto--; pasos++; }
        else Sospechas.Anotar(NetworkManager, OwnerClientId, "mas pasos de los que el tiempo explica");
        pasosServidor.Value = new EstadoPasos { pasos = pasos, secuencia = sec };
    }

    private void Reconciliar(EstadoPasos antes, EstadoPasos servidor)
    {
        int predije = -1;
        foreach (Prediccion p in pendientes) if (p.secuencia == servidor.secuencia) predije = p.valor;
        pendientes.RemoveAll(p => p.secuencia <= servidor.secuencia);

        // 1) vuelvo a la verdad del servidor  2) re-aplico lo que todavia no ha visto
        pasosPredichos = servidor.pasos + pendientes.Count;

        int ms = (int)((Time.realtimeSinceStartup - enviadoEn) * 1000f);
        if (predije != -1 && predije != servidor.pasos)
        {
            correcciones++;
            Debug.LogWarning("[Pasos] CORRECCION: predije " + predije + ", el servidor dice " + servidor.pasos);
        }
        else Debug.Log("[Pasos] el servidor confirma " + servidor.pasos + " (" + ms + " ms despues del ultimo envio)");
    }

    // ---- En cada tick del servidor ----------------------------------------------

    private void AlTickDelServidor()
    {
        int tick = NetworkManager.ServerTime.Tick;
        historial[tick % TAM] = new Muestra { tick = tick, pos = transform.position };   // S11
        presupuesto = Mathf.Min(presupuesto + 1, RAFAGA);                               // S12

        // S12 · Con Authority Mode = Owner el servidor NO puede corregir la posicion: solo vigilarla.
        float maximo = velocidad * NetworkManager.ServerTime.FixedDeltaTime * 2f;       // 100 % de margen
        if (tick - tickDeSpawn > 30 && Vector3.Distance(transform.position, ultimaPos) > maximo)
            Sospechas.Anotar(NetworkManager, OwnerClientId, "se movio mas de lo posible");
        ultimaPos = transform.position;
    }

    // ---- Trampas: lo que haria un cliente modificado (S12) ----------------------

    public void TrampaTeletransporte()
    {
        transform.position += Vector3.forward * 20f;   // el dueno escribe su posicion: nadie se lo impide
    }

    public void TrampaEscribirVida()
    {
        vida.Value = 999;   // en un cliente NGO lo rechaza con un error; en el host SI deja: es el servidor
    }

    // ---- Ayudas -------------------------------------------------------------------

    private void Guardar(int tipo, int cantidad)
    {
        int i = Buscar(tipo);
        if (i == -1) { mochila.Add(new Ranura { tipo = tipo, cantidad = cantidad }); return; }
        Ranura r = mochila[i];   // mochila[i].cantidad++ no compila: el indice devuelve una copia
        r.cantidad += cantidad;
        mochila[i] = r;
    }

    private int Buscar(int tipo)
    {
        for (int i = 0; i < mochila.Count; i++)
            if (mochila[i].tipo == tipo) return i;
        return -1;
    }

    private void AlCambiarVida(int antes, int ahora)
    {
        Debug.Log("[Jugador " + OwnerClientId + "] vida " + antes + " -> " + ahora);
    }

    private void AlCambiarExperiencia(int antes, int ahora)
    {
        Debug.Log("[Jugador " + OwnerClientId + "] experiencia " + ahora + " (nivel " + Nivel + ")");
    }

    private void AlCambiarMochila(NetworkListEvent<Ranura> e)
    {
        Debug.Log("[Mochila de " + OwnerClientId + "] " + e.Type + ", casilla " + e.Index + ": " + e.Value.Nombre() + " x" + e.Value.cantidad);
    }
}
