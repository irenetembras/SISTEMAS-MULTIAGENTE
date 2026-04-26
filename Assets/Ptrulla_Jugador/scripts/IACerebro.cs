using UnityEngine;

[RequireComponent(typeof(GestorSensores))]
[RequireComponent(typeof(IAMovimiento))]
[RequireComponent(typeof(GestorSocial))]
[RequireComponent(typeof(MaquinaDeEstados))]
[RequireComponent(typeof(FSMTactica))]
public class IACerebro : MonoBehaviour
{
    [Header("Memoria Global")]
    public Transform puntoMeta;
    public Transform puntoObjetivo;
    public bool objetivoDetectado = false;
    public Vector3 ultimaPosJugador;

    private GestorSensores sensores;
    private IAMovimiento movimiento;
    private RolTactico ultimoRolEjecutado = RolTactico.PatrullaNormal;

    // FSM Ejecutora: controla el cuerpo (patrullar, perseguir, buscar...)
    public MaquinaDeEstados fsm { get; private set; }

    // FSM Táctica: controla la jerarquía (libre, comandante, subordinado)
    public FSMTactica fsmTactica { get; private set; }

    // Puente de lectura para que EjecutarRolTactico sepa qué hacer
    public GestorSocial capaSocial { get; private set; }

    void Awake()
    {
        sensores   = GetComponent<GestorSensores>();
        movimiento = GetComponent<IAMovimiento>();
        capaSocial = GetComponent<GestorSocial>();
        fsm        = GetComponent<MaquinaDeEstados>();
        fsmTactica = GetComponent<FSMTactica>();

        fsm.Inicializar(this, movimiento);
        fsmTactica.Inicializar(this);
    }

    void OnEnable()
    {
        sensores.OnJugadorDetectado += AlDetectar;
        sensores.OnJugadorPerdido   += AlPerder;
    }

    void OnDisable()
    {
        if (sensores != null)
        {
            sensores.OnJugadorDetectado -= AlDetectar;
            sensores.OnJugadorPerdido   -= AlPerder;
        }
    }

    void Update()
    {
        if (fsm.estadoActual == null) return;

        if (objetivoDetectado && sensores.TransformJugador != null)
            ultimaPosJugador = sensores.TransformJugador.position;

        // Puente FSM Táctica → FSM Ejecutora: traduce rol social a estado físico
        EjecutarRolTactico();

        fsm.ActualizarMaquina();
    }

    private void EjecutarRolTactico()
    {
        RolTactico rolActual = capaSocial.miRolAsignado;
        if (rolActual != ultimoRolEjecutado)
        {
            Debug.Log($"[CEREBRO {gameObject.name}] Rol cambiado: {ultimoRolEjecutado} → {rolActual}. Actualizando FSM Ejecutora.");
            ultimoRolEjecutado = rolActual;
        }

        switch (rolActual)
        {
            case RolTactico.PatrullaNormal:
            case RolTactico.PatrullaSectorAdyacente:
                if (fsm.estadoActual != fsm.patrulla) fsm.CambiarEstado(fsm.patrulla);
                break;

            case RolTactico.BloqueoSalida:
                if (fsm.estadoActual != fsm.emboscada) fsm.CambiarEstado(fsm.emboscada);
                break;

            case RolTactico.PersecucionActiva:
                if (fsm.estadoActual != fsm.persecucion) fsm.CambiarEstado(fsm.persecucion);
                break;

            case RolTactico.ExplorarSectorSospechoso:
                if (fsm.estadoActual != fsm.busqueda && fsm.estadoActual != fsm.exploracion)
                    fsm.CambiarEstado(fsm.busqueda);
                break;
        }
    }

    private void AlDetectar(Vector3 pos)
    {
        objetivoDetectado = true;
        ultimaPosJugador  = pos;
        Debug.Log($"[CEREBRO {gameObject.name}] Jugador DETECTADO en {pos}. Lanzando persecucion y asumiendo mando.");

        if (fsm.estadoActual != fsm.persecucion)
            fsm.CambiarEstado(fsm.persecucion);

        fsmTactica.AsumirMando(pos);
    }

    private void AlPerder()
    {
        Debug.Log($"[CEREBRO {gameObject.name}] Jugador PERDIDO. objetivoDetectado = false.");
        objetivoDetectado = false;
    }
}
