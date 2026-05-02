using UnityEngine;

// Clase base para todos los estados físicos del guardia
public abstract class EstadoIA : MonoBehaviour
{
    [HideInInspector] public IACerebro cerebro;
    [HideInInspector] public IAMovimiento movimiento;

    public virtual void Configurar(IACerebro c, IAMovimiento m)
    {
        cerebro = c;
        movimiento = m;
        this.enabled = false;
    }

    public virtual void AlEntrar() { this.enabled = true; }
    public virtual void AlSalir()  { this.enabled = false; }
}
