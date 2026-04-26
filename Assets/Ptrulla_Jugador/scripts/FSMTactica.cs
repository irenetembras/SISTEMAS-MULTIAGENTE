using UnityEngine;

// FSM JERÁRQUICA: gestiona el rol social del guardia (Libre / Comandante / Subordinado).
// Corre en paralelo con MaquinaDeEstados (FSM Ejecutora) en el mismo GameObject.
[RequireComponent(typeof(BuzonMensajes))]
[RequireComponent(typeof(PlanificadorTactico))]
public class FSMTactica : MonoBehaviour
{
    [Header("Estados de la jerarquía")]
    public EstadoTacticoBase libre;
    public EstadoTacticoBase comandante;
    public EstadoTacticoBase subordinado;

    public EstadoTacticoBase estadoActual { get; private set; }

    private BuzonMensajes buzon;

    // IACerebro llama a esto en su Awake
    public void Inicializar(IACerebro cerebro)
    {
        buzon = GetComponent<BuzonMensajes>();
        GetComponent<PlanificadorTactico>().Inicializar(cerebro);

        if (libre)       libre.Configurar(this, cerebro);
        if (comandante)  comandante.Configurar(this, cerebro);
        if (subordinado) subordinado.Configurar(this, cerebro);

        CambiarEstado(libre);
    }

    void Update()
    {
        int leidos = 0;
        while (buzon != null && buzon.HayMensajesNuevos() && leidos < 3)
        {
            MensajeFIPA msg = buzon.ExtraerSiguienteMensaje();
            if (msg != null)
            {
                if (estadoActual == null)
                    Debug.LogWarning($"[FSM-T {gameObject.name}] Mensaje {msg.performativa} recibido pero estadoActual es NULL. Comprueba que libre/comandante/subordinado estan asignados en el Inspector.");
                else
                    Debug.Log($"[FSM-T {gameObject.name}] Mensaje recibido: {msg.performativa} de {msg.emisor.name} | Estado actual: {estadoActual.GetType().Name}");
                estadoActual?.ProcesarMensaje(msg);
            }
            leidos++;
        }
    }

    public void CambiarEstado(EstadoTacticoBase nuevo)
    {
        if (nuevo == null) return;
        estadoActual?.AlSalir();
        estadoActual = nuevo;
        estadoActual.AlEntrar();
    }

    // Llamado por IACerebro al detectar al jugador con los propios sensores
    public void AsumirMando(Vector3 posLadron)
    {
        if (estadoActual == comandante) return;
        CambiarEstado(comandante);
        ((EstadoTactico_Comandante)comandante).IniciarMando(posLadron);
    }
}
