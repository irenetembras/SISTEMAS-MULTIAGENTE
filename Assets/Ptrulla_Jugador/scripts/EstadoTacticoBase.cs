using UnityEngine;

// Clase base para todos los estados de la FSM Táctica (jerarquía/contratos).
// Mismo patrón que EstadoIA pero para la capa social.
public abstract class EstadoTacticoBase : MonoBehaviour
{
    [HideInInspector] public FSMTactica fsmTactica;
    [HideInInspector] public IACerebro cerebro;

    public virtual void Configurar(FSMTactica fsm, IACerebro c)
    {
        fsmTactica = fsm;
        cerebro = c;
        this.enabled = false;
    }

    public virtual void AlEntrar() { this.enabled = true; }
    public virtual void AlSalir()  { this.enabled = false; }

    // Cada estado decide qué hacer con cada mensaje FIPA que le llega
    public virtual void ProcesarMensaje(MensajeFIPA mensaje) { }
}
