using UnityEngine;

// FSM jerárquica: gestiona el rol social del guardia (Libre / Comandante / Subordinado).
// Corre en paralelo con MaquinaDeEstados en el mismo GameObject.
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
        while (buzon != null && buzon.HayMensajesNuevos() && leidos < 2)
        {
            MensajeFIPA msg = buzon.ExtraerSiguienteMensaje();
            if (msg != null)
            {
                if (estadoActual == null)
                    Debug.LogWarning($"[FSM-T {gameObject.name}] Mensaje {msg.performativa} recibido pero estadoActual es NULL. Comprueba que libre/comandante/subordinado estan asignados en el Inspector.");
                else
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

    public void AsumirMando(Vector3 posLadron)
    {
        if (estadoActual == comandante) return;
        CambiarEstado(comandante);
        ((EstadoTactico_Comandante)comandante).IniciarMando(posLadron);
    }
}
