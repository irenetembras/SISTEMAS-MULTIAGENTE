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
    public Vector3 coordenadaTactica;
    public System.Collections.Generic.List<Vector3> rutaExploracion = new System.Collections.Generic.List<Vector3>();
    public bool objetivoDetectado = false;
    public Vector3 ultimaPosJugador;

    private GestorSensores sensores;
    private IAMovimiento movimiento;
    private RolTactico ultimoRolEjecutado = RolTactico.PatrullaNormal;

    // FSM ejecutora: controla el cuerpo (patrullar, perseguir, buscar...)
    public MaquinaDeEstados fsm { get; private set; }

    // FSM táctica: controla la jerarquía (libre, comandante, subordinado)
    public FSMTactica fsmTactica { get; private set; }

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
        RecogerObjetivo.OnBotinRobado += AlRobarBotinEnMiCara;
    }

    void OnDisable()
    {
        if (sensores != null)
        {
            sensores.OnJugadorDetectado -= AlDetectar;
            sensores.OnJugadorPerdido   -= AlPerder;
        }
        RecogerObjetivo.OnBotinRobado -= AlRobarBotinEnMiCara;
    }

    void Update()
    {
        if (fsm.estadoActual == null) return;

        if (objetivoDetectado && sensores.TransformJugador != null)
            ultimaPosJugador = sensores.TransformJugador.position;

        EjecutarRolTactico();
        fsm.ActualizarMaquina();
    }

    // Traduce el rol social asignado al estado físico correspondiente
    private void EjecutarRolTactico()
    {
        RolTactico rolActual = capaSocial.miRolAsignado;
        if (rolActual != ultimoRolEjecutado)
        {
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
                // No requiere que el guardia haya visto al jugador directamente
                if (fsm.estadoActual != fsm.persecucion) fsm.CambiarEstado(fsm.persecucion);
                break;

            case RolTactico.ExplorarSectorSospechoso:
                if (fsm.estadoActual != fsm.exploracion)
                    fsm.CambiarEstado(fsm.exploracion);
                break;
        }
    }

    private void AlDetectar(Vector3 pos)
    {
        objetivoDetectado = true;
        ultimaPosJugador  = pos;

        if (sensores.LoVeo && RecogerObjetivo.tieneElBotin)
        {
            Debug.Log($"[CEREBRO {gameObject.name}] ¡LE VEO CON EL BOTÍN! ¡Cerrad la puerta!");
            capaSocial.miRolAsignado = RolTactico.PersecucionActiva;
            coordenadaTactica = pos;
            capaSocial.DarAlarmaRobo();
        }

        Debug.Log($"[CEREBRO {gameObject.name}] Jugador DETECTADO en {pos}. Lanzando persecucion y asumiendo mando.");

        if (fsm.estadoActual != fsm.persecucion)
            fsm.CambiarEstado(fsm.persecucion);

        fsmTactica.AsumirMando(pos);
    }

    private void AlRobarBotinEnMiCara()
    {
        if (sensores.LoVeo)
        {
            Debug.Log($"[CEREBRO {gameObject.name}] ¡Acaba de coger el botín en mis narices! ¡Cerrad puertas!");
            capaSocial.DarAlarmaRobo();
        }
    }

    private void AlPerder()
    {
        Debug.Log($"[CEREBRO {gameObject.name}] Jugador PERDIDO. objetivoDetectado = false.");
        objetivoDetectado = false;
    }
}
