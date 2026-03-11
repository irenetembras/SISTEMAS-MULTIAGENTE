using UnityEngine;

// EL PATRÓN ESTADO - Todos los comportamientos heredarán de aquí
public abstract class EstadoIA : MonoBehaviour
{
    [HideInInspector] public IACerebro cerebro;
    [HideInInspector] public IAMovimiento movimiento;

    // Esta función la llamará el Cerebro para darle al estado las herramientas que necesita
    public virtual void Configurar(IACerebro c, IAMovimiento m)
    {
        cerebro = c;
        movimiento = m;
        this.enabled = false; // Todos los cartuchos empiezan apagados
    }

    // Funciones estándar del Patrón Estado
    public virtual void AlEntrar() { this.enabled = true; } // Encendemos el Update de este script
    public virtual void AlSalir() { this.enabled = false; } // Apagamos el Update
}
