using UnityEngine;

// Estado: este guardia ha detectado al jugador y asume el mando táctico del escuadrón.
// Delega toda la lógica de planificación en PlanificadorTactico.
public class EstadoTactico_Comandante : EstadoTacticoBase
{
    private PlanificadorTactico planificador;

    public override void Configurar(FSMTactica fsm, IACerebro c)
    {
        base.Configurar(fsm, c);
        planificador = GetComponent<PlanificadorTactico>();
        if (planificador == null)
            Debug.LogError($"[{gameObject.name}] FSMTactica: falta el componente PlanificadorTactico en el guardia.");
    }

    // El cerebro llama a esto inmediatamente tras la transición
    public void IniciarMando(Vector3 posLadron)
    {
        planificador.IniciarPlanificacion(posLadron);
    }

    void Update()
    {
        if (planificador == null) return;

        planificador.ActualizarFase();

        if (planificador.DebeTerminarMando())
            fsmTactica.CambiarEstado(fsmTactica.libre);
    }

    public override void AlSalir()
    {
        base.AlSalir();
        planificador.TerminarMando();
    }

    // Solo el comandante recibe y registra las ofertas (PROPOSE) del resto del escuadrón
    public override void ProcesarMensaje(MensajeFIPA mensaje)
    {
        if (mensaje.performativa == PerformativaFIPA.PROPOSE)
        {
            float dist = float.Parse(mensaje.contenido, System.Globalization.CultureInfo.InvariantCulture);
            Debug.Log($"[COMANDANTE {gameObject.name}] Oferta recibida de {mensaje.emisor.name}: {dist:F1}m al objetivo.");
            planificador.RecibirOferta(mensaje.emisor, dist);
        }
        else
        {
            Debug.Log($"[COMANDANTE {gameObject.name}] Mensaje ignorado: {mensaje.performativa} de {mensaje.emisor.name}");
        }
    }
}
