using UnityEngine;

public class MaquinaDeEstados : MonoBehaviour
{
    [Header("Cartuchos de Comportamiento")]
    public EstadoIA patrulla;
    public EstadoIA persecucion;
    public EstadoIA emboscada;
    public EstadoIA busqueda;
    public EstadoIA exploracion;
    public EstadoIA comprobandoObjetivo;

    public EstadoIA estadoActual { get; private set; }

    private IACerebro cerebro;
    private IAMovimiento movimiento;

    public void Inicializar(IACerebro cerebroRef, IAMovimiento movRef)
    {
        cerebro = cerebroRef;
        movimiento = movRef;

        if(patrulla) patrulla.Configurar(cerebro, movimiento);
        if(persecucion) persecucion.Configurar(cerebro, movimiento);
        if(emboscada) emboscada.Configurar(cerebro, movimiento);
        if(exploracion) exploracion.Configurar(cerebro, movimiento);

        CambiarEstado(patrulla);
    }

    public void ActualizarMaquina()
    {
        if (estadoActual == null) return;

        if (estadoActual == exploracion)
        {
            // Al terminar el barrido, reiniciamos el estado para seguir explorando
            if (((EstadoExploracion)exploracion).exploracionTerminada) CambiarEstado(exploracion);
        }
    }

    public void CambiarEstado(EstadoIA nuevoEstado)
    {
        if (nuevoEstado == null) return;

        if (estadoActual != null) estadoActual.AlSalir();
        estadoActual = nuevoEstado;
        estadoActual.AlEntrar();
    }
}
