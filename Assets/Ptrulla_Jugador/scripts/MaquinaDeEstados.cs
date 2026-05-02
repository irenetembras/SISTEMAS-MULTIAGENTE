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

    // Referencias para poder leer la memoria y movernos
    private IACerebro cerebro;
    private IAMovimiento movimiento;

    // El cerebro llama a esto al despertar
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

        // Se persigue hasta que el líder diga basta.
        // No hacemos nada, el GPS o la vista nos guía 

        if (estadoActual == exploracion)
        {
            // Bucle infinito de exploración.
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